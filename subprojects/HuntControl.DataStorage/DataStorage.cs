﻿using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace HuntControl.DataStorage
{
    public static class Storage
    {
        public static Harmony harmony;
        public static ConfigFile Config;
        public static ConfigEntry<bool> verboseLogOutput;
        public static ConfigEntry<bool> missionsProgressOffline;
        public static ConfigEntry<float> missionTimeMultiplier;
        public static ConfigEntry<bool> injuriesProgressOffline;
        public static ConfigEntry<float> injuryTimeMultiplier;
        public static ConfigEntry<long> lastTick;
        public static ManualLogSource logger;
        public static bool isAlive = false;

        public static void onUpdate()
        {
            lastTick.Value = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public static float getSecondsSinceLastSave()
        {
            int current = (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
            int last = (int)lastTick.Value;
            int diff = current - last;
            float seconds = diff / 1000;
            float multi = missionTimeMultiplier.Value;
            if (multi > 0) seconds *= multi;
            return seconds;
        }

        public static float getTimerMultiplierTick()
        {
            float multi = missionTimeMultiplier.Value;
            float seconds = 60 * multi;
            seconds -= 60;
            return seconds;
        }

        public static void onSave()
        {
            onUpdate();
            Config.Save();
        }

        public static void onDestroy()
        {
            logger.LogInfo("Saving data for server shutdown...");
            onSave();
            if (!isAlive) return;
            harmony.UnpatchSelf();
            isAlive = false;
        }

        public static void logVerbose(string str)
        {
            if (verboseLogOutput.Value) logger.LogInfo(str);
        }
    }
}
