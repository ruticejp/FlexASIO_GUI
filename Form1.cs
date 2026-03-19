using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Commons.Media.PortAudio;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using Microsoft.Win32;
// Tomlyn is used instead of deprecated Nett library for TOML parsing (migrated in v0.36)
using Tomlyn;
using System.Runtime.InteropServices;

namespace FlexASIOGUI
{
    public partial class Form1 : Form
    {

        private bool InitDone = false;
        private string TOMLPath;
        private FlexGUIConfig flexGUIConfig;
        private Encoding legacyEncoding;
        private readonly string flexasioGuiVersion = "0.36";
        private readonly string flexasioVersion = "1.9";
        private readonly string tomlName = "FlexASIO.toml";
        private readonly string docUrl = "https://github.com/dechamps/FlexASIO/blob/master/CONFIGURATION.md";
        // Tomlyn library options for TOML serialization/deserialization
        TomlModelOptions tomlModelOptions = new();

        // FlexASIO is a separate driver DLL. Rather than hard-coding its path, read the install
        // location from the installer-written registry key and load it dynamically via LoadLibrary.
        // This improves portability and avoids relying on a fixed path.

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport(@"kernel32.dll")]
        public static extern uint GetACP();

        private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private delegate int InitializeDelegate(string PathName, bool TestMode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int CreateFlexASIODelegate();

        private static IntPtr flexAsioModule = IntPtr.Zero;
        private static InitializeDelegate initializeFunc;
        private static CreateFlexASIODelegate createFlexAsioFunc;

        private static string GetFlexASIOInstallPath()
        {
            // 1) Prefer the installer-written fork-specific key.
            //    The installer writes the GUI install path, but we need the FlexASIO install path.
            //    If the registry path does not actually contain FlexASIO.dll, ignore it and fall back.
            string[] registryKeys = new[]
            {
                "SOFTWARE\\Fabrikat\\FlexASIOGUI_Rutice\\Install",
                "SOFTWARE\\Fabrikat\\FlexASIOGUI\\Install"
            };

            try
            {
                foreach (var keyPath in registryKeys)
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);
                    var value = key?.GetValue("InstallPath") as string;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var normalized = NormalizeFlexAsioInstallPath(value);
                        if (!string.IsNullOrEmpty(normalized))
                        {
                            return normalized;
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry access failures and fall back to common install locations.
            }

            // 2) Common FlexASIO install paths (used by official installer).
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "FlexASIO"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "FlexASIO")
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            // 3) Fallback: locate FlexASIO.exe and derive install path from it.
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string flexASIOExe = FindFlexASIOExeUnder(programFiles);
            if (flexASIOExe != null)
            {
                return Path.GetDirectoryName(flexASIOExe);
            }

            // 4) Fallback: search under Program Files for FlexASIO.dll and pick the best candidate.
            var bestDll = ChooseBestFlexASIODll(programFiles);
            if (!string.IsNullOrEmpty(bestDll))
            {
                return Path.GetDirectoryName(Path.GetDirectoryName(bestDll));
            }

            return null;
        }

        private static string NormalizeFlexAsioInstallPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            // Accept a direct path to the DLL.
            if (File.Exists(path) && string.Equals(Path.GetFileName(path), "FlexASIO.dll", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetDirectoryName(path);
            }

            // Accept a folder containing one or more FlexASIO.dll candidates.
            if (Directory.Exists(path) && !string.IsNullOrEmpty(ChooseBestFlexASIODll(path)))
            {
                return path;
            }

            return null;
        }

        private static string FindFlexASIOExeUnder(string root)
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(root, "FlexASIO.exe", SearchOption.AllDirectories))
                {
                    return path;
                }
            }
            catch
            {
                // Ignore any access issues.
            }

            return null;
        }

        private static string ChooseBestFlexASIODll(string installPath)
        {
            try
            {
                var candidates = Directory.GetFiles(installPath, "FlexASIO.dll", SearchOption.AllDirectories);
                if (candidates.Length == 0)
                    return null;

                return FlexAsioDllSelector.ChooseBestDll(candidates, GetFileVersion, IsDll64Bit);
            }
            catch
            {
                return null;
            }
        }

        private static Version GetFileVersion(string path)
        {
            try
            {
                var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsDll64Bit(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var peReader = new System.Reflection.PortableExecutable.PEReader(stream);
                return peReader.PEHeaders.CoffHeader.Machine == System.Reflection.PortableExecutable.Machine.Amd64;
            }
            catch
            {
                return false;
            }
        }

        private void ResetFlexAsioModule()
        {
            if (flexAsioModule != IntPtr.Zero)
            {
                FreeLibrary(flexAsioModule);
                flexAsioModule = IntPtr.Zero;
                initializeFunc = null;
                createFlexAsioFunc = null;
            }
        }

        private static void WriteInstallPathToRegistry(string installPath)
        {
            try
            {
                using var key1 = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Fabrikat\\FlexASIOGUI_Rutice\\Install");
                key1?.SetValue("InstallPath", installPath);
            }
            catch
            {
                // ignore
            }

            try
            {
                using var key2 = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Fabrikat\\FlexASIOGUI\\Install");
                key2?.SetValue("InstallPath", installPath);
            }
            catch
            {
                // ignore
            }
        }

        private static bool TryLoadFlexASIODll(out string error, out string dllPathTried)
        {
            error = null;
            dllPathTried = null;

            if (flexAsioModule != IntPtr.Zero)
            {
                return true;
            }

            string installPath = GetFlexASIOInstallPath();
            if (string.IsNullOrWhiteSpace(installPath))
            {
                error = "Could not determine FlexASIO install path from registry or known locations.";
                return false;
            }

            // Choose the best candidate from all FlexASIO.dll under the install directory.
            dllPathTried = ChooseBestFlexASIODll(installPath);
            if (string.IsNullOrEmpty(dllPathTried))
            {
                error = $"FlexASIO.dll not found under install path: {installPath}";
                return false;
            }

            flexAsioModule = LoadLibraryEx(dllPathTried, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
            if (flexAsioModule == IntPtr.Zero)
            {
                int win32 = Marshal.GetLastWin32Error();
                error = $"Failed to load FlexASIO.dll (LoadLibraryEx failed; code={win32}: {new System.ComponentModel.Win32Exception(win32).Message}).";
                return false;
            }

            IntPtr proc = GetProcAddress(flexAsioModule, "Initialize");
            if (proc == IntPtr.Zero)
            {
                var exports = GetExportedNames(dllPathTried, 200);

                // Try to find a similar export name if Initialize is mangled or renamed.
                string altName = exports
                    .FirstOrDefault(n => n.IndexOf("initialize", StringComparison.OrdinalIgnoreCase) >= 0);

                if (!string.IsNullOrEmpty(altName))
                {
                    proc = GetProcAddress(flexAsioModule, altName);
                    if (proc != IntPtr.Zero)
                    {
                        initializeFunc = Marshal.GetDelegateForFunctionPointer<InitializeDelegate>(proc);
                        return true;
                    }
                }

                // Some FlexASIO versions export CreateFlexASIO instead of Initialize.
                if (exports.Contains("CreateFlexASIO"))
                {
                    IntPtr createProc = GetProcAddress(flexAsioModule, "CreateFlexASIO");
                    if (createProc != IntPtr.Zero)
                    {
                        createFlexAsioFunc = Marshal.GetDelegateForFunctionPointer<CreateFlexASIODelegate>(createProc);
                        // Call it once to ensure it can be instantiated (if required).
                        createFlexAsioFunc();
                        return true;
                    }
                }

                // If we still can't locate the right entrypoint, check for missing import dependencies.
                var imports = GetImportedDllNames(dllPathTried);
                var missingDeps = GetMissingDependencies(dllPathTried, imports);
                int win32 = Marshal.GetLastWin32Error();
                error = $"FlexASIO.dll does not export Initialize (GetProcAddress failed; code={win32}: {new System.ComponentModel.Win32Exception(win32).Message}). " +
                        $"Exports: {string.Join(", ", exports)}";
                if (missingDeps.Length > 0)
                {
                    error += $" Missing dependencies: {string.Join(", ", missingDeps)}.";
                    var hint = GetDependencyHelp(missingDeps);
                    if (!string.IsNullOrEmpty(hint))
                    {
                        error += " " + hint;
                    }
                }
                return false;
            }

            initializeFunc = Marshal.GetDelegateForFunctionPointer<InitializeDelegate>(proc);
            return true;
        }

        private static string[] GetExportedNames(string dllPath, int maxNames)
        {
            try
            {
                using var stream = File.OpenRead(dllPath);
                using var peReader = new System.Reflection.PortableExecutable.PEReader(stream);
                var headers = peReader.PEHeaders;
                var exportDir = headers.PEHeader.ExportTableDirectory;
                if (exportDir.RelativeVirtualAddress == 0)
                    return Array.Empty<string>();

                uint exportDirOffset = RvaToOffset(headers, (uint)exportDir.RelativeVirtualAddress);
                stream.Seek(exportDirOffset, SeekOrigin.Begin);
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

                reader.ReadUInt32(); // Characteristics
                reader.ReadUInt32(); // TimeDateStamp
                reader.ReadUInt16(); // MajorVersion
                reader.ReadUInt16(); // MinorVersion
                uint nameRva = reader.ReadUInt32();
                uint ordinalBase = reader.ReadUInt32();
                uint numberOfFunctions = reader.ReadUInt32();
                uint numberOfNames = reader.ReadUInt32();
                uint addressOfFunctionsRva = reader.ReadUInt32();
                uint addressOfNamesRva = reader.ReadUInt32();
                uint addressOfNameOrdinalsRva = reader.ReadUInt32();

                var names = new List<string>();
                int toRead = (int)Math.Min((long)numberOfNames, (long)maxNames);
                for (int i = 0; i < toRead; i++)
                {
                    uint nameRvaEntry = ReadUInt32AtRva(reader, headers, addressOfNamesRva + (uint)(i * 4));
                    string exportName = ReadNullTerminatedStringAtRva(stream, headers, nameRvaEntry);
                    if (!string.IsNullOrEmpty(exportName))
                        names.Add(exportName);
                }

                return names.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static string[] GetImportedDllNames(string dllPath)
        {
            try
            {
                using var stream = File.OpenRead(dllPath);
                using var peReader = new System.Reflection.PortableExecutable.PEReader(stream);
                var headers = peReader.PEHeaders;
                var importDir = headers.PEHeader.ImportTableDirectory;
                if (importDir.RelativeVirtualAddress == 0)
                    return Array.Empty<string>();

                uint importDirOffset = RvaToOffset(headers, (uint)importDir.RelativeVirtualAddress);
                stream.Seek(importDirOffset, SeekOrigin.Begin);
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

                var names = new List<string>();
                while (true)
                {
                    uint originalFirstThunk = reader.ReadUInt32();
                    uint timeDateStamp = reader.ReadUInt32();
                    uint forwarderChain = reader.ReadUInt32();
                    uint nameRva = reader.ReadUInt32();
                    uint firstThunk = reader.ReadUInt32();

                    if (originalFirstThunk == 0 && timeDateStamp == 0 && forwarderChain == 0 && nameRva == 0 && firstThunk == 0)
                        break;

                    string importName = ReadNullTerminatedStringAtRva(stream, headers, nameRva);
                    if (!string.IsNullOrEmpty(importName))
                        names.Add(importName);
                }

                return names.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static string[] GetMissingDependencies(string dllPath, string[] importedNames)
        {
            var missing = new List<string>();
            foreach (var name in importedNames)
            {
                try
                {
                    // Try to load it from the same directory first, then system path.
                    if (!NativeLibrary.TryLoad(Path.Combine(Path.GetDirectoryName(dllPath), name), out var handle) &&
                        !NativeLibrary.TryLoad(name, out handle))
                    {
                        missing.Add(name);
                    }
                    else
                    {
                        NativeLibrary.Free(handle);
                    }
                }
                catch
                {
                    missing.Add(name);
                }
            }

            return missing.ToArray();
        }

        private static uint RvaToOffset(System.Reflection.PortableExecutable.PEHeaders headers, uint rva)
        {
            foreach (var section in headers.SectionHeaders)
            {
                uint sectionRva = (uint)section.VirtualAddress;
                uint sectionRaw = (uint)section.PointerToRawData;
                uint sectionSize = (uint)Math.Max((long)section.VirtualSize, (long)section.SizeOfRawData);
                if (rva >= sectionRva && rva < sectionRva + sectionSize)
                {
                    return sectionRaw + (rva - sectionRva);
                }
            }
            return 0;
        }

        private static uint ReadUInt32AtRva(BinaryReader reader, System.Reflection.PortableExecutable.PEHeaders headers, uint rva)
        {
            long pos = reader.BaseStream.Position;
            uint offset = RvaToOffset(headers, rva);
            reader.BaseStream.Seek((long)offset, SeekOrigin.Begin);
            uint value = reader.ReadUInt32();
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            return value;
        }

        private static string ReadNullTerminatedStringAtRva(Stream stream, System.Reflection.PortableExecutable.PEHeaders headers, uint rva)
        {
            uint offset = RvaToOffset(headers, rva);
            stream.Seek((long)offset, SeekOrigin.Begin);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var bytes = new List<byte>();
            while (true)
            {
                byte b = reader.ReadByte();
                if (b == 0)
                    break;
                bytes.Add(b);
            }
            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        private static string GetDependencyHelp(string[] missingDeps)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "VCRUNTIME140.dll", "Install the Microsoft Visual C++ 2015-2022 Redistributable (x64)." },
                { "MSVCP140.dll", "Install the Microsoft Visual C++ 2015-2022 Redistributable (x64)." },
                { "VCRUNTIME140_1.dll", "Install the Microsoft Visual C++ 2015-2022 Redistributable (x64)." },
                { "VCRUNTIME140_2.dll", "Install the Microsoft Visual C++ 2015-2022 Redistributable (x64)." },
                { "ucrtbased.dll", "This looks like a debug build dependency (ucrtbased). Use a release build of FlexASIO or install the debug runtime." },
                { "api-ms-win-crt-*.dll", "Install the Windows Universal C Runtime (KB2999226) or the Visual C++ Redistributable." }
            };

            var hints = new HashSet<string>();
            foreach (var dep in missingDeps)
            {
                foreach (var pair in map)
                {
                    if (pair.Key.EndsWith("*"))
                    {
                        var prefix = pair.Key.TrimEnd('*');
                        if (dep.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            hints.Add(pair.Value);
                        }
                    }
                    else if (string.Equals(dep, pair.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        hints.Add(pair.Value);
                    }
                }
            }

            return hints.Count > 0 ? "Hint: " + string.Join(" ", hints) : null;
        }

        private static int InitializeFlexASIO(string PathName, bool TestMode)
        {
            if (!TryLoadFlexASIODll(out string err, out _))
            {
                throw new InvalidOperationException(err);
            }

            return initializeFunc(PathName, TestMode);
        }

        // Direct PortAudio P/Invoke declarations for UTF-8 string handling
        [DllImport("portaudio_x64.dll")]
        private static extern IntPtr Pa_GetDeviceInfo(int device);

        [StructLayout(LayoutKind.Sequential)]
        private struct PaDeviceInfo
        {
            public int structVersion;
            public IntPtr name;  // const char* - UTF-8 string
            public int hostApi;
            public int maxInputChannels;
            public int maxOutputChannels;
            public double defaultLowInputLatency;
            public double defaultLowOutputLatency;
            public double defaultHighInputLatency;
            public double defaultHighOutputLatency;
            public double defaultSampleRate;
        }

        // Helper method to safely get device name as UTF-8
        private static string GetDeviceNameUTF8(int deviceIndex)
        {
            try
            {
                IntPtr deviceInfoPtr = Pa_GetDeviceInfo(deviceIndex);
                if (deviceInfoPtr == IntPtr.Zero)
                    return string.Empty;

                PaDeviceInfo deviceInfo = Marshal.PtrToStructure<PaDeviceInfo>(deviceInfoPtr);
                if (deviceInfo.name == IntPtr.Zero)
                    return string.Empty;

                // Read the string as UTF-8
                int length = 0;
                while (Marshal.ReadByte(deviceInfo.name, length) != 0)
                    length++;

                byte[] buffer = new byte[length];
                Marshal.Copy(deviceInfo.name, buffer, 0, length);
                return Encoding.UTF8.GetString(buffer);
            }
            catch
            {
                return string.Empty;
            }
        }

        public Form1()
        {
            InitializeComponent();

            // Make the status bar label expand to fill the window width so messages are visible without resizing.
            toolStripStatusLabel1.Spring = true;
            toolStripStatusLabel1.AutoSize = false;
            toolStripStatusLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Use the same icon as the installer/executable to keep the taskbar icon consistent.
            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "flexasiogui.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new System.Drawing.Icon(iconPath);
                }
            }
            catch
            {
                // Ignore failures; fallback to default icon.
            }

            this.Text = $"FlexASIO GUI v{flexasioGuiVersion}";

            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // get the value of the "Language for non-Unicode programs" setting (1252 for English)
            // note: in Win11 this could be UTF-8 already, since it's natively supported
            legacyEncoding = Encoding.GetEncoding((int)GetACP());

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            CultureInfo.DefaultThreadCurrentCulture = customCulture;
            CultureInfo.DefaultThreadCurrentUICulture = customCulture;

            TOMLPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\{tomlName}";

            // Keep C# property names as-is when serializing/deserializing TOML (no case conversion)
            tomlModelOptions.ConvertPropertyName = (string name) => name;

            // Parse the TOML; if parsing fails, show a message so users can fix the file.
            var (config, tomlError) = FlexAsioToml.ParseWithError(TOMLPath, tomlModelOptions);
            if (!string.IsNullOrEmpty(tomlError))
            {
                SetStatusMessage($"Failed to parse {tomlName}: {tomlError}", isError: true);
            }

            flexGUIConfig = config;

            // Initialize UI controls (backend list, device list, etc.) from the loaded/parsed config.
            LoadFlexASIOConfig(TOMLPath);

            InitDone = true;

            if (TryLoadFlexASIODll(out string dllError, out string dllPathTried))
            {
                SetStatusMessage($"FlexASIO GUI for FlexASIO {flexasioVersion} started ({Configuration.VersionString}); loaded FlexASIO.dll from {dllPathTried}.");
            }
            else
            {
                SetStatusMessage($"FlexASIO GUI started ({Configuration.VersionString}); failed to load FlexASIO.dll (tried {dllPathTried}): {dllError}", isError: true);
            }


            GenerateOutput();
        }

        private FlexGUIConfig LoadFlexASIOConfig(string tomlPath)
        {
            flexGUIConfig = new FlexGUIConfig();
            if (File.Exists(tomlPath))
            {
                var tomlPathAsText = File.ReadAllText(tomlPath);
                flexGUIConfig = FlexAsioToml.Parse(tomlPathAsText, tomlModelOptions);
            }

            numericBufferSize.Maximum = 8192;
            numericBufferSize.Increment = 16;

            numericLatencyInput.Increment = 0.1m;
            numericLatencyOutput.Increment = 0.1m;

            for (var i = 0; i < Configuration.HostApiCount; i++)
            {
                comboBackend.Items.Add(Configuration.GetHostApiInfo(i).name);
            }

            if (comboBackend.Items.Contains(flexGUIConfig.backend))
            {
                comboBackend.SelectedIndex = comboBackend.Items.IndexOf(flexGUIConfig.backend);
            }
            else
            {
                comboBackend.SelectedIndex = 0;
            }

            if (flexGUIConfig.bufferSizeSamples != null)
                numericBufferSize.Value = (Int64)flexGUIConfig.bufferSizeSamples;
            checkBoxSetBufferSize.Checked = numericBufferSize.Enabled = flexGUIConfig.bufferSizeSamples != null;

            treeDevicesInput.SelectedNode = treeDevicesInput.Nodes.Cast<TreeNode>().FirstOrDefault(x => x.Text == (flexGUIConfig.input.device == "" ? "(None)" : flexGUIConfig.input.device));
            treeDevicesOutput.SelectedNode = treeDevicesOutput.Nodes.Cast<TreeNode>().FirstOrDefault(x => x.Text == (flexGUIConfig.output.device == "" ? "(None)" : flexGUIConfig.output.device));

            checkBoxSetInputLatency.Checked = numericLatencyInput.Enabled = flexGUIConfig.input.suggestedLatencySeconds != null;
            checkBoxSetOutputLatency.Checked = numericLatencyOutput.Enabled = flexGUIConfig.output.suggestedLatencySeconds != null;

            if (flexGUIConfig.input.suggestedLatencySeconds != null)
                numericLatencyInput.Value = (decimal)(double)flexGUIConfig.input.suggestedLatencySeconds;
            if (flexGUIConfig.output.suggestedLatencySeconds != null)
                numericLatencyOutput.Value = (decimal)(double)flexGUIConfig.output.suggestedLatencySeconds;

            numericChannelsInput.Value = (decimal)(flexGUIConfig.input.channels ?? 0);
            numericChannelsOutput.Value = (decimal)(flexGUIConfig.output.channels ?? 0);

            checkBoxWasapiInputSet.Checked = flexGUIConfig.input.wasapiExclusiveMode != null || flexGUIConfig.input.wasapiAutoConvert != null;
            checkBoxWasapiOutputSet.Checked = flexGUIConfig.output.wasapiExclusiveMode != null || flexGUIConfig.output.wasapiAutoConvert != null;

            wasapiExclusiveInput.Enabled = flexGUIConfig.input.wasapiExclusiveMode != null;
            wasapiExclusiveInput.Checked = flexGUIConfig.input.wasapiExclusiveMode ?? false;
            wasapiExclusiveOutput.Enabled = flexGUIConfig.output.wasapiExclusiveMode != null;
            wasapiExclusiveOutput.Checked = flexGUIConfig.output.wasapiExclusiveMode ?? false;

            wasapiAutoConvertInput.Enabled = flexGUIConfig.input.wasapiAutoConvert != null;
            wasapiAutoConvertInput.Checked = flexGUIConfig.input.wasapiAutoConvert ?? false;
            wasapiAutoConvertOutput.Enabled = flexGUIConfig.output.wasapiAutoConvert != null;
            wasapiAutoConvertOutput.Checked = flexGUIConfig.output.wasapiAutoConvert ?? false;
            return flexGUIConfig;
        }

        private string DescrambleUTF8(string s)
        {
            // portaudio incorrectly returns UTF-8 strings as if they were ANSI (CP1252 for most Latin systems, CP1251 for Cyrillic, etc...)
            // this line fixes the issue by reading the input as CP* and parsing it as UTF-8
            var bytes = legacyEncoding.GetBytes(s);
            return Encoding.UTF8.GetString(bytes);
        }

        private TreeNode[] GetDevicesForBackend(string Backend, bool Input)
        {
            List<TreeNode> treeNodes = new List<TreeNode>();
            treeNodes.Add(new TreeNode("(None)"));
            for (var i = 0; i < Configuration.DeviceCount; i++)
            {
                var deviceInfo = Configuration.GetDeviceInfo(i);

                var apiInfo = Configuration.GetHostApiInfo(deviceInfo.hostApi);

                if (apiInfo.name != Backend)
                    continue;

                // Use direct P/Invoke to get UTF-8 device name
                string deviceName = GetDeviceNameUTF8(i);

                // Fallback to the old method if P/Invoke fails
                if (string.IsNullOrEmpty(deviceName))
                {
                    deviceName = DescrambleUTF8(deviceInfo.name);
                }

                if (Input == true)
                {
                    if (deviceInfo.maxInputChannels > 0)
                    {
                        treeNodes.Add(new TreeNode(deviceName));
                    }
                }
                else
                {
                    if (deviceInfo.maxOutputChannels > 0)
                    {
                        treeNodes.Add(new TreeNode(deviceName));
                    }
                }
            }
            return treeNodes.ToArray();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void comboBackend_SelectedIndexChanged(object sender, EventArgs e)
        {
            var o = sender as ComboBox;
            if (o != null)
            {
                var selectedBackend = o.SelectedItem as string;
                RefreshDevices(selectedBackend);
                if (selectedBackend == "(None)") selectedBackend = "";
                flexGUIConfig.backend = selectedBackend;
                GenerateOutput();
            }
        }

        private void RefreshDevices(string selectedBackend)
        {
            var tmpInput = treeDevicesInput.SelectedNode;
            var tmpOutput = treeDevicesOutput.SelectedNode;
            if (selectedBackend != null)
            {
                treeDevicesInput.Nodes.Clear();
                treeDevicesInput.Nodes.AddRange(GetDevicesForBackend(selectedBackend, true));
                for (int i = 0; i < treeDevicesInput.Nodes.Count; i++)
                {
                    if (treeDevicesInput?.Nodes[i].Text == tmpInput?.Text)
                    {
                        treeDevicesInput.SelectedNode = treeDevicesInput.Nodes[i];
                        break;
                    }
                }

                treeDevicesOutput.Nodes.Clear();
                treeDevicesOutput.Nodes.AddRange(GetDevicesForBackend(selectedBackend, false));
                for (int i = 0; i < treeDevicesOutput.Nodes.Count; i++)
                {
                    if (treeDevicesOutput?.Nodes[i].Text == tmpOutput?.Text)
                    {
                        treeDevicesOutput.SelectedNode = treeDevicesOutput.Nodes[i];
                        break;
                    }
                }
            }
        }

        private void GenerateOutput()
        {
            if (!InitDone) return;

            if (!checkBoxSetBufferSize.Checked) flexGUIConfig.bufferSizeSamples = null;
            if (!checkBoxSetInputLatency.Checked) flexGUIConfig.input.suggestedLatencySeconds = null;
            if (!checkBoxSetOutputLatency.Checked) flexGUIConfig.output.suggestedLatencySeconds = null;
            if (!checkBoxWasapiInputSet.Checked)
            {
                flexGUIConfig.input.wasapiAutoConvert = null;
                flexGUIConfig.input.wasapiExclusiveMode = null;
            }
            if (!checkBoxWasapiOutputSet.Checked)
            {
                flexGUIConfig.output.wasapiAutoConvert = null;
                flexGUIConfig.output.wasapiExclusiveMode = null;
            }

            configOutput.Clear();
            configOutput.Text = Toml.FromModel(flexGUIConfig, options: tomlModelOptions);
        }



        private void SetStatusMessage(string msg, bool isError = false)
        {
            toolStripStatusLabel1.ForeColor = isError ? System.Drawing.Color.DarkRed : System.Drawing.SystemColors.ControlText;
            toolStripStatusLabel1.Text = $"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {msg}";
            toolStripStatusLabel1.ToolTipText = msg;
        }

        private void btClipboard_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(configOutput.Text);
            SetStatusMessage("Configuration copied to Clipboard");
        }

        private void btSaveToProfile_Click(object sender, EventArgs e)
        {
            File.WriteAllText(TOMLPath, configOutput.Text);
            SetStatusMessage($"Configuration written to {TOMLPath}");
        }

        private void btSaveAs_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            saveFileDialog.FileName = tomlName;
            var ret = saveFileDialog.ShowDialog();
            if (ret == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, configOutput.Text);
            }
            SetStatusMessage($"Configuration written to {saveFileDialog.FileName}");
        }

        private void treeDevicesInput_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (sender == null) return;
            else
            {
                e.Node.Checked = true;
                this.onTreeViewSelected(eventArgs: e, forInput: true);
            }
        }

        private void treeDevicesOutput_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (sender == null) return;
            else
            {
                e.Node.Checked = true;
                this.onTreeViewSelected(eventArgs: e, forInput: false);
            }
        }

        private void treeDevicesOutput_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (sender == null) return;
            else
            {
                this.onTreeViewSelected(eventArgs: e, forInput: false);
            }
        }

        private void treeDevicesInput_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (sender == null) return;
            else
            {
                this.onTreeViewSelected(eventArgs: e, forInput: true);
            }
        }

        private void unCheckAllOthers(TreeNode treeNode)
        {
            foreach (TreeNode node in treeNode.TreeView.Nodes)
            {
                if (node != treeNode)
                {
                    node.Checked = false;
                }
            }
        }

        private void onTreeViewSelected(TreeViewEventArgs eventArgs, bool forInput)
        {
            if (eventArgs.Node.Checked == true)
            {
                if (forInput == true)
                    flexGUIConfig.input.device = eventArgs.Node.Text == "(None)" ? "" : eventArgs.Node.Text;
                else
                    flexGUIConfig.output.device = eventArgs.Node.Text == "(None)" ? "" : eventArgs.Node.Text;
                this.unCheckAllOthers(eventArgs.Node);
                GenerateOutput();
            }
        }

        private void numericChannelsOutput_ValueChanged(object sender, EventArgs e)
        {
            var o = sender as NumericUpDown;
            if (o == null) return;
            flexGUIConfig.output.channels = (o.Value > 0 ? (int?)o.Value : null);
            GenerateOutput();
        }

        private void numericChannelsInput_ValueChanged(object sender, EventArgs e)
        {
            var o = sender as NumericUpDown;
            if (o == null) return;
            flexGUIConfig.input.channels = (o.Value > 0 ? (int?)o.Value : null);
            GenerateOutput();
        }

        private void numericLatencyOutput_ValueChanged(object sender, EventArgs e)
        {
            var o = sender as NumericUpDown;
            if (o == null) return;
            if (checkBoxSetOutputLatency.Enabled)
            {
                flexGUIConfig.output.suggestedLatencySeconds = (o.Value > 0 ? (double)o.Value : 0);
                GenerateOutput();
            }
        }

        private void numericLatencyInput_ValueChanged(object sender, EventArgs e)
        {
            var o = sender as NumericUpDown;
            if (o == null) return;
            if (checkBoxSetInputLatency.Enabled)
            {
                flexGUIConfig.input.suggestedLatencySeconds = (o.Value > 0 ? (double)o.Value : 0);
                GenerateOutput();
            }
        }

        private void wasapiAutoConvertOutput_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            flexGUIConfig.output.wasapiAutoConvert = o.Checked;
            GenerateOutput();
        }

        private void wasapiExclusiveOutput_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            flexGUIConfig.output.wasapiExclusiveMode = o.Checked;
            GenerateOutput();
        }

        private void wasapiAutoConvertInput_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            flexGUIConfig.input.wasapiAutoConvert = o.Checked;
            GenerateOutput();
        }

        private void wasapiExclusiveInput_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            flexGUIConfig.input.wasapiExclusiveMode = o.Checked;
            GenerateOutput();
        }

        private void numericBufferSize_ValueChanged(object sender, EventArgs e)
        {
            var o = sender as NumericUpDown;
            if (o == null) return;
            flexGUIConfig.bufferSizeSamples = (o.Value > 0 ? (int)o.Value : 0);
            GenerateOutput();
        }


        private void checkBoxSetInputLatency_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            numericLatencyInput.Enabled = o.Checked;
            if (o.Checked == false)
            {
                flexGUIConfig.input.suggestedLatencySeconds = null;
            }
            else
            {
                numericLatencyInput_ValueChanged(numericLatencyInput, null);
            }
            GenerateOutput();
        }

        private void checkBoxSetOutputLatency_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            numericLatencyOutput.Enabled = o.Checked;
            if (o.Checked == false) {
                flexGUIConfig.output.suggestedLatencySeconds = null;
            }
            else
            {
                numericLatencyOutput_ValueChanged(numericLatencyOutput, null);
            }
            GenerateOutput();
        }


        private void checkBoxSetBufferSize_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            numericBufferSize.Enabled = o.Checked;
            if (o.Checked == false) {
                flexGUIConfig.bufferSizeSamples = null;
            }
            else
            {
                numericBufferSize_ValueChanged(numericBufferSize, null);
            }
            GenerateOutput();
        }

        private void checkBoxWasapiInputSet_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            wasapiAutoConvertInput.Enabled = o.Checked;
            wasapiExclusiveInput.Enabled = o.Checked;

            if (o.Checked == false)
            {
                flexGUIConfig.input.wasapiAutoConvert = null;
                flexGUIConfig.input.wasapiExclusiveMode = null;
            }
            else
            {
                flexGUIConfig.input.wasapiAutoConvert = wasapiAutoConvertInput.Checked;
                flexGUIConfig.input.wasapiExclusiveMode = wasapiExclusiveInput.Checked;
            }
            GenerateOutput();
        }

        private void checkBoxWasapOutputSet_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            wasapiAutoConvertOutput.Enabled = o.Checked;
            wasapiExclusiveOutput.Enabled = o.Checked;

            if (o.Checked == false)
            {
                flexGUIConfig.output.wasapiAutoConvert = null;
                flexGUIConfig.output.wasapiExclusiveMode = null;
            }
            else
            {
                flexGUIConfig.output.wasapiAutoConvert = wasapiAutoConvertOutput.Checked;
                flexGUIConfig.output.wasapiExclusiveMode = wasapiExclusiveOutput.Checked;
            }
            GenerateOutput();
        }

        private void btRefreshDevices_Click(object sender, EventArgs e)
        {
            var selectedBackend = comboBackend.SelectedItem as string;
            RefreshDevices(selectedBackend);
        }

        private void linkLabelDocs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo(docUrl) { UseShellExecute = true });
        }

        private void btLoadFrom_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            openFileDialog.FileName = tomlName;
            openFileDialog.Filter = "FlexASIO Config (*.toml)|*.toml";
            openFileDialog.CheckFileExists = true;
            var ret = openFileDialog.ShowDialog();
            if (ret == DialogResult.OK)
            {
                try
                {
                    this.LoadFlexASIOConfig(openFileDialog.FileName);
                }
                catch (Exception)
                {
                    SetStatusMessage($"Error loading from {openFileDialog.FileName}");
                    this.LoadFlexASIOConfig(TOMLPath);
                    return;
                }

            }
            SetStatusMessage($"Configuration loaded from {openFileDialog.FileName}");
        }

        private void btLocateFlexASIO_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog();
            dlg.Filter = "FlexASIO.dll|FlexASIO.dll";
            dlg.Title = "Locate FlexASIO.dll";
            dlg.CheckFileExists = true;
            dlg.Multiselect = false;

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            string folder = Path.GetDirectoryName(dlg.FileName);
            WriteInstallPathToRegistry(folder);

            ResetFlexAsioModule();
            if (TryLoadFlexASIODll(out string err2, out string dllPath2))
            {
                SetStatusMessage($"Manually selected FlexASIO.dll loaded from {dllPath2}.");
                GenerateOutput();
            }
            else
            {
                SetStatusMessage($"Failed to load FlexASIO.dll from selected folder ({folder}): {err2}", isError: true);
            }
        }
    }
}
