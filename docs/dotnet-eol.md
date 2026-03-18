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

## Notes

- Some APIs or behaviors may change in newer frameworks.
- Refer to official documentation and migration guides for details.

## Reference Links

- [.NET Support Policy](https://aka.ms/dotnet-core-support)
- [.NET 8.0 Release Notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8)
- [Migration Guide](https://learn.microsoft.com/dotnet/core/porting/)

---

*Supplementary note by Rutice ([ruticejp](https://github.com/ruticejp))*
