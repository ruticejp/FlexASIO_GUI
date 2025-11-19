# .NET Target Framework EOL (End Of Life) Notice

## Current Status

This project currently targets `net6.0-windows` as its framework.

- `net6.0-windows` reached End Of Life (EOL) in November 2024.
- No further security updates or bug fixes will be provided for this version.
- For details, see the official [.NET Support Policy](https://aka.ms/dotnet-core-support).

## Recommended Actions

To ensure continued security and stability, it is recommended to:

1. **Migrate to the latest LTS (Long Term Support) framework**
    - Example: `net8.0-windows`
2. **Migration Steps**
    - Change `<TargetFramework>` in `FlexASIOGUI.csproj` to `net8.0-windows`
    - Check compatibility of dependencies and APIs
    - Build and test the application

## Notes

- Some APIs or behaviors may change in newer frameworks.
- Refer to official documentation and migration guides for details.

## Reference Links

- [.NET Support Policy](https://aka.ms/dotnet-core-support)
- [.NET 8.0 Release Notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8)
- [Migration Guide](https://learn.microsoft.com/dotnet/core/porting/)

---

*Supplementary note by Rutice ([ruticejp](https://github.com/ruticejp))*
