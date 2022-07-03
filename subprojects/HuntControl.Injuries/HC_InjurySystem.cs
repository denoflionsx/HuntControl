using HuntControl.Lib;
using ProjectM.Shared.Systems;
using System;

namespace HuntControl.Injuries
{

    public static class InjurySystem
    {
        public static void Apply()
        {
            Storage.createCallbacks.Add(typeof(InjurySystem_Create).Name, InjurySystem_Create.Prefix);
            Storage.timeouts.Add(typeof(InjurySystem).Name, new Timeout(Register, 35));
            Storage.logger.LogInfo("Injury module will start in 35 seconds.");
        }

        public static void Register(bool b)
        {
            Storage.updateCallbacks.Add(typeof(InjurySystem_Update).Name, InjurySystem_Update.Prefix);
            Storage.destroyCallbacks.Add(typeof(InjurySystem_Destroy).Name, InjurySystem_Destroy.Prefix);
        }
    }

    // Setup initial data.
    public static class InjurySystem_Create
    {
        public static void Prefix(ServantMissionUpdateSystem __instance)
        {
            Storage.injuriesProgressOffline = Storage.Config.Bind<bool>("HuntControl.Injuries", "injuriesProgressOffline", true, "Let injuries heal while offline.");
            Storage.injuryTimeMultiplier = Storage.Config.Bind<float>("HuntControl.Injuries", "injuryTimeMultiplier", 0, "Speed up injury healing. 0 = disabled. 2 = 2x speed, etc.");
            Storage.forceCompleteAllInjuries = Storage.Config.Bind<bool>("HuntControl.Injuries", "forceCompleteAllInjuries", false, "This option will force every injury to heal immediately on boot then disable itself.");
        }
    }

    public static class InjurySystem_Destroy
    {
        public static void Prefix(ServantMissionUpdateSystem __instance) {
            Storage.forceCompleteAllInjuries.Value = false;
        }
    }

    public static class InjurySystem_Update
    {

        private static DateTime NoUpdateBefore = DateTime.MinValue;
        private static bool firstTick = true;

        public static void Prefix(ServantMissionUpdateSystem __instance)
        {
            if (NoUpdateBefore > DateTime.Now) return;

            if (firstTick)
            {
                float seconds = Storage.getSecondsSinceLastSave(Storage.injuryTimeMultiplier.Value);
                Storage.logger.LogInfo("Processing all servant coffins...");
                int proc = InjuryHelper.processInjuries(__instance, seconds, Storage.forceCompleteAllInjuries.Value);
                Storage.logger.LogInfo("Processed " + proc.ToString() + " coffins. Injury time reduced by " + seconds.ToString() + " seconds.");
                firstTick = false;
                NoUpdateBefore = DateTime.Now.AddMilliseconds(60000);
                return;
            }

            if (Storage.injuryTimeMultiplier.Value > 0)
            {
                float seconds = Storage.getInjuryTimerMultiplierTick();
                if (seconds > 0) InjuryHelper.processInjuries(__instance, seconds);
            }

            NoUpdateBefore = DateTime.Now.AddMilliseconds(60000);
        }
    }
}
