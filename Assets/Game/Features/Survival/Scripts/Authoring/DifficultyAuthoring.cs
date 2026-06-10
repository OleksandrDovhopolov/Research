using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Survival
{
    // Designer-facing list of difficulty milestones. Order rows by
    // timeThreshold ascending — the runtime walks the buffer forward only.
    public class DifficultyAuthoring : MonoBehaviour
    {
        [System.Serializable]
        public class StageData
        {
            public float timeThreshold = 0f;
            public float hpMultiplier = 1f;
            public float damageMultiplier = 1f;
            public float spawnIntervalMultiplier = 1f;
            public int countPerWaveAddend = 0;
        }

        public List<StageData> stages = new();

        public class Baker : Baker<DifficultyAuthoring>
        {
            public override void Bake(DifficultyAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new DifficultyState
                {
                    ElapsedTime = 0f,
                    CurrentStageIndex = -1,  // tick will advance to 0 on first frame
                    HpMultiplier = 1f,
                    DamageMultiplier = 1f,
                    SpawnIntervalMultiplier = 1f,
                    CountPerWaveAddend = 0
                });

                DynamicBuffer<DifficultyStage> buffer = AddBuffer<DifficultyStage>(entity);
                if (authoring.stages != null)
                {
                    foreach (var s in authoring.stages)
                    {
                        buffer.Add(new DifficultyStage
                        {
                            TimeThreshold = s.timeThreshold,
                            HpMultiplier = s.hpMultiplier,
                            DamageMultiplier = s.damageMultiplier,
                            SpawnIntervalMultiplier = s.spawnIntervalMultiplier,
                            CountPerWaveAddend = s.countPerWaveAddend
                        });
                    }
                }
            }
        }
    }
}
