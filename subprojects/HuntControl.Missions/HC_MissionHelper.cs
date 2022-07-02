using HuntControl.Lib;
using ProjectM;
using ProjectM.Shared.Systems;
using Unity.Entities;

namespace HuntControl.Missions
{
    public static class MissionHelper
    {

        public static int getNumberOfMissions(EntityManager __instance){
            var servantMissonQuery = __instance.CreateEntityQuery(ComponentType.ReadWrite<ActiveServantMission>());
            var missionEntities = servantMissonQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            var count = 0;

            foreach (var missionEntity in missionEntities)
            {
                var missionBuffer = __instance.GetBuffer<ActiveServantMission>(missionEntity);

                count += missionBuffer.Length;
            }

            return count;
        }


        public static int processMissions(EntityManager __instance, float reduction)
        {

            int missionsProcessed = 0;

            if (reduction < 0) return missionsProcessed;

            var servantMissonQuery = __instance.CreateEntityQuery(ComponentType.ReadWrite<ActiveServantMission>());
            var missionEntities = servantMissonQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var missionEntity in missionEntities)
            {
                var missionBuffer = __instance.GetBuffer<ActiveServantMission>(missionEntity);

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
                        Storage.logVerbose("Reducing mission " + mission.MissionID.ToString() + " time remaining by " + reduction.ToString() + " seconds.");
                        missionBuffer[i] = mission;
                    }
                }
            }

            return missionsProcessed;
        }
    }
}
