using BepInEx;
using BepInEx.IL2CPP;
using Wetstone.API;
using HarmonyLib;
using System;
using ProjectM;
using Unity.Entities;
using BepInEx.Logging;
using BepInEx.Configuration;

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
            logger.LogInfo("Shutting down mission management system.");
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

            if (VWorld.IsClient)
            {
                Log.LogWarning("This mod only needs to be installed server side.");
            }
            if (!VWorld.IsServer) return;

            DataStorage.missionsProgressOffline = Config.Bind<bool>("HuntControl", "missionsProgressOffline", true, "Keep mission time running even if offline.");
            DataStorage.missionTimeMultiplier = Config.Bind<float>("HuntControl", "missionTimeMultiplier", 0, "Multiply how fast missions progress. Example: 2 = 24h becomes 12h. Updates in bulk once a minute.");
            DataStorage.lastTick = Config.Bind<long>("HuntControl", "lastTick", 0, "Don't edit this.");
            if (DataStorage.lastTick.Value == 0)
            {
                DataStorage.onUpdate();
            }

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

        // Save data when the server shuts down.
        [HarmonyPatch(typeof(ServerBootstrapSystem), "OnDestroy")]
        public static class ServerBootstrapSystemDestroy_Patch
        {
            public static void Prefix(ServerBootstrapSystem __instance)
            {
                DataStorage.onDestroy();
            }
        }

        // I'm unsure if there is a better way to make an update tick, but this seems to work fine from my testing.
        [HarmonyPatch(typeof(FeedableInventorySystem_Update), "OnUpdate")]
        public static class FeedSystem_OnUpdate_Patch
        {

            private static DateTime NoUpdateBefore = DateTime.MinValue;

            public static void processMissions(FeedableInventorySystem_Update __instance, float reduction)
            {
                if (reduction < 0) return;
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
                            logger.LogInfo("Reducing mission " + mission.MissionID.ToString() + " time remaining by " + reduction.ToString() + " seconds.");
                        }
                        missionBuffer[i] = mission;
                    }
                }
            }

            // Every 60 seconds process mission data.
            public static void Prefix(FeedableInventorySystem_Update __instance)
            {

                if (NoUpdateBefore > DateTime.Now) return;

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
                        if (multi > 0)
                        {
                            seconds *= multi;
                        }
                        processMissions(__instance, seconds);
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
                    if (seconds > 0)
                    {
                        processMissions(__instance, seconds);
                    }
                }

                NoUpdateBefore = DateTime.Now.AddMilliseconds(60000);
                DataStorage.onUpdate();
            }
        }
    }
}
