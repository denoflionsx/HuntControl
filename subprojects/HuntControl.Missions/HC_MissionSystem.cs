using System;
using ProjectM.Shared.Systems;
using HuntControl.Lib;

namespace HuntControl.Missions
{

    public static class MissionSystem
    {
        public static void Apply() {
            Storage.createCallbacks.Add(typeof(MissionSystem_Startup).Name, MissionSystem_Startup.Prefix);
            Storage.timeouts.Add(typeof(MissionSystem).Name, new Timeout(Register, 30));
            Storage.logger.LogInfo("Mission module will start in 30 seconds.");
        }

        public static void Register(bool b)
        {
            Storage.destroyCallbacks.Add(typeof(MissionSystem_Shutdown).Name, MissionSystem_Shutdown.Prefix);
            Storage.updateCallbacks.Add(typeof(MissionSystem_Update).Name, MissionSystem_Update.Prefix);
        }
    }

    // Setup initial data.
    public static class MissionSystem_Startup
    {
        public static void Prefix(ServantMissionUpdateSystem __instance)
        {
            Storage.logVerbose("ServantMissionUpdateSystem created.");
            Storage.missionsProgressOffline = Storage.Config.Bind<bool>("HuntControl", "missionsProgressOffline", true, "Keep mission time running even if offline.");
            Storage.missionTimeMultiplier = Storage.Config.Bind<float>("HuntControl", "missionTimeMultiplier", 0, "Multiply how fast missions progress. Example: 2 = 24h becomes 12h. Updates in bulk once a minute.");
            Storage.lastTick = Storage.Config.Bind<long>("HuntControl", "lastTick", 0, "Don't edit this.");
            Storage.forceCompleteAllMissions = Storage.Config.Bind<bool>("HuntControl", "forceCompleteAllMissions", false, "This option will force every mission to complete immediately on boot then disable itself.");
            if (Storage.lastTick.Value == 0) Storage.onSave();
        }
    }

    // Save data when the server shuts down.
    public static class MissionSystem_Shutdown
    {
        public static void Prefix(ServantMissionUpdateSystem __instance)
        {
            Storage.forceCompleteAllMissions.Value = false;
        }
    }

    // Hook the update loop for missions.
    public static class MissionSystem_Update
    {

        public static bool firstTick = true;
        public static DateTime NoUpdateBefore = DateTime.MinValue;

        // Every 60 seconds process mission data.
        public static void Prefix(ServantMissionUpdateSystem __instance)
        {

            if (NoUpdateBefore > DateTime.Now) return;

            // @TODO: Find something to hook that only runs once after the mission stuff is fully loaded so I don't have to do a bool check here.
            if (firstTick)
            {
                // We've been offline.
                if (Storage.missionsProgressOffline.Value && MissionHelper.getNumberOfMissions(__instance.EntityManager) > 0)
                {
                    
                    Storage.logger.LogInfo("Processing missions since last session...");
                    float seconds = Storage.getSecondsSinceLastSave(Storage.missionTimeMultiplier.Value);
                    int proc = MissionHelper.processMissions(__instance.EntityManager, seconds, Storage.forceCompleteAllMissions.Value);
                    Storage.logger.LogInfo("Processed " + proc.ToString() + " missions. Time reduced by " + seconds.ToString() + " seconds.");
                }
                firstTick = false;
                NoUpdateBefore = DateTime.Now.AddMilliseconds(60000);
                return;
            }

            // Do time reduction multiplier.
            if (Storage.missionTimeMultiplier.Value > 0 && MissionHelper.getNumberOfMissions(__instance.EntityManager) > 0)
            {
                float seconds = Storage.getMissionTimerMultiplierTick();
                if (seconds > 0) MissionHelper.processMissions(__instance.EntityManager, seconds);
            }

            NoUpdateBefore = DateTime.Now.AddMilliseconds(60000);
        }
    }
}
