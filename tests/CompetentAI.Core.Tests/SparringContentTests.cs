using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompetentAI.Core;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CompetentAI.Core.Tests
{
    /// <summary>The shipped sparring content (containers + spells) must agree with the default configuration.</summary>
    public class SparringContentTests
    {
        private static string Root()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir, "mod", "CompetentAI"))) return Path.Combine(dir, "mod", "CompetentAI");
                dir = Path.GetDirectoryName(dir);
            }
            throw new DirectoryNotFoundException("mod/CompetentAI");
        }

        private static JObject Container(string id) =>
            JObject.Parse(File.ReadAllText(Path.Combine(Root(), "Containers", "Container_" + id + ".json")));

        [Fact]
        public void Every_default_loadout_has_a_shipped_container_with_one_weapon_in_a_holster()
        {
            var cfg = new ModConfig();
            cfg.Normalize();
            var ids = cfg.Sparring.Loadouts.Select(l => l.ContainerId).Concat(new[] { cfg.Sparring.DummyContainerId }).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            foreach (string id in ids)
            {
                JObject c = Container(id);
                Assert.Equal(id, (string)c["id"]);
                Assert.Equal(1, (int)c["version"]);
                var holstered = ((JArray)c["containerContents"])
                    .Where(x => ((string)x["state"]?["$type"] ?? "").Contains("ContentStateHolder")).ToList();
                Assert.NotEmpty(holstered);
                foreach (JToken h in holstered)
                    Assert.Contains((string)h["state"]["holderName"], new[] { "BackRight", "BackLeft", "HipsRight", "HipsLeft" });
                Assert.Contains(holstered, h => (string)h["state"]["holderName"] == "BackRight"); // main hand weapon
            }
        }

        [Fact]
        public void Container_merge_keys_are_unique_within_each_container()
        {
            foreach (string file in Directory.GetFiles(Path.Combine(Root(), "Containers"), "*.json"))
            {
                var keys = ((JArray)JObject.Parse(File.ReadAllText(file))["containerContents"]).Select(x => (string)x["mergeKey"]).ToList();
                Assert.Equal(keys.Count, keys.Distinct().Count());
            }
        }

        [Fact]
        public void Spells_are_default_skills_so_they_reach_the_spell_wheel_and_are_hidden_from_the_skill_tree()
        {
            foreach (string file in Directory.GetFiles(Path.Combine(Root(), "Spells"), "*.json"))
            {
                JObject s = JObject.Parse(File.ReadAllText(file));
                Assert.Equal("CompetentAI.Mod.SpellSparring, CompetentAI", (string)s["$type"]);
                Assert.True((bool)s["allowSkill"]);
                Assert.True((bool)s["isDefaultSkill"]);
                Assert.False((bool)s["showInTree"]);
                Assert.True((bool)s["hasOrder"]);
                Assert.Equal(0, (int)s["version"]);
                Assert.Contains((string)s["mode"], new[] { "Attackers", "Dummy" });
            }
            Assert.Equal(2, Directory.GetFiles(Path.Combine(Root(), "Spells"), "*.json").Length);
        }

        [Fact]
        public void Referenced_game_items_exist_in_the_extracted_base_game_when_available()
        {
            string dir = AppContext.BaseDirectory;
            string items = null;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                string c = Path.Combine(dir, ".local", "basjson", "Items");
                if (Directory.Exists(c)) { items = c; break; }
                dir = Path.GetDirectoryName(dir);
            }
            if (items == null) return; // data not present on this machine
            var known = new HashSet<string>();
            foreach (string f in Directory.GetFiles(items, "*.json"))
            {
                try { known.Add((string)JObject.Parse(File.ReadAllText(f))["id"]); } catch { }
            }
            foreach (string file in Directory.GetFiles(Path.Combine(Root(), "Containers"), "*.json"))
                foreach (JToken c in (JArray)JObject.Parse(File.ReadAllText(file))["containerContents"])
                    Assert.True(known.Contains((string)c["referenceID"]), (string)c["referenceID"] + " in " + Path.GetFileName(file));
        }

        [Fact]
        public void Sparring_config_survives_overrides_and_garbage()
        {
            var cfg = ConfigIO.Parse("{ \"Sparring\": { \"RingRadius\": 0, \"AttackerHealth\": -5, \"Loadouts\": [ { \"Name\": \"x\", \"ContainerId\": \"A\" }, { \"Name\": \"bad\" } ] } }", null);
            Assert.Equal(1.5f, cfg.Sparring.RingRadius);
            Assert.Equal(10, cfg.Sparring.AttackerHealth);
            Assert.Single(cfg.Sparring.Loadouts);               // the list REPLACES the defaults; entries without container are dropped
            Assert.Equal("A", cfg.Sparring.Loadouts[0].ContainerId);
            var defaults = ConfigIO.Parse("{}", null);
            Assert.Equal(13, defaults.Sparring.Loadouts.Count);
        }
    }
}
