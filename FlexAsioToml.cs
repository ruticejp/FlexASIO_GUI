using Tomlyn;

namespace FlexASIOGUI
{
    public static class FlexAsioToml
    {
        /// <summary>
        /// Parses a FlexASIO TOML configuration string into the typed config model.
        /// Missing keys will not overwrite the default values.
        /// </summary>
        public static FlexGUIConfig Parse(string tomlText, TomlModelOptions options = null)
        {
            return ParseWithError(tomlText, options).Config;
        }

        /// <summary>
        /// Parses a FlexASIO TOML configuration string and returns an error message when parsing fails.
        /// </summary>
        public static (FlexGUIConfig Config, string Error) ParseWithError(string tomlText, TomlModelOptions options = null)
        {
            var defaults = new FlexGUIConfig();

            if (string.IsNullOrWhiteSpace(tomlText))
            {
                return (defaults, null);
            }

            options ??= new TomlModelOptions();

            try
            {
                var parsed = Toml.ToModel<FlexGUIConfig>(tomlText, options: options);
                return (MergeWithDefaults(defaults, parsed), null);
            }
            catch (TomlException ex)
            {
                // If TOML contains invalid types / malformed values, keep defaults rather than crashing.
                // Provide the error message so callers can log or show it.
                return (defaults, ex.Message);
            }
        }

        private static FlexGUIConfig MergeWithDefaults(FlexGUIConfig defaults, FlexGUIConfig parsed)
        {
            if (parsed == null)
                return defaults;

            var merged = new FlexGUIConfig
            {
                backend = parsed.backend ?? defaults.backend,
                bufferSizeSamples = parsed.bufferSizeSamples ?? defaults.bufferSizeSamples,
                input = MergeDeviceSection(defaults.input, parsed.input),
                output = MergeDeviceSection(defaults.output, parsed.output)
            };

            return merged;
        }

        private static FlexGUIConfigDeviceSection MergeDeviceSection(FlexGUIConfigDeviceSection defaults, FlexGUIConfigDeviceSection parsed)
        {
            if (parsed == null)
                return defaults;

            return new FlexGUIConfigDeviceSection
            {
                device = parsed.device ?? defaults.device,
                suggestedLatencySeconds = parsed.suggestedLatencySeconds ?? defaults.suggestedLatencySeconds,
                wasapiExclusiveMode = parsed.wasapiExclusiveMode ?? defaults.wasapiExclusiveMode,
                wasapiAutoConvert = parsed.wasapiAutoConvert ?? defaults.wasapiAutoConvert,
                channels = parsed.channels ?? defaults.channels
            };
        }
    }
}
