# .NET Target Framework EOL (End Of Life) Notice

## Current Status

This project now targets `net10.0-windows` as its primary framework (with optional `net11.0-windows` support in preview).

- `net6.0-windows` reached End Of Life (EOL) in November 2024.
- No further security updates or bug fixes will be provided for that version.
- For details, see the official [.NET Support Policy](https://aka.ms/dotnet-core-support).

## Recommended Actions

To ensure continued security and stability, it is recommended to:

1. **Migrate to the latest supported framework**
    - Example: `net10.0-windows` (or `net11.0-windows` for preview builds)
2. **Migration Steps**
    - Change `<TargetFramework>` (or `<TargetFrameworks>`) in `FlexASIOGUI.csproj` to `net10.0-windows` (and/or `net11.0-windows`)
    - Check compatibility of dependencies and APIs
    - Install the matching .NET SDK (e.g., .NET 10 preview) and build the application
    - Test the application

## Build & Tooling Notes

- A `global.json` file can be used to pin the SDK version for this repo (recommended for consistent CI builds).
- Use `dotnet --list-sdks` to confirm which SDKs are installed.
- If you build for `net11.0-windows`, you may see a notice about using preview SDKs; this is expected while the runtime is in preview.
- The installer script (`installer/FlexASIOGUI.iss`) is configured to package the `bin\Release\net10.0-windows\` output. If you switch to `net11.0-windows`, update the installer path or use a build variable.

## SDK Download Links

- [.NET 10 SDK (stable)](https://dotnet.microsoft.com/download/dotnet/10.0)
- [.NET 11 SDK (preview)](https://dotnet.microsoft.com/download/dotnet/11.0)

## Notes

- Some APIs or behaviors may change in newer frameworks.
- Refer to official documentation and migration guides for details.

## Reference Links

- [.NET Support Policy](https://aka.ms/dotnet-core-support)
- [.NET 8.0 Release Notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8)
- [Migration Guide](https://learn.microsoft.com/dotnet/core/porting/)

---

*Supplementary note by Rutice ([ruticejp](https://github.com/ruticejp))*
