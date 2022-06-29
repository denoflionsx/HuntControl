using BepInEx;
using BepInEx.IL2CPP;
using Wetstone.API;
using HarmonyLib;
using System;
using ProjectM;
using Unity.Entities;
using BepInEx.Logging;
using BepInEx.Configuration;
using ProjectM.Shared.Systems;

// This mod takes some code from these two other mods.
// https://github.com/Blargerist/Sleeping-Speeds-Time - How to edit mission times.
// https://github.com/decaprime/LeadAHorseToWater - How to make a timed tick.

namespace HuntControl
{

    public static class DataStorage
    {
        public static bool isFirstTick = true;
        public static Harmony harmony;
        public static ConfigFile Config;
        public static ConfigEntry<bool> missionsProgressOffline;
        public static ConfigEntry<float> missionTimeMultiplier;
        public static ConfigEntry<long> lastTick;
        public static ManualLogSource logger;
        public static bool isAlive = false;

        public static void onUpdate()
        {
            lastTick.Value = DateTimeOffset.Now.ToUnixTimeMilliseconds();
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
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("xyz.molenzwiebel.wetstone")]
    public class Plugin : BasePlugin
    {

        public static ManualLogSource logger;
        const String pluginGUID = "com.modloader64.HuntControl";
        const String pluginName = "HuntControl";
        const String pluginVersion = "1.0.0";

        public override void Load()
        {
            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            logger = this.Log;

            if (VWorld.IsClient) Log.LogWarning("This mod only needs to be installed server side.");
            if (!VWorld.IsServer) return;

            DataStorage.Config = Config;
            DataStorage.logger = logger;
            DataStorage.harmony = new Harmony(pluginGUID);
            DataStorage.harmony.PatchAll();
            DataStorage.isAlive = true;
        }

        public override bool Unload()
        {
            DataStorage.onDestroy();
            return true;
        }

        public static class MissionHelper
        {
            public static int processMissions(ServantMissionUpdateSystem __instance, float reduction)
            {

                int missionsProcessed = 0;

                if (reduction < 0) return missionsProcessed;

                var servantMissonQuery = __instance.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<ActiveServantMission>());
                var missionEntities = servantMissonQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

                foreach (var missionEntity in missionEntities)
                {
                    var missionBuffer = __instance.EntityManager.GetBuffer<ActiveServantMission>(missionEntity);

                    for (int i = 0; i < missionBuffer.Length; i++)
                    {
                        var mission = missionBuffer[i];
                        if (mission.MissionLength > 0)
                        {
                            if (mission.MissionLength - reduction < 0)
                            {
                                mission.MissionLength = 0;
                            }
                            else
                            {
                                mission.MissionLength -= reduction;
                            }
                            missionsProcessed++;
                            logger.LogDebug("Reducing mission " + mission.MissionID.ToString() + " time remaining by " + reduction.ToString() + " seconds.");
                        }
                        missionBuffer[i] = mission;
                    }
                }

                return missionsProcessed;
            }
        }

        // Setup initial data.
        [HarmonyPatch(typeof(ServantMissionUpdateSystem), "OnCreate")]
        public static class ServantMissionUpdateSystem_OnCreate_Patch
        {
            public static void Prefix(ServantMissionUpdateSystem __instance)
            {
                DataStorage.logger.LogDebug("ServantMissionUpdateSystem created.");
                DataStorage.missionsProgressOffline = DataStorage.Config.Bind<bool>("HuntControl", "missionsProgressOffline", true, "Keep mission time running even if offline.");
                DataStorage.missionTimeMultiplier = DataStorage.Config.Bind<float>("HuntControl", "missionTimeMultiplier", 0, "Multiply how fast missions progress. Example: 2 = 24h becomes 12h. Updates in bulk once a minute.");
                DataStorage.lastTick = DataStorage.Config.Bind<long>("HuntControl", "lastTick", 0, "Don't edit this.");
                if (DataStorage.lastTick.Value == 0) DataStorage.onSave();
            }
        }

        // Save data when the server shuts down.
        [HarmonyPatch(typeof(ServantMissionUpdateSystem), "OnDestroy")]
        public static class ServantMissionUpdateSystem_OnDestroy_Patch
        {
            public static void Prefix(ServerBootstrapSystem __instance)
            {
                DataStorage.logger.LogDebug("ServantMissionUpdateSystem destroyed.");
                DataStorage.onDestroy();
            }
        }

        // Hook the update loop for missions.
        [HarmonyPatch(typeof(ServantMissionUpdateSystem), "OnUpdate")]
        public static class ServantMissionUpdateSystem_OnUpdate_Patch
        {

            private static DateTime NoUpdateBefore = DateTime.MinValue;

            // Every 60 seconds process mission data.
            public static void Prefix(ServantMissionUpdateSystem __instance)
            {

                if (NoUpdateBefore > DateTime.Now) return;

                // @TODO: Find something to hook that only runs once after the mission stuff is fully loaded so I don't have to do a bool check here.
                if (DataStorage.isFirstTick)
                {
                    // We've been offline.
                    if (DataStorage.missionsProgressOffline.Value)
                    {
                        logger.LogInfo("Processing missions since last session...");
                        int current = (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        int last = (int)DataStorage.lastTick.Value;
                        int diff = current - last;
                        float seconds = diff / 1000;
                        float multi = DataStorage.missionTimeMultiplier.Value;
                        if (multi > 0) seconds *= multi;
                        int proc = MissionHelper.processMissions(__instance, seconds);
                        logger.LogInfo("Processed " + proc.ToString() + " missions. Time reduced by " + seconds.ToString() + " seconds.");
                    }
                    DataStorage.isFirstTick = false;
                    NoUpdateBefore = DateTime.Now.AddMilliseconds(60000);
                    return;
                }

                // Do time reduction multiplier.
                if (DataStorage.missionTimeMultiplier.Value > 0)
                {
                    float multi = DataStorage.missionTimeMultiplier.Value;
                    float seconds = 60 * multi;
                    seconds -= 60;
                    if (seconds > 0) MissionHelper.processMissions(__instance, seconds);
                }

                NoUpdateBefore = DateTime.Now.AddMilliseconds(60000);
            }
        }
    }
}
