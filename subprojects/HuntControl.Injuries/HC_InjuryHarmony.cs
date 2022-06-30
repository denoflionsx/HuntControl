using HarmonyLib;
using HuntControl.DataStorage;
using ProjectM.Shared.Systems;
using System;

namespace HuntControl.Injuries
{

    public static class InjuryHarmony
    {
        public static void Apply()
        {
            Storage.harmony.PatchAll();
        }
    }

    // Setup initial data.
    [HarmonyPatch(typeof(ServantMissionUpdateSystem), "OnCreate")]
    public static class ServantMissionUpdateSystem_OnCreate_Patch
    {
        public static void Prefix(ServantMissionUpdateSystem __instance)
        {
            Storage.injuriesProgressOffline = Storage.Config.Bind<bool>("HuntControl.Injuries", "injuriesProgressOffline", true, "Let injuries heal while offline.");
            Storage.injuryTimeMultiplier = Storage.Config.Bind<float>("HuntControl.Injuries", "injuryTimeMultiplier", 0, "Speed up injury healing. 0 = disabled. 2 = 2x speed, etc.");
        }
    }

    [HarmonyPatch(typeof(ServantMissionUpdateSystem), "OnUpdate")]
    public static class ServantMissionUpdateSystem_OnUpdate_Patch
    {

        private static DateTime NoUpdateBefore = DateTime.MinValue;
        private static bool firstTick = true;

        public static void Prefix(ServantMissionUpdateSystem __instance)
        {
            if (NoUpdateBefore > DateTime.Now) return;

            if (firstTick)
            {
                float seconds = Storage.getSecondsSinceLastSave();
                Storage.logger.LogInfo("Processing all servant coffins...");
                int proc = InjuryHelper.processInjuries(__instance, seconds);
                Storage.logger.LogInfo("Processed " + proc.ToString() + " coffins. Injury time reduced by " + seconds.ToString() + " seconds.");
                firstTick = false;
                NoUpdateBefore = DateTime.Now.AddMilliseconds(60000);
                return;
            }

            if (Storage.injuryTimeMultiplier.Value > 0)
            {
                float seconds = Storage.getTimerMultiplierTick();
                if (seconds > 0) InjuryHelper.processInjuries(__instance, seconds);
            }

            NoUpdateBefore = DateTime.Now.AddMilliseconds(60000);
        }
    }
}
