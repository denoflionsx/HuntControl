﻿using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ProjectM;
using ProjectM.Shared.Systems;
using System;
using System.Collections.Generic;

namespace HuntControl.Lib
{

    public class Timeout
    {
        public int time;
        public Action<bool> t;

        public Timeout(Action<bool> t, int time)
        {
            this.time = time * 1000;
            this.t = t;
        }
    }

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
        public static ConfigEntry<bool> forceCompleteAllMissions;
        public static ConfigEntry<bool> forceCompleteAllInjuries;
        public static Dictionary<string, Action<ServantMissionUpdateSystem>> createCallbacks = new Dictionary<string, Action<ServantMissionUpdateSystem>>();
        public static Dictionary<string, Action<ServantMissionUpdateSystem>> updateCallbacks = new Dictionary<string, Action<ServantMissionUpdateSystem>>();
        public static Dictionary<string, Action<ServantMissionUpdateSystem>> destroyCallbacks = new Dictionary<string, Action<ServantMissionUpdateSystem>>();
        public static Dictionary<string, Timeout> timeouts = new Dictionary<string, Timeout>();
        public static ManualLogSource logger;
        public static bool isAlive = false;

        public static float getSecondsSinceLastSave(float multi)
        {
            int current = (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
            int last = (int)lastTick.Value;
            int diff = current - last;
            float seconds = diff / 1000;
            if (multi > 0) seconds *= multi;
            return seconds;
        }

        public static float getMissionTimerMultiplierTick()
        {
            float multi = missionTimeMultiplier.Value;
            float seconds = 60 * multi;
            seconds -= 60;
            return seconds;
        }

        public static float getInjuryTimerMultiplierTick()
        {
            float multi = injuryTimeMultiplier.Value;
            float seconds = 60 * multi;
            seconds -= 60;
            return seconds;
        }

        public static void onSave()
        {
            lastTick.Value = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Config.Save();
        }

        public static void onDestroy()
        {
            logger.LogInfo("Saving data for server shutdown...");
            forceCompleteAllMissions.Value = false;
            forceCompleteAllInjuries.Value = false;
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
