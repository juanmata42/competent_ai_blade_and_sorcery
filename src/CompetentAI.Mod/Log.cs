using CompetentAI.Core;
using UnityEngine;

namespace CompetentAI.Mod
{
    /// <summary>Leveled, rate-limited logger. Everything goes to the Unity log (and therefore to the game's Player.log).</summary>
    internal static class Log
    {
        public const string Prefix = "[CompetentAI] ";

        public static LogLevel Level = LogLevel.Info;
        public static bool AttackLogs = true;
        public static bool DefenseLogs = true;
        public static int MaxLinesPerSecond = 30;

        private static float _windowStart;
        private static int _count;
        private static int _dropped;

        public static bool Enabled(LogLevel level) => level <= Level && Level != LogLevel.Off;

        public static void Error(string msg)
        {
            if (Level >= LogLevel.Error && Allow(true)) Debug.LogError(Prefix + msg);
        }

        public static void Warn(string msg)
        {
            if (Level >= LogLevel.Warn && Allow(false)) Debug.LogWarning(Prefix + msg);
        }

        public static void Info(string msg)
        {
            if (Level >= LogLevel.Info && Allow(false)) Debug.Log(Prefix + msg);
        }

        public static void Debug_(string msg)
        {
            if (Level >= LogLevel.Debug && Allow(false)) Debug.Log(Prefix + "[D] " + msg);
        }

        public static void Trace(string msg)
        {
            if (Level >= LogLevel.Trace && Allow(false)) Debug.Log(Prefix + "[T] " + msg);
        }

        public static void Attack(string msg)
        {
            if (AttackLogs && Level >= LogLevel.Debug && Allow(false)) Debug.Log(Prefix + "[atk] " + msg);
        }

        public static void Defense(string msg)
        {
            if (DefenseLogs && Level >= LogLevel.Debug && Allow(false)) Debug.Log(Prefix + "[def] " + msg);
        }

        /// <summary>Errors always get through (they are the most useful lines), everything else is rate limited.</summary>
        private static bool Allow(bool force)
        {
            float now = Time.realtimeSinceStartup;
            if (now - _windowStart >= 1f)
            {
                if (_dropped > 0) Debug.Log(Prefix + "(" + _dropped + " log lines dropped by the rate limiter)");
                _windowStart = now;
                _count = 0;
                _dropped = 0;
            }
            if (force) return true;
            if (_count >= MaxLinesPerSecond)
            {
                _dropped++;
                return false;
            }
            _count++;
            return true;
        }
    }
}
