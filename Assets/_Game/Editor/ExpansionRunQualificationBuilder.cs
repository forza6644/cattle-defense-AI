using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Editor
{
    public static class ExpansionRunQualificationBuilder
    {
        public const string Root = "Assets/_Game/ScriptableObjects/ExpansionRunQualification";
        public const string WaveRoot = Root + "/Waves";
        public const string StagePath = Root + "/StoneholdExpansionTrial.asset";
        public const string PoolPath = Root + "/ExpansionRun20.asset";
        public const string FixturePath = "Assets/_Game/Prefabs/ExpansionRunQualification/ExpansionBattlefieldAnchors.prefab";

        [MenuItem("Stonehold/Expansion/Build Task 13G Expansion Run")]
        public static void Build()
        {
            EnsureFolder("Assets/_Game/ScriptableObjects", "ExpansionRunQualification");
            EnsureFolder(Root, "Waves");
            EnsureFolder("Assets/_Game/Prefabs", "ExpansionRunQualification");

            CardPoolDefinition pool = BuildPool();
            GameObject fixture = BuildAnchorFixture();
            var enemy = new Dictionary<string, EnemyData>(StringComparer.Ordinal)
            {
                ["grunt"] = Load<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/GruntData.asset"),
                ["runner"] = Load<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/RunnerData.asset"),
                ["brute"] = Load<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/BruteData.asset"),
                ["armored"] = Load<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/ArmoredData.asset"),
                ["crossbow_raider"] = Load<EnemyData>("Assets/_Game/ScriptableObjects/EnemyExpansionQualification/CrossbowRaiderData.asset"),
                ["elite_war_shaman"] = Load<EnemyData>("Assets/_Game/ScriptableObjects/EnemyExpansionQualification/WarShamanData.asset"),
                ["warlord_boss"] = Load<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/BossData.asset")
            };

            WaveData[] waves =
            {
                Wave(1, "Foundation", E(enemy,"grunt",8,.55f,0f), E(enemy,"runner",4,.42f,.5f)),
                Wave(2, "Speed Pressure", E(enemy,"grunt",8,.5f,0f), E(enemy,"runner",12,.28f,.4f)),
                Wave(3, "Durable Frontline", E(enemy,"grunt",12,.45f,0f), E(enemy,"brute",6,.65f,.8f)),
                Wave(4, "Ranged Introduction", E(enemy,"grunt",14,.42f,0f), E(enemy,"crossbow_raider",6,.7f,.7f)),
                Wave(5, "Mixed Midpoint", E(enemy,"armored",8,.6f,0f), E(enemy,"runner",12,.3f,.5f), E(enemy,"crossbow_raider",6,.62f,.6f)),
                Wave(6, "Elite Introduction", E(enemy,"grunt",12,.42f,0f), E(enemy,"brute",6,.6f,.4f), E(enemy,"elite_war_shaman",1,1f,.8f)),
                Wave(7, "Combined Counterplay", E(enemy,"runner",20,.24f,0f), E(enemy,"armored",10,.48f,.5f), E(enemy,"crossbow_raider",8,.55f,.5f), E(enemy,"elite_war_shaman",1,1f,.9f)),
                Wave(8, "Heavy Pressure", E(enemy,"brute",12,.5f,0f), E(enemy,"armored",12,.46f,.4f), E(enemy,"crossbow_raider",10,.5f,.5f), E(enemy,"elite_war_shaman",2,1.2f,.8f)),
                Wave(9, "Peak Encounter", E(enemy,"runner",24,.18f,0f), E(enemy,"grunt",18,.24f,.35f), E(enemy,"armored",12,.4f,.45f), E(enemy,"crossbow_raider",12,.43f,.45f), E(enemy,"brute",2,.65f,.6f), E(enemy,"elite_war_shaman",2,1.2f,.8f)),
                Wave(10, "Warlord Finale", E(enemy,"grunt",10,.4f,0f), E(enemy,"armored",6,.52f,.5f), E(enemy,"crossbow_raider",4,.62f,.6f), E(enemy,"elite_war_shaman",1,1f,.8f), E(enemy,"warlord_boss",1,1f,1.2f))
            };

            StageData stage = GetOrCreate<StageData>(StagePath);
            stage.stageId = ExpansionRunValidation.StageId;
            stage.stageDisplayName = "Stonehold Expansion Trial";
            stage.stageDescription = "Isolated Task 13G ten-wave run combining four heroes, behavior upgrades, expanded enemies and fixed battlefield defenses.";
            stage.stageNumber = 13;
            stage.stageMode = StageMode.CastleDefense;
            stage.waves = waves;
            stage.enemyCountMultiplier = 1f;
            stage.spawnIntervalMultiplier = 1f;
            stage.cardPoolOverride = pool;
            stage.startingHeroId = "archer";
            stage.battlefieldFixturePrefab = fixture;
            stage.useExactWaveCounts = true;
            stage.expectedEnemyTypes = new[] { enemy["grunt"], enemy["runner"], enemy["brute"], enemy["armored"], enemy["crossbow_raider"], enemy["elite_war_shaman"], enemy["warlord_boss"] };
            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = stage;
            Debug.Log("[Task13G] Built isolated expansion stage, ten waves, ExpansionRun20 and battlefield anchor fixture.");
        }

        private static CardPoolDefinition BuildPool()
        {
            string[] paths =
            {
                "Assets/_Game/Resources/Cards/AddBombardier.asset", "Assets/_Game/Resources/Cards/AddFrostMage.asset", "Assets/_Game/Resources/Cards/AddElectricEngineer.asset",
                "Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/TwinVolley.asset", "Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/PiercingArrows.asset",
                "Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/ClusterShells.asset", "Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/WideBlast.asset",
                "Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/ShardVolley.asset", "Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/EchoingNova.asset",
                "Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/ExtendedCircuit.asset", "Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/ForkedCurrent.asset",
                "Assets/_Game/Resources/Cards/WarTraining.asset", "Assets/_Game/Resources/Cards/BattleRhythm.asset", "Assets/_Game/Resources/Cards/FastCasting.asset",
                "Assets/_Game/Resources/Cards/EmpoweredAbilities.asset", "Assets/_Game/Resources/Cards/Frostbite.asset", "Assets/_Game/Resources/Cards/WideBlast.asset",
                BattlefieldDefenseQualificationBuilder.CardRoot + "/DeployCaltrops.asset", BattlefieldDefenseQualificationBuilder.CardRoot + "/DeployBurningOil.asset",
                BattlefieldDefenseQualificationBuilder.CardRoot + "/DeployWoodenBarricade.asset"
            };
            CardPoolDefinition pool = GetOrCreate<CardPoolDefinition>(PoolPath);
            pool.stableId = ExpansionRunValidation.PoolId;
            pool.displayName = "Expansion Run 20";
            pool.description = "Isolated twenty-card pool for the Task 13G expansion trial.";
            pool.startingHeroId = "archer";
            pool.supportedHeroIds = new[] { "archer", "bombardier", "frost_mage", "electric_engineer" };
            pool.expectedCardCount = 20;
            pool.recruitOptionPolicy = RecruitOptionPolicy.GuaranteeWhileAvailable;
            pool.allowedCategories = new[] { CardCategory.Modifier, CardCategory.RecruitHero, CardCategory.HeroUpgrade, CardCategory.Trap, CardCategory.BattlefieldDefense };
            pool.allowedRarities = new[] { CardRarity.Common, CardRarity.Rare, CardRarity.Epic };
            pool.cards = new List<CardPoolEntry>(20);
            for (int i = 0; i < paths.Length; i++)
            {
                CardDefinition card = Load<CardDefinition>(paths[i]);
                float weight = card.cardCategory == CardCategory.RecruitHero ? 2.2f
                    : card.cardCategory == CardCategory.HeroUpgrade ? 1.1f
                    : card.cardCategory == CardCategory.Trap ? 0.8f
                    : card.cardCategory == CardCategory.BattlefieldDefense ? 0.7f : 1f;
                pool.cards.Add(new CardPoolEntry { card = card, rarity = card.rarity, weight = weight });
            }
            EditorUtility.SetDirty(pool);
            return pool;
        }

        private static GameObject BuildAnchorFixture()
        {
            GameObject root = new GameObject("ExpansionBattlefieldAnchors");
            AddAnchor(root.transform, "TrapAnchor_Front", BattlefieldAnchorType.Trap, new Vector3(-2.25f, 0f, 3.5f));
            AddAnchor(root.transform, "TrapAnchor_Mid", BattlefieldAnchorType.Trap, new Vector3(2.25f, 0f, -0.5f));
            AddAnchor(root.transform, "DefenseAnchor_Gate", BattlefieldAnchorType.Defense, new Vector3(0f, 0f, -4f));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, FixturePath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AddAnchor(Transform parent, string name, BattlefieldAnchorType type, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<BattlefieldAnchor>().Configure(type);
        }

        private static WaveData Wave(int number, string label, params WaveData.SpawnEntry[] entries)
        {
            string path = $"{WaveRoot}/ExpansionWave{number:00}.asset";
            WaveData wave = GetOrCreate<WaveData>(path);
            wave.waveLabel = label;
            wave.spawns = entries;
            EditorUtility.SetDirty(wave);
            return wave;
        }

        private static WaveData.SpawnEntry E(Dictionary<string, EnemyData> enemies, string id, int count, float interval, float delay) =>
            new WaveData.SpawnEntry { enemy = enemies[id], count = count, spawnInterval = interval, startDelay = delay };

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null) throw new InvalidOperationException("Missing required Task 13G source asset: " + path);
            return value;
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value != null) return value;
            value = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
