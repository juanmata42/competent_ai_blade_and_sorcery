using System;
using ThunderRoad;
using UnityEngine;

namespace CompetentAI.Mod
{
    /// <summary>
    /// Optional on-screen text ("Extra info" in the options menu, off by default) describing what NPCs near the player are
    /// doing: sequences, feints, reading or falling for the player's feints, counters. Uses the game's own message panel,
    /// anchored by the left hand. Everything is guarded: if the panel misbehaves the feature switches itself off.
    /// </summary>
    internal static class Hud
    {
        private static float _last = -100f;
        private static bool _broken;

        public static void Show(Creature who, string text)
        {
            try
            {
                if (_broken || who == null || !Runtime.Flags.ExtraInfo) return;
                var cfg = Runtime.Cfg.Hud;
                Creature player = Player.currentCreature;
                if (player == null || Player.local == null || DisplayMessage.instance == null) return;
                if ((who.transform.position - player.transform.position).sqrMagnitude > cfg.MaxDistance * cfg.MaxDistance) return;

                float now = Time.unscaledTime;
                if (now - _last < cfg.MinSecondsBetween) return;
                _last = now;

                DisplayMessage.instance.ShowMessage(new DisplayMessage.MessageData(
                    text, 0, 0f, null, null, false, false, false, false,
                    MessageAnchorType.HandLeft, null, true, cfg.DismissSeconds, null, true, typeof(Hud)));
            }
            catch (Exception ex)
            {
                _broken = true;
                Log.Warn("on-screen info disabled after an error (the log still has everything): " + ex.Message);
            }
        }
    }
}
