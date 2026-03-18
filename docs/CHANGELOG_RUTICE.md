# Change Log (Rutice/ruticejp)

This document summarizes the key changes and maintenance actions performed by Rutice ([ruticejp](https://github.com/ruticejp)) for this repository.

---

## 2025-11-19

- Changed default git branch from `master` to `main` and deleted the old `master` branch.
- Created branch `fix-utf8-encoding` for minimal UTF-8 encoding fixes (Japanese audio device names).
- Added English documentation: `dotnet-eol.md` (.NET 6.0 EOL notice and migration recommendations), `fix-utf8-encoding.md` (Purpose and scope of UTF-8 encoding fix branch), `ENCODING_FIX.md` (Details of the UTF-8 encoding fix implementation)
- Added supplementary notes to documentation files, clarifying authorship and contact info.
- Built Debug and Release configurations, confirmed .pdb exclusion in Release builds.
- Updated `.gitignore` to explicitly exclude `.pdb` files for Debug/Release builds.
- Configured `.csproj` to prevent `.pdb` generation in Release builds.
- Cleaned up `bin` directory and rebuilt both Debug and Release outputs.

## 2026-03-18 (single-instance-mutex)

- Added a single-instance guard using a local mutex to prevent multiple GUI windows from running simultaneously.
- When a second launch is attempted, the existing window is restored and brought to the foreground.
- Made the taskbar icon consistent with the installer by bundling and loading `flexasiogui.ico` at runtime.
- Built Debug and Release configurations to verify both build targets succeed.

---

## 2026-03-18 (upgrade-dotnet10)

> This project is a continuation fork of the original `dechamps/FlexASIO_GUI` repository. The original author is credited and respected; this fork exists to enable ongoing maintenance and improvements when upstream changes were not merged.

- Upgraded project to **.NET 10.0 (and optional .NET 11 preview)** by updating `FlexASIOGUI.csproj` and adding `global.json`.
- Updated documentation and installer script to reference .NET 10 build output (`net10.0-windows`).
- Removed unused package references (`Microsoft.Win32.Registry`, `System.Text.Encoding.CodePages`) to eliminate `NU1510` warnings.
- Verified successful Debug/Release builds for both `net10.0-windows` and `net11.0-windows`.

## 2026-03-18 (DLL loading / dependency stability)

- Implemented robust automatic detection of `FlexASIO.dll`:
  - Reads installer-written registry keys (fork-specific + original)
  - Falls back to common install paths and searches for `FlexASIO.exe`
  - Scans install directory for any `FlexASIO.dll` and selects the best candidate (x64 + highest file version)
- Added runtime dependency inspection:
  - Detects missing import DLLs and displays them in the status message
  - Includes actionable hints (e.g., install Visual C++ Redistributable)
- Improved status bar messaging to show:
  - which DLL path was tried
  - why loading failed (including missing exports or missing dependencies)

---

*This changelog is maintained by Rutice ([ruticejp](https://github.com/ruticejp)).*
