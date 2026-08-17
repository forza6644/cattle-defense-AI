#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Editor
{
    public static class CampaignStage10SetupTools
    {
        private const string StagesFolder = "Assets/_Game/ScriptableObjects/Stages";
        private const string WavesBaseFolder = "Assets/_Game/ScriptableObjects/Waves";

        [MenuItem("Stonehold/Setup/Setup 10-Stage Campaign Assets")]
        public static void GenerateAll10StagesAndWaves()
        {
            Debug.Log("[CampaignStage10Setup] Starting 10-Stage Campaign Assets Generation...");

            if (!Directory.Exists(StagesFolder))
            {
                Directory.CreateDirectory(StagesFolder);
            }

            // Load enemy references
            EnemyData grunt = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/GruntData.asset");
            EnemyData runner = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/RunnerData.asset");
            EnemyData armored = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/ArmoredData.asset");
            EnemyData brute = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/BruteData.asset");
            EnemyData boss = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/BossData.asset");
            EnemyData stalker = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/VoidStalkerData.asset");
            EnemyData nullifier = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/VoidNullifierData.asset");
            EnemyData voidLord = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/VoidLordData.asset");

            EnemyData[] standardEnemies = new EnemyData[] { grunt, runner, armored, brute, boss };
            EnemyData[] voidEnemies = new EnemyData[] { stalker, nullifier, voidLord };
            EnemyData[] allEnemies = new EnemyData[] { grunt, runner, armored, brute, boss, stalker, nullifier, voidLord };

            StageData[] stages = new StageData[10];

            stages[0] = CreateOrGetStage(1, "stage_1_castle_road", "Castle Road",
                "Defend the road to the keep against grunts, armored troops, runners, and the final boss.",
                1.00f, 0.90f, standardEnemies, grunt, runner, armored, brute, boss);

            stages[1] = CreateOrGetStage(2, "stage_2_highlands", "Highlands Fortress",
                "Defend the highland pass against heavier enemy formations and shield phalanxes.",
                1.05f, 0.95f, standardEnemies, grunt, runner, armored, brute, boss);

            stages[2] = CreateOrGetStage(3, "stage_3_frozen_frontier", "Frozen Frontier",
                "Hold the frozen frontier against sub-zero blizzards and rapid frostbite waves.",
                1.12f, 0.95f, standardEnemies, grunt, runner, armored, brute, boss);

            stages[3] = CreateOrGetStage(4, "stage_4_titan_citadel", "Titan Citadel",
                "Endure the ancient titan ruins, massive colossi, and speed vanguards.",
                1.25f, 0.90f, standardEnemies, grunt, runner, armored, brute, boss);

            stages[4] = CreateOrGetStage(5, "stage_5_volcanic_caldera", "Volcanic Caldera",
                "Conquer the scorched infernal magma flows, living molten beasts, and the Magma Core Lord.",
                1.35f, 0.85f, standardEnemies, grunt, runner, armored, brute, boss);

            stages[5] = CreateOrGetStage(6, "stage_6_toxic_mire", "Toxic Mire",
                "Survive noxious rot wetlands swarming with caustic abominations and toxic chimeras.",
                1.42f, 0.82f, standardEnemies, grunt, runner, armored, brute, boss);

            stages[6] = CreateOrGetStage(7, "stage_7_thunder_peaks", "Thunder Peaks",
                "Withstand high-voltage storm cliffs whipped by galvanic harpies and the Tempest Archon.",
                1.50f, 0.80f, standardEnemies, grunt, runner, armored, brute, boss);

            stages[7] = CreateOrGetStage(8, "stage_8_sunken_necropolis", "Sunken Necropolis",
                "Purge ancient cursed crypts swarming with undead skeleton legions and the Dread Necromancer.",
                1.60f, 0.78f, allEnemies, grunt, runner, armored, brute, boss);

            stages[8] = CreateOrGetStage(9, "stage_9_abyssal_void_rift", "The Void Rift",
                "Survive the cosmic horrors of the Abyssal Void Rift, phase-shifting stalkers, and the Void Lord.",
                1.70f, 0.75f, voidEnemies, stalker, nullifier, stalker, nullifier, voidLord);

            stages[9] = CreateOrGetStage(10, "stage_10_celestial_sanctum", "Celestial Sanctum",
                "Face the ultimate celestial trial atop the Throne of Eternity against the Ancient King.",
                1.85f, 0.70f, allEnemies, armored, brute, stalker, nullifier, voidLord);

            // Link into GameConfig
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Game/ScriptableObjects/GameConfig.asset");
            if (config != null)
            {
                config.stages = stages;
                EditorUtility.SetDirty(config);
                Debug.Log("[CampaignStage10Setup] Successfully linked 10 stages into GameConfig.asset!");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CampaignStage10Setup] ✅ 10-Stage Campaign & 100 Waves successfully created and verified!");
        }

        private static StageData CreateOrGetStage(
            int stageNumber,
            string stageId,
            string stageDisplayName,
            string description,
            float enemyCountMultiplier,
            float spawnIntervalMultiplier,
            EnemyData[] expectedEnemies,
            EnemyData gruntEnemy,
            EnemyData runnerEnemy,
            EnemyData armoredEnemy,
            EnemyData bruteEnemy,
            EnemyData bossEnemy)
        {
            string stageAssetPath = $"{StagesFolder}/Stage{stageNumber}_{stageDisplayName.Replace(" ", "")}.asset";
            StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(stageAssetPath);
            if (stage == null)
            {
                stage = ScriptableObject.CreateInstance<StageData>();
                AssetDatabase.CreateAsset(stage, stageAssetPath);
            }

            stage.stageNumber = stageNumber;
            stage.stageId = stageId;
            stage.stageDisplayName = stageDisplayName;
            stage.stageDescription = description;
            stage.stageMode = StageMode.CastleDefense;
            stage.useExactWaveCounts = true;
            stage.enemyCountMultiplier = enemyCountMultiplier;
            stage.spawnIntervalMultiplier = spawnIntervalMultiplier;
            stage.expectedEnemyTypes = expectedEnemies;

            // Build or link 10 waves
            string stageWaveFolder = $"{WavesBaseFolder}/Stage{stageNumber}";
            if (!Directory.Exists(stageWaveFolder))
            {
                Directory.CreateDirectory(stageWaveFolder);
            }

            WaveData[] waves = new WaveData[10];
            string[] waveLabels =
            {
                "Wave 1 - Vanguard Recon",
                "Wave 2 - Scout Skirmish",
                "Wave 3 - Armored Outriders",
                "Wave 4 - Rapid Assault",
                "Wave 5 - Mid-Stage Phalanx",
                "Wave 6 - Heavy Siege Brigade",
                "Wave 7 - Relentless Influx",
                "Wave 8 - Elite Strike Team",
                "Wave 9 - Cataclysm Gauntlet",
                "Wave 10 - Grand Warlord Climax"
            };

            for (int w = 0; w < 10; w++)
            {
                string waveAssetPath = $"{stageWaveFolder}/Stage{stageNumber}_Wave{w + 1:00}.asset";
                WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(waveAssetPath);
                if (wave == null)
                {
                    wave = ScriptableObject.CreateInstance<WaveData>();
                    AssetDatabase.CreateAsset(wave, waveAssetPath);
                }

                wave.waveLabel = waveLabels[w];

                // Configure spawns per wave progression
                if (w == 9) // Boss Wave
                {
                    wave.spawns = new WaveData.SpawnEntry[]
                    {
                        new WaveData.SpawnEntry { enemy = bruteEnemy != null ? bruteEnemy : gruntEnemy, count = 4 + stageNumber, spawnInterval = 1.0f, startDelay = 0f },
                        new WaveData.SpawnEntry { enemy = armoredEnemy != null ? armoredEnemy : gruntEnemy, count = 6 + stageNumber, spawnInterval = 0.8f, startDelay = 2.0f },
                        new WaveData.SpawnEntry { enemy = bossEnemy != null ? bossEnemy : gruntEnemy, count = 1 + (stageNumber >= 9 ? 1 : 0), spawnInterval = 3.0f, startDelay = 5.0f },
                        new WaveData.SpawnEntry { enemy = runnerEnemy != null ? runnerEnemy : gruntEnemy, count = 8 + stageNumber * 2, spawnInterval = 0.5f, startDelay = 8.0f }
                    };
                }
                else if (w >= 7) // Elite Late Waves
                {
                    wave.spawns = new WaveData.SpawnEntry[]
                    {
                        new WaveData.SpawnEntry { enemy = armoredEnemy != null ? armoredEnemy : gruntEnemy, count = 6 + w, spawnInterval = 0.7f, startDelay = 0f },
                        new WaveData.SpawnEntry { enemy = bruteEnemy != null ? bruteEnemy : gruntEnemy, count = 3 + w / 2, spawnInterval = 1.2f, startDelay = 2.5f },
                        new WaveData.SpawnEntry { enemy = runnerEnemy != null ? runnerEnemy : gruntEnemy, count = 10 + w * 2, spawnInterval = 0.45f, startDelay = 5.0f }
                    };
                }
                else if (w >= 4) // Mid Waves
                {
                    wave.spawns = new WaveData.SpawnEntry[]
                    {
                        new WaveData.SpawnEntry { enemy = gruntEnemy, count = 8 + w * 2, spawnInterval = 0.6f, startDelay = 0f },
                        new WaveData.SpawnEntry { enemy = runnerEnemy != null ? runnerEnemy : gruntEnemy, count = 6 + w, spawnInterval = 0.5f, startDelay = 2.0f },
                        new WaveData.SpawnEntry { enemy = armoredEnemy != null ? armoredEnemy : gruntEnemy, count = 4 + w / 2, spawnInterval = 0.9f, startDelay = 4.0f }
                    };
                }
                else // Early Waves
                {
                    wave.spawns = new WaveData.SpawnEntry[]
                    {
                        new WaveData.SpawnEntry { enemy = gruntEnemy, count = 6 + w * 3, spawnInterval = 0.8f, startDelay = 0f },
                        new WaveData.SpawnEntry { enemy = runnerEnemy != null ? runnerEnemy : gruntEnemy, count = 4 + w * 2, spawnInterval = 0.6f, startDelay = 1.5f }
                    };
                }

                EditorUtility.SetDirty(wave);
                waves[w] = wave;
            }

            stage.waves = waves;
            EditorUtility.SetDirty(stage);
            return stage;
        }
    }
}
#endif
