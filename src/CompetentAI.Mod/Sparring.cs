using System;
using System.Collections.Generic;
using CompetentAI.Core;
using Newtonsoft.Json;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AI;

namespace CompetentAI.Mod
{
    internal enum SparringRole
    {
        None = 0,
        Attacker,
        Dummy
    }

    /// <summary>
    /// Spell of the spell wheel that summons practice NPCs. Two instances are shipped as JSON (Spells/*.json):
    /// mode "Attackers" (one NPC per weapon type, they attack you) and mode "Dummy" (one NPC that only parries).
    /// </summary>
    [Serializable]
    public class SpellSparring : SpellCastData
    {
        /// <summary>"Attackers" or "Dummy".</summary>
        public string mode = "Attackers";

        [NonSerialized, JsonIgnore]
        private SpellCaster _caster;

        public override void Load(SpellCaster spellCaster)
        {
            // The base implementation counts skills of a skill tree, which these utility spells do not have.
            _caster = spellCaster;
        }

        public override void Unload()
        {
            _caster = null;
            base.Unload();
        }

        public override void Fire(bool active)
        {
            if (!active) return;
            try
            {
                Sparring.Cast(mode, _caster);
            }
            catch (Exception ex)
            {
                Log.Error("sparring spell failed: " + ex);
            }
        }
    }

    /// <summary>Spawns and removes the practice NPCs.</summary>
    internal static class Sparring
    {
        private static readonly List<Creature> Attackers = new List<Creature>();
        private static readonly List<Creature> Dummies = new List<Creature>();

        public static void Cast(string mode, SpellCaster caster)
        {
            SparringConfig cfg = Runtime.Cfg.Sparring;
            if (!cfg.Enabled)
            {
                Log.Info("sparring spell cast but Sparring.Enabled is false");
                return;
            }
            Creature player = (caster != null && caster.ragdollHand != null ? caster.ragdollHand.creature : null) ?? Player.currentCreature;
            if (player == null) return;

            Vector3 aimed = AimPoint(caster, player);
            bool dummy = string.Equals(mode, "Dummy", StringComparison.OrdinalIgnoreCase);
            List<Creature> list = dummy ? Dummies : Attackers;

            if (HasLive(list))
            {
                Despawn(list);
                Log.Info("sparring: removed the previous " + (dummy ? "parry dummy" : "weapon attackers"));
                return; // casting again dismisses them; cast once more to summon a fresh set
            }

            if (dummy) SpawnDummy(cfg, player, aimed);
            else SpawnAttackers(cfg, player, aimed);
        }

        private static bool HasLive(List<Creature> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i] == null) list.RemoveAt(i);
            return list.Count > 0;
        }

        private static void Despawn(List<Creature> list)
        {
            foreach (Creature c in list)
            {
                if (c == null) continue;
                Runtime.Roles.Remove(c);
                try { c.Despawn(); } catch (Exception ex) { Log.Warn("could not despawn a practice NPC: " + ex.Message); }
            }
            list.Clear();
        }

        private static Vector3 AimPoint(SpellCaster caster, Creature player)
        {
            Vector3 point = player.transform.position + player.transform.forward * 5f;
            if (caster != null && caster.magicSource != null)
            {
                Vector3 origin = caster.magicSource.position;
                Vector3 dir = caster.magicSource.forward;
                RaycastHit[] hits = Physics.RaycastAll(origin, dir, 20f, ~0, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit h in hits)
                {
                    if (h.distance < 0.25f) continue;
                    if (h.collider.GetComponentInParent<Creature>() != null) continue; // ignore bodies (ours and others')
                    Item item = h.collider.GetComponentInParent<Item>();
                    if (item != null && item.mainHandler != null) continue;           // ignore held weapons
                    point = h.point;
                    break;
                }
            }
            if (NavMesh.SamplePosition(point, out NavMeshHit nav, 4f, NavMesh.AllAreas)) point = nav.position;
            return point;
        }

        private static float FaceYaw(Vector3 from, Vector3 target)
        {
            Vector3 d = target - from;
            d.y = 0f;
            return d.sqrMagnitude < 1e-4f ? 0f : Quaternion.LookRotation(d).eulerAngles.y;
        }

        private static Vector3 Snap(Vector3 p)
        {
            return NavMesh.SamplePosition(p, out NavMeshHit nav, 2.5f, NavMesh.AllAreas) ? nav.position : p;
        }

        private static CreatureData MakeData(SparringConfig cfg, string containerId, string brainId, int health)
        {
            CreatureData source = Catalog.GetData<CreatureData>(cfg.CreatureId);
            if (source == null)
            {
                Log.Error("sparring: creature '" + cfg.CreatureId + "' not found in the catalog");
                return null;
            }
            if (!Catalog.TryGetData<ContainerData>(containerId, out _))
            {
                Log.Warn("sparring: container '" + containerId + "' is not in the catalog (did the mod's Containers folder load?) - skipped");
                return null;
            }
            // Same technique the game uses to apply container/brain/faction overrides to a spawn.
            var data = source.Clone() as CreatureData;
            if (data == null) return null;
            data.containerID = containerId;
            data.brainId = brainId;
            data.factionId = cfg.FactionId;
            data.health = health;
            return data;
        }

        private static void SpawnAttackers(SparringConfig cfg, Creature player, Vector3 center)
        {
            int n = cfg.Loadouts.Count;
            int spawned = 0;
            for (int i = 0; i < n; i++)
            {
                SparringLoadout l = cfg.Loadouts[i];
                CreatureData data = MakeData(cfg, l.ContainerId, cfg.AttackerBrainId, cfg.AttackerHealth);
                if (data == null) continue;
                float angle = i * 360f / n;
                Vector3 pos = Snap(center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * cfg.RingRadius);
                string label = l.Name;
                data.SpawnAsync(pos, FaceYaw(pos, player.transform.position), null, true, null, c => OnSpawned(c, SparringRole.Attacker, Attackers, label));
                spawned++;
            }
            Log.Info($"sparring: summoning {spawned} weapon attackers around {center} (cast again to dismiss them)");
        }

        private static void SpawnDummy(SparringConfig cfg, Creature player, Vector3 center)
        {
            CreatureData data = MakeData(cfg, cfg.DummyContainerId, cfg.DummyBrainId, cfg.DummyHealth);
            if (data == null) return;
            Vector3 toPlayer = player.transform.position - center;
            toPlayer.y = 0f;
            Vector3 pos = Snap(center + toPlayer.normalized * Mathf.Min(cfg.DummyDistance, toPlayer.magnitude));
            data.SpawnAsync(pos, FaceYaw(pos, player.transform.position), null, true, null, c => OnSpawned(c, SparringRole.Dummy, Dummies, "parry dummy"));
            Log.Info($"sparring: summoning the parry dummy at {pos} (cast again to dismiss it)");
        }

        private static void OnSpawned(Creature c, SparringRole role, List<Creature> list, string label)
        {
            if (c == null) return;
            Runtime.Roles[c] = role;
            list.Add(c);
            Log.Debug_($"sparring: {role} '{label}' spawned as {c.name}");
        }
    }
}
