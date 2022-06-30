using System;
using ProjectM;
using ProjectM.Shared.Systems;
using HarmonyLib;
using HuntControl.Lib;

namespace HuntControl.Missions
{

    public static class MissionHarmony
    {
        public static void Apply() {
            Storage.harmony.PatchAll();
        }
    }

    // Setup initial data.
    [HarmonyPatch(typeof(ServantMissionUpdateSystem), "OnCreate")]
    public static class ServantMissionUpdateSystem_OnCreate_Patch
    {
        public static void Prefix(ServantMissionUpdateSystem __instance)
        {
            Storage.logVerbose("ServantMissionUpdateSystem created.");
            Storage.missionsProgressOffline = Storage.Config.Bind<bool>("HuntControl", "missionsProgressOffline", true, "Keep mission time running even if offline.");
            Storage.missionTimeMultiplier = Storage.Config.Bind<float>("HuntControl", "missionTimeMultiplier", 0, "Multiply how fast missions progress. Example: 2 = 24h becomes 12h. Updates in bulk once a minute.");
            Storage.lastTick = Storage.Config.Bind<long>("HuntControl", "lastTick", 0, "Don't edit this.");
            if (Storage.lastTick.Value == 0) Storage.onSave();
        }
    }

    // Save data when the server shuts down.
    [HarmonyPatch(typeof(ServantMissionUpdateSystem), "OnDestroy")]
    public static class ServantMissionUpdateSystem_OnDestroy_Patch
    {
        public static void Prefix(ServerBootstrapSystem __instance)
        {
            Storage.logVerbose("ServantMissionUpdateSystem destroyed.");
            Storage.onDestroy();
        }
    }

    // Hook the update loop for missions.
    [HarmonyPatch(typeof(ServantMissionUpdateSystem), "OnUpdate")]
    public static class ServantMissionUpdateSystem_OnUpdate_Patch
    {

        private static bool firstTick = true;
        private static DateTime NoUpdateBefore = DateTime.MinValue;

        // Every 60 seconds process mission data.
        public static void Prefix(ServantMissionUpdateSystem __instance)
        {

            if (NoUpdateBefore > DateTime.Now) return;

            // @TODO: Find something to hook that only runs once after the mission stuff is fully loaded so I don't have to do a bool check here.
            if (firstTick)
            {
                // We've been offline.
                if (Storage.missionsProgressOffline.Value)
                {
                    Storage.logger.LogInfo("Processing missions since last session...");
                    float seconds = Storage.getSecondsSinceLastSave();
                    int proc = MissionHelper.processMissions(__instance, seconds);
                    Storage.logger.LogInfo("Processed " + proc.ToString() + " missions. Time reduced by " + seconds.ToString() + " seconds.");
                }
                firstTick = false;
                NoUpdateBefore = DateTime.Now.AddMilliseconds(60000);
                return;
            }

            // Do time reduction multiplier.
            if (Storage.missionTimeMultiplier.Value > 0)
            {
                float seconds = Storage.getTimerMultiplierTick();
                if (seconds > 0) MissionHelper.processMissions(__instance, seconds);
            }

            NoUpdateBefore = DateTime.Now.AddMilliseconds(60000);
        }
    }
}
