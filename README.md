# FlexASIO GUI (fork of flipswitchingmonkey/FlexASIO_GUI)

⚠️ **This repository is a fork/continuation of `flipswitchingmonkey/FlexASIO_GUI`** (https://github.com/flipswitchingmonkey/FlexASIO_GUI).

This fork is maintained by `Rutice` (https://github.com/ruticejp) and includes ongoing improvements such as .NET 10/11 support, installer updates, and UTF‑8 handling fixes. The original FlexASIO project (the underlying audio driver) is authored by `dechamps`, and that work is credited and respected.

This is a small GUI to make the configuration of https://github.com/dechamps/FlexASIO a bit quicker.

It should pick up your existing $Usersprofile/FlexASIO.toml file and read the basic parameters. Not all of them have been implemented yet...

To run, please make sure you have [.NET Desktop Runtime 10.x (or higher)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed.

## What this GUI does for you

- Reads your existing `$UsersProfile/FlexASIO.toml` and generates a configuration snippet you can copy.
- Automatically finds your FlexASIO installation, even if it is not in the standard location.
- Detects missing native dependencies (e.g., Visual C++ runtimes) and provides actionable hints.

> ⚠️ This project targets **.NET 10 (net10.0-windows)** only. Preview versions (e.g., .NET 11) are not supported.
>
> 🧪 **Automated tests** are included (xUnit) and run via CI to validate key logic such as FlexASIO DLL discovery.

## Install path detection (how the GUI finds FlexASIO)

The GUI does not hard-code a fixed path to `FlexASIO.dll`. Instead it follows a deterministic search order so users can understand and troubleshoot how the install location is found.

### 1) Registry keys written by the installer (highest priority)

The installer writes the install location into the following registry key(s) under `HKEY_LOCAL_MACHINE`:

- `SOFTWARE\Fabrikat\FlexASIOGUI_Rutice\Install\InstallPath`
- `SOFTWARE\Fabrikat\FlexASIOGUI\Install\InstallPath` (original upstream key)

If either key exists and contains a valid folder path, the GUI uses that as the FlexASIO install location.

> ✅ This is the preferred mechanism. The installer already writes these keys so the GUI can reliably locate FlexASIO.

### 2) Common install folders (official installer defaults)

If no valid registry key is found, the GUI looks for FlexASIO in the standard locations:

- `%ProgramFiles%\FlexASIO`
- `%ProgramFiles(x86)%\FlexASIO`

### 3) Search for `FlexASIO.exe` under `%ProgramFiles%`

If neither the registry nor the common folders contain FlexASIO, the GUI scans `%ProgramFiles%` recursively for `FlexASIO.exe` and uses the folder containing it as the install location.

### 4) Fallback: scan for `FlexASIO.dll` and pick the best candidate

As a last resort, the GUI scans under `%ProgramFiles%` for any `FlexASIO.dll`, then chooses the most suitable candidate (preferring x64 builds and the highest file version).

### Manual selection (if auto-detection fails)

If the GUI cannot locate FlexASIO automatically (e.g. because the install path is non-standard or the registry key is missing), you can manually point it to `FlexASIO.dll`:

- Click **Locate** in the GUI.
- Select the `FlexASIO.dll` file in your FlexASIO installation folder.

The GUI will store the selected folder in the same registry key used by the installer, so it will be remembered on next launch.

## If it fails to load FlexASIO.dll

The GUI will show a message in the status bar containing:

- the **path it tried**
- the **reason it failed**
- a **list of missing DLL dependencies**, and
- a **hint** about installing the appropriate Visual C++ redistributable if needed.

v0.36 adds a registry key with the install path to:

- `HKEY_LOCAL_MACHINE\SOFTWARE\Fabrikat\FlexASIOGUI\Install\InstallPath`
- `HKEY_LOCAL_MACHINE\SOFTWARE\Fabrikat\FlexASIOGUI_Rutice\Install\InstallPath` (fork-specific)

It also makes most settings optional so that default settings are not overwritten.

## Troubleshooting (common issues)

### 1) Paths containing non-ASCII characters

Some native Windows DLL loaders and drivers can fail to load DLLs when the install path contains non-ASCII characters (e.g. Japanese or full-width characters).

- If possible, install FlexASIO into an ASCII-only path such as `C:\Program Files\FlexASIO`.
- If you already have a problematic path, try reinstalling under `%ProgramFiles%`.

### 2) Administrator privileges (registry / driver loading)

- The installer writes to `HKLM`. If you did not run the installer as an administrator, the registry key may not be created.
- Loading a driver or `FlexASIO.dll` may also require administrator privileges.

### 3) 32-bit vs 64-bit mismatch

- This GUI is a 64-bit application.
- A 32-bit `FlexASIO.dll` cannot be loaded (or will fail with dependency errors).
- Use a 64-bit build of FlexASIO or ensure you are loading the correct architecture.

### 4) Missing dependencies (Visual C++ redistributables)

- If the status bar shows **missing DLL dependencies**, install the appropriate Visual C++ redistributable package.
- Common missing DLLs include:
  - `msvcp140.dll`, `vcruntime140.dll`, etc.

### 5) Antivirus / security software interference

- Some security products may block loading `FlexASIO.dll`.
- Try adding an exception or temporarily disabling the security software to verify whether it is the cause.

### 6) TOML syntax errors

- If `FlexASIO.toml` has a syntax error or invalid value, the GUI may fail to load the config.
- The status bar will show the error message; use that to correct the file.

![image](https://user-images.githubusercontent.com/6930367/118895016-a4746a80-b905-11eb-806c-7fd3fee4fcd1.png)
