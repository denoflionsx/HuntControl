using HuntControl.DataStorage;
using ProjectM;
using ProjectM.Shared.Systems;
using Unity.Entities;

namespace HuntControl.Injuries
{
    public static class InjuryHelper
    {
        public static int processInjuries(ServantMissionUpdateSystem __instance, float reduction)
        {

            int proc = 0;

            if (reduction < 0) return proc;

            var servantInjuryQuery = __instance.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<ServantCoffinstation>());
            var injuryEntries = servantInjuryQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var injuryEntry in injuryEntries)
            {
                var injury = __instance.EntityManager.GetComponentData<ServantCoffinstation>(injuryEntry);
                if (injury.RecuperateEndTime > 0)
                {
                    if (injury.RecuperateEndTime - reduction < 0)
                    {
                        injury.RecuperateEndTime = 0;
                    }
                    else
                    {
                        injury.RecuperateEndTime -= reduction;
                    }
                    __instance.EntityManager.SetComponentData(injuryEntry, injury);
                    proc++;
                }
            }

            return proc;
        }
    }
}
