# .NET Target Framework EOL (End Of Life) Notice

## Current Status

This project now targets `net10.0-windows` only.

- `net6.0-windows` reached End Of Life (EOL) in November 2024.
- No further security updates or bug fixes will be provided for that version.
- For details, see the official [.NET Support Policy](https://aka.ms/dotnet-core-support).

## Recommended Actions

To ensure continued security and stability, it is recommended to:

1. **Use the supported framework**
    - Target: `net10.0-windows`
2. **Migration Steps**
    - Change `<TargetFramework>` in `FlexASIOGUI.csproj` to `net10.0-windows`
    - Check compatibility of dependencies and APIs
    - Install the matching .NET SDK and build the application
    - Test the application

## Build & Tooling Notes

- A `global.json` file can be used to pin the SDK version for this repo (recommended for consistent CI builds).
- Use `dotnet --list-sdks` to confirm which SDKs are installed.
- The installer script (`installer/FlexASIOGUI.iss`) is configured to package the `bin\Release\net10.0-windows\` output.

## SDK Download Links

- [.NET 10 SDK (stable)](https://dotnet.microsoft.com/download/dotnet/10.0)

## Notes

- Some APIs or behaviors may change in newer frameworks.
- Refer to official documentation and migration guides for details.

## Reference Links

- [.NET Support Policy](https://aka.ms/dotnet-core-support)
- [.NET 8.0 Release Notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8)
- [Migration Guide](https://learn.microsoft.com/dotnet/core/porting/)

---

*Supplementary note by Rutice ([ruticejp](https://github.com/ruticejp))*
