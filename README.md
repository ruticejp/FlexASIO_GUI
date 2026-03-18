⚠️ **This repository is a fork/continuation of `flipswitchingmonkey/FlexASIO_GUI`** (https://github.com/flipswitchingmonkey/FlexASIO_GUI).

This fork is maintained by `Rutice` (https://github.com/ruticejp) and includes ongoing improvements such as .NET 10/11 support, installer updates, and UTF‑8 handling fixes. The original FlexASIO project (the underlying audio driver) is authored by `dechamps`, and that work is credited and respected.

This is a small GUI to make the configuration of https://github.com/dechamps/FlexASIO a bit quicker.

It should pick up your existing $Usersprofile/FlexASIO.toml file and read the basic parameters. Not all of them have been implemented yet...

To run, please make sure you have [.NET Desktop Runtime 10.x (or higher)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed.

### What this GUI does for you
- Reads your existing `$UsersProfile/FlexASIO.toml` and generates a configuration snippet you can copy.
- Automatically finds your FlexASIO installation, even if it is not in the standard location.
- Detects missing native dependencies (e.g., Visual C++ runtimes) and provides actionable hints.

### How it finds FlexASIO
This GUI tries to locate `FlexASIO.dll` using a chain of strategies (in priority order):
1. **Read the installer-written registry key** (`HKLM\SOFTWARE\Fabrikat\FlexASIOGUI_Rutice\Install\InstallPath`, then the upstream key).
2. Search in **common install paths** (`%ProgramFiles%\FlexASIO`, `%ProgramFiles(x86)%\FlexASIO`).
3. Search for **`FlexASIO.exe`** anywhere under `%ProgramFiles%` and use its folder.
4. Search for **any `FlexASIO.dll` under the install directory** and choose the best candidate (64-bit + highest version).

### If it fails to load FlexASIO.dll
The GUI will show a message in the status bar containing:
- the **path it tried**
- the **reason it failed**
- a **list of missing DLL dependencies**, and
- a **hint** about installing the appropriate Visual C++ redistributable if needed.

v0.36 adds a registry key with the install path to:
- `HKEY_LOCAL_MACHINE\SOFTWARE\Fabrikat\FlexASIOGUI\Install\InstallPath`
- `HKEY_LOCAL_MACHINE\SOFTWARE\Fabrikat\FlexASIOGUI_Rutice\Install\InstallPath` (fork-specific)

It also makes most settings optional so that default settings are not overwritten.

![image](https://user-images.githubusercontent.com/6930367/118895016-a4746a80-b905-11eb-806c-7fd3fee4fcd1.png)
