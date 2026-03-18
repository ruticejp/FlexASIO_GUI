# UTF-8 Encoding Fix for Audio Device Names

## Problem

The original FlexASIO GUI had character encoding issues when displaying audio device names containing non-ASCII characters. Specifically:

- **Japanese** device names like `スピーカー` (Speaker) and `マイク` (Microphone) were displayed as garbled text: `繝槭う繧ｯ`, `繧ｹ繝斐・繧ｫ繝ｼ`
- This issue affected all languages using non-ASCII characters

## Root Cause

The problem was caused by the `portaudio-sharp` wrapper library incorrectly interpreting UTF-8 encoded strings from the native PortAudio C library:

1. PortAudio C API returns device names as UTF-8 encoded `const char*` strings
2. The `portaudio-sharp` managed wrapper uses default marshaling, which interprets the strings as ANSI (system code page)
3. On Japanese Windows (CP932/Shift-JIS), UTF-8 bytes were misinterpreted as Shift-JIS, causing mojibake (garbled text)

## Solution

We implemented a **P/Invoke (Platform Invocation Services)** approach to directly call the native PortAudio API and correctly decode UTF-8 strings:

### Implementation

```csharp
// Direct P/Invoke declaration to PortAudio C API
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

// Helper method to safely read UTF-8 device names
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
        return Encoding.UTF8.GetString(buffer);  // Correct UTF-8 decoding
    }
    catch
    {
        return string.Empty;
    }
}
```

### Key Points

1. **Direct API Call**: Bypasses the `portaudio-sharp` wrapper entirely
2. **Manual UTF-8 Decoding**: Reads raw bytes and uses `Encoding.UTF8.GetString()` for proper decoding
3. **Fallback Support**: Falls back to the original `DescrambleUTF8` method if P/Invoke fails

## Language Support

This fix supports **all languages that use non-ASCII characters**, including but not limited to:

### Verified Languages

- ✅ **Japanese** (日本語) - e.g., `スピーカー`, `マイク`

### Expected to Work

- ✅ **Korean** (한국어) - e.g., `스피커`, `마이크`
- ✅ **Simplified Chinese** (简体中文) - e.g., `扬声器`, `麦克风`
- ✅ **Traditional Chinese** (繁體中文) - e.g., `揚聲器`, `麥克風`
- ✅ **Cyrillic** (Русский) - e.g., `Динамики`, `Микрофон`
- ✅ **Arabic** (العربية) - RTL (right-to-left) text
- ✅ **Hebrew** (עברית) - RTL (right-to-left) text
- ✅ **Thai** (ไทย) - Complex script
- ✅ **Greek** (Ελληνικά)
- ✅ **All other Unicode-supported languages**

### Technical Reason

Since the fix uses standard UTF-8 decoding (`Encoding.UTF8.GetString()`), it supports **all Unicode character sets** (covering 149+ writing systems). UTF-8 is a universal encoding that can represent any Unicode character.

## Before and After

### Before (Garbled Japanese Text)

```text
Device: 繝槭う繧ｯ
Device: 繧ｹ繝斐・繧ｫ繝ｼ
Device: 繧ｹ繝・Ξ繧ｪ 繝溘く繧ｵ繝ｼ
```

### After (Correct Japanese Text)

```text
Device: マイク (Microphone)
Device: スピーカー (Speaker)
Device: ステレオ ミキサー (Stereo Mixer)
```

## Files Modified

- **Form1.cs**: Added P/Invoke declarations and `GetDeviceNameUTF8()` helper method
- **FlexASIOGUI.csproj**: Fixed `portaudio_x64.dll` auto-copy configuration

## Testing

To verify the fix works on your system:

1. Build the project:

   ```bash
   dotnet build -c Release
   ```

2. Run the application and check if device names with non-ASCII characters display correctly

3. Test with devices that have names in different languages

## Notes

- This fix maintains backward compatibility with the original `DescrambleUTF8` fallback method
- The P/Invoke approach is preferred as it addresses the root cause rather than working around symptoms
- RTL (right-to-left) languages like Arabic and Hebrew are supported for text storage and retrieval, though UI display may require additional Windows Forms RTL settings

## References

- [PortAudio API Documentation](http://www.portaudio.com/docs/v19-doxydocs/portaudio_8h.html)
- [UTF-8 Encoding](https://en.wikipedia.org/wiki/UTF-8)
- [P/Invoke in .NET](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)

---
*Supplementary note by Rutice ([ruticejp](https://github.com/ruticejp))*
