using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlexASIOGUI
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Ensure only one instance of the GUI is running at a time.
            // Use a Local mutex so the mutex is only visible within the current session.
            const string mutexName = "Local\\FlexASIOGUI";
            using (var mutex = new System.Threading.Mutex(initiallyOwned: true, name: mutexName, createdNew: out bool createdNew))
            {
                if (!createdNew)
                {
                    // Try to activate the already-running window.
                    try
                    {
                        var current = Process.GetCurrentProcess();
                        var other = Process.GetProcessesByName(current.ProcessName)
                            .FirstOrDefault(p => p.Id != current.Id && p.MainWindowHandle != IntPtr.Zero);
                        if (other != null)
                        {
                            IntPtr handle = other.MainWindowHandle;
                            ShowWindow(handle, SW_RESTORE);
                            SetForegroundWindow(handle);
                        }
                    }
                    catch
                    {
                        // Best effort only.
                    }

                    MessageBox.Show(
                        "FlexASIO GUI is already running.",
                        "FlexASIO GUI",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }
    }
}
