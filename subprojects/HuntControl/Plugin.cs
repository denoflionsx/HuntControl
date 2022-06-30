using BepInEx;
using BepInEx.IL2CPP;
using Wetstone.API;
using HarmonyLib;
using System;
using BepInEx.Logging;
using HuntControl.Lib;
using HuntControl.Missions;
using HuntControl.Injuries;

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

            if (VWorld.IsClient) Log.LogWarning("This mod only needs to be installed server side.");
            if (!VWorld.IsServer) return;

            Storage.Config = Config;

            Storage.verboseLogOutput = Storage.Config.Bind<bool>("Debugging", "verboseLogOutput", false, "Log everything for debugging");

            Storage.logVerbose("Logging all information.");

            Storage.logger = logger;
            Storage.harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            MissionHarmony.Apply();
            InjuryHarmony.Apply();
            Storage.isAlive = true;
        }

        public override bool Unload()
        {
            Storage.onDestroy();
            return true;
        }
    }
}
