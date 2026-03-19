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

## 2026-03-18

- Added a single-instance guard using a local mutex to prevent multiple GUI windows from running simultaneously.
- When a second launch is attempted, the existing window is restored and brought to the foreground.
- Made the taskbar icon consistent with the installer by bundling and loading `flexasiogui.ico` at runtime.
- Built Debug and Release configurations to verify both build targets succeed.

---

*This changelog was created and maintained by Rutice ([ruticejp](https://github.com/ruticejp)).*
