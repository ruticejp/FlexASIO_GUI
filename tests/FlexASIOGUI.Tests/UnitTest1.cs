using System;
using System.Collections.Generic;
using Xunit;

namespace FlexASIOGUI.Tests
{
    public class FlexAsioDllSelectorTests
    {
        [Fact]
        public void ChooseBestDll_Prefers64AndHighestVersion()
        {
            var candidates = new[] { "a.dll", "b.dll", "c.dll", "d.dll" };
            var versions = new Dictionary<string, Version>
            {
                ["a.dll"] = new Version(1, 0),
                ["b.dll"] = new Version(2, 0),
                ["c.dll"] = new Version(1, 5),
                ["d.dll"] = new Version(99, 0),
            };
            var is64 = new HashSet<string> { "b.dll", "c.dll" };

            string best = FlexAsioDllSelector.ChooseBestDll(
                candidates,
                path => versions.TryGetValue(path, out var v) ? v : null,
                path => is64.Contains(path));

            // Prefer x64 candidates even if a 32-bit candidate has a higher file version.
            Assert.Equal("b.dll", best);
        }

        [Fact]
        public void ChooseBestDll_ReturnsFirstCandidateWhenNoVersionsAvailable()
        {
            var candidates = new[] { "x.dll", "y.dll" };

            string best = FlexAsioDllSelector.ChooseBestDll(
                candidates,
                _ => null,
                _ => false);

            Assert.Equal("x.dll", best);
        }
    }

    public class FlexAsioTomlTests
    {
        [Fact]
        public void Parse_EmptyString_ReturnsDefaultConfig()
        {
            var config = FlexAsioToml.Parse(string.Empty);
            Assert.NotNull(config);
            Assert.Equal("Windows WASAPI", config.backend);
        }

        [Fact]
        public void Parse_ValidToml_MapsValuesCorrectly()
        {
            var toml =
                "backend = 'ALSA'\n" +
                "bufferSizeSamples = 512\n" +
                "\n" +
                "[input]\n" +
                "device = 'InputDevice'\n" +
                "suggestedLatencySeconds = 0.1\n" +
                "channels = 2\n" +
                "\n" +
                "[output]\n" +
                "device = 'OutputDevice'\n" +
                "suggestedLatencySeconds = 0.2\n" +
                "wasapiExclusiveMode = true\n";

            var options = new Tomlyn.TomlModelOptions
            {
                ConvertPropertyName = name => name
            };

            var config = FlexAsioToml.Parse(toml, options);

            Assert.Equal("ALSA", config.backend);
            Assert.Equal(512, config.bufferSizeSamples);
            Assert.Equal("InputDevice", config.input.device);
            Assert.Equal(0.1, config.input.suggestedLatencySeconds);
            Assert.Equal(2, config.input.channels);
            Assert.Equal("OutputDevice", config.output.device);
            Assert.Equal(0.2, config.output.suggestedLatencySeconds);
            Assert.True(config.output.wasapiExclusiveMode);
        }

        [Fact]
        public void Parse_MissingKeys_DoesNotOverwriteDefaults()
        {
            var toml =
                "bufferSizeSamples = 256\n" +
                "[input]\n" +
                "device = 'OnlyInputDevice'\n";

            var options = new Tomlyn.TomlModelOptions
            {
                ConvertPropertyName = name => name
            };

            var config = FlexAsioToml.Parse(toml, options);

            Assert.Equal("Windows WASAPI", config.backend); // default preserved
            Assert.Equal(256, config.bufferSizeSamples);
            Assert.Equal("OnlyInputDevice", config.input.device);
            Assert.Null(config.output.device);
        }

        [Fact]
        public void Parse_TypeMismatch_DoesNotThrowAndKeepsDefaults()
        {
            // bufferSizeSamples should be a number, but this TOML specifies a string.
            var toml =
                "bufferSizeSamples = 'not-a-number'\n" +
                "backend = 'ASIO'\n";

            var options = new Tomlyn.TomlModelOptions
            {
                ConvertPropertyName = name => name
            };

            var (config, error) = FlexAsioToml.ParseWithError(toml, options);

            // Parsing should not throw; defaults should be kept because TOML is invalid.
            Assert.Equal("Windows WASAPI", config.backend);
            Assert.Null(config.bufferSizeSamples);
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void ConvertPropertyName_IsApplied_WhenParsingToml()
        {
            // Using a custom converter to match TOML keys that are PascalCase.
            var toml =
                "Backend = 'ALSA'\n" +
                "BufferSizeSamples = 256\n";

            var options = new Tomlyn.TomlModelOptions
            {
                // Convert model property name to PascalCase so it matches the TOML key.
                ConvertPropertyName = name => char.ToUpperInvariant(name[0]) + name.Substring(1)
            };

            var (config, error) = FlexAsioToml.ParseWithError(toml, options);

            Assert.Null(error);
            Assert.Equal("ALSA", config.backend);
            Assert.Equal(256, config.bufferSizeSamples);
        }
    }
}
