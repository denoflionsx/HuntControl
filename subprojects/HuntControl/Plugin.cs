using BepInEx;
using BepInEx.IL2CPP;
using Wetstone.API;
using HarmonyLib;
using BepInEx.Logging;
using HuntControl.Lib;
using HuntControl.Missions;
using HuntControl.Injuries;
using ProjectM.Shared.Systems;
using System;
using System.Collections.Generic;

// This mod takes some code from these two other mods.
// https://github.com/Blargerist/Sleeping-Speeds-Time - How to edit mission times.
// https://github.com/decaprime/LeadAHorseToWater - How to make a timed tick.

namespace HuntControl
{

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("xyz.molenzwiebel.wetstone")]
    public class Plugin : BasePlugin
    {

        public static ManualLogSource logger;

        public override void Load()
        {
            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            logger = this.Log;

            universal(this);

            if (VWorld.IsClient)
            {
                clientside(this);
            }
            if (VWorld.IsServer)
            {
                serverside(this);
            }

        }

        public static void serverside(Plugin parent) {
            Storage.harmony.PatchAll();
            MissionSystem.Apply();
            InjurySystem.Apply();
        }

        public static void clientside(Plugin parent) {
            Storage.clientMsg = parent.Config.Bind<string>("HuntControl", "WrongSide", "This is a server side mod but you're looking in the client config folder. Go to BepInEx_Server/config instead.", "I get asked this question a lot.");
        }

        public static void universal(Plugin parent)
        {
            Storage.Config = parent.Config;
            Storage.verboseLogOutput = Storage.Config.Bind<bool>("Debugging", "verboseLogOutput", false, "Log everything for debugging");
            Storage.logger = logger;
            Storage.logVerbose("Logging all information.");
            Storage.isAlive = true;
            Storage.harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        }

        public override bool Unload()
        {
            Storage.onDestroy();
            return true;
        }
        
        // Setup initial data.
        [HarmonyPatch(typeof(ServantMissionUpdateSystem), "OnCreate")]
        public static class ServantMissionUpdateSystem_OnCreate_Patch
        {
            public static void Prefix(ServantMissionUpdateSystem __instance)
            {
                foreach (Action< ServantMissionUpdateSystem> cb in Storage.createCallbacks.Values)
                {
                    cb(__instance);
                }
            }
        }

        // Save data when the server shuts down.
        [HarmonyPatch(typeof(ServantMissionUpdateSystem), "OnDestroy")]
        public static class ServantMissionUpdateSystem_OnDestroy_Patch
        {
            public static void Prefix(ServantMissionUpdateSystem __instance)
            {
                foreach (Action<ServantMissionUpdateSystem> cb in Storage.destroyCallbacks.Values)
                {
                    cb(__instance);
                }
            }
        }

        // Hook the update loop for missions.
        [HarmonyPatch(typeof(ServantMissionUpdateSystem), "OnUpdate")]
        public static class ServantMissionUpdateSystem_OnUpdate_Patch
        {

            public static DateTime NoUpdateBefore = DateTime.Now.AddMilliseconds(1000);

            public static void Prefix(ServantMissionUpdateSystem __instance)
            {
                foreach (KeyValuePair<string, Action<ServantMissionUpdateSystem>> entry in Storage.updateCallbacks)
                {
                    entry.Value(__instance);
                }

                if (NoUpdateBefore > DateTime.Now) return;

                NoUpdateBefore = DateTime.Now.AddMilliseconds(1000);

                if (Storage.timeouts.Count > 0)
                {
                    Dictionary<string, bool> remove = new Dictionary<string, bool>();
                    foreach (KeyValuePair<string, Timeout> entry in Storage.timeouts)
                    {
                        if (entry.Value.time > 0)
                        {
                            entry.Value.time -= 1000;
                        }
                        else
                        {
                            remove.Add(entry.Key, true);
                            entry.Value.t(true);
                        }
                    }
                    if (remove.Count > 0)
                    {
                        foreach(KeyValuePair<string, bool> entry in remove)
                        {
                            Storage.timeouts.Remove(entry.Key);
                        }
                    }
                }
            }
        }
    }
}
