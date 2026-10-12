using System;
using System.Collections.Generic;
using System.IO;
using CompetentAI.Core;
using Xunit;

namespace CompetentAI.Core.Tests
{
    public class ShippedConfigTests
    {
        private static string Find(string relative)
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, relative);
                if (File.Exists(candidate)) return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            throw new FileNotFoundException(relative);
        }

        [Fact]
        public void Shipped_default_config_parses_without_warnings_and_equals_the_built_in_defaults()
        {
            var warnings = new List<string>();
            string json = File.ReadAllText(Find(Path.Combine("mod", "CompetentAI", "CompetentAI.config.json")));
            ModConfig shipped = ConfigIO.Parse(json, warnings);
            Assert.Empty(warnings);

            var defaults = new ModConfig();
            defaults.Normalize();
            Assert.Equal(ConfigIO.Serialize(defaults), ConfigIO.Serialize(shipped));
        }

        [Fact]
        public void Manifest_targets_the_game_min_mod_version_verified_in_the_game_data()
        {
            string json = File.ReadAllText(Find(Path.Combine("mod", "CompetentAI", "manifest.json")));
            var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
            Assert.Equal("1.0.0.0", (string)obj["GameVersion"]);
            Assert.False(string.IsNullOrEmpty((string)obj["Name"]));
            Assert.True(Version.TryParse((string)obj["ModVersion"], out _));
        }
    }
}
