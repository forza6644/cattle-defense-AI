using System;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Editor
{
    public static class EnemyRosterExpansionBuilder
    {
        public const string RaiderId = "crossbow_raider";
        public const string ShamanId = "elite_war_shaman";
        public const string RaiderDataPath = "Assets/_Game/ScriptableObjects/EnemyExpansionQualification/CrossbowRaiderData.asset";
        public const string ShamanDataPath = "Assets/_Game/ScriptableObjects/EnemyExpansionQualification/WarShamanData.asset";
        public const string QualificationWavePath = "Assets/_Game/ScriptableObjects/EnemyExpansionQualification/Task13E_QualificationWave.asset";
        public const string RaiderPrefabPath = "Assets/_Game/Prefabs/EnemyExpansion/CrossbowRaider.prefab";
        public const string ShamanPrefabPath = "Assets/_Game/Prefabs/EnemyExpansion/WarShaman.prefab";
        public const string ProjectilePrefabPath = "Assets/_Game/Prefabs/EnemyExpansion/CrossbowBolt.prefab";

        [MenuItem("Stonehold/Expansion/Build Task 13E Enemy Roster")]
        public static void Build()
        {
            EnsureFolder("Assets/_Game/Prefabs", "EnemyExpansion");
            EnsureFolder("Assets/_Game/ScriptableObjects", "EnemyExpansionQualification");
            EnsureFolder("Assets/_Game/Art/Materials", "EnemyExpansion");

            Material raiderMaterial = GetOrCreateMaterial("Assets/_Game/Art/Materials/EnemyExpansion/CrossbowRaiderAccent.mat", new Color(0.16f, 0.55f, 0.75f));
            Material shamanMaterial = GetOrCreateMaterial("Assets/_Game/Art/Materials/EnemyExpansion/WarShamanAccent.mat", new Color(0.55f, 0.18f, 0.78f));
            Material projectileMaterial = GetOrCreateMaterial("Assets/_Game/Art/Materials/EnemyExpansion/CrossbowBolt.mat", new Color(1f, 0.55f, 0.08f));

            GameObject projectilePrefab = BuildProjectile(projectileMaterial);
            GameObject raiderPrefab = BuildEnemyPrefab("Assets/_Game/Prefabs/Enemies/RunnerEnemy.prefab", RaiderPrefabPath, "CrossbowRaider", new Vector3(0.82f, 0.82f, 0.82f));
            AddCrossbowVisual(raiderPrefab, raiderMaterial);
            PrefabUtility.SaveAsPrefabAsset(raiderPrefab, RaiderPrefabPath);
            UnityEngine.Object.DestroyImmediate(raiderPrefab);
            raiderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RaiderPrefabPath);

            GameObject shamanPrefab = BuildEnemyPrefab("Assets/_Game/Prefabs/Enemies/ArmoredEnemy.prefab", ShamanPrefabPath, "WarShaman", new Vector3(1.15f, 1.15f, 1.15f));
            AddStaffVisual(shamanPrefab, shamanMaterial);
            PrefabUtility.SaveAsPrefabAsset(shamanPrefab, ShamanPrefabPath);
            UnityEngine.Object.DestroyImmediate(shamanPrefab);
            shamanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShamanPrefabPath);

            EnemyData raider = GetOrCreateEnemyData(RaiderDataPath);
            raider.stableId = RaiderId;
            raider.enemyName = "Crossbow Raider";
            raider.prefab = raiderPrefab;
            raider.classification = EnemyClassification.Normal;
            raider.health = 17f;
            raider.moveSpeed = 3f;
            raider.armor = 0f;
            raider.goldReward = 6;
            raider.xpValue = 6;
            raider.castleDamage = 2;
            raider.specialRole = EnemySpecialRole.RangedCastleAttacker;
            raider.rangedAttack.standOffRange = 5.5f;
            raider.rangedAttack.windUpSeconds = 0.75f;
            raider.rangedAttack.cooldownSeconds = 2.1f;
            raider.rangedAttack.projectileSpeed = 10f;
            raider.rangedAttack.projectilePrefab = projectilePrefab;

            EnemyData shaman = GetOrCreateEnemyData(ShamanDataPath);
            shaman.stableId = ShamanId;
            shaman.enemyName = "War Shaman";
            shaman.prefab = shamanPrefab;
            shaman.classification = EnemyClassification.Elite;
            shaman.health = 75f;
            shaman.moveSpeed = 1.7f;
            shaman.armor = 1f;
            shaman.goldReward = 45;
            shaman.xpValue = 40;
            shaman.castleDamage = 3;
            shaman.specialRole = EnemySpecialRole.HealingElite;
            shaman.healingPulse.intervalSeconds = 5f;
            shaman.healingPulse.castSeconds = 1f;
            shaman.healingPulse.radius = 4f;
            shaman.healingPulse.maxHealthFraction = 0.12f;
            shaman.healingPulse.selfHealMultiplier = 0.5f;
            shaman.healingPulse.targetCap = 5;
            shaman.healingPulse.excludeBoss = true;

            EditorUtility.SetDirty(raider);
            EditorUtility.SetDirty(shaman);
            BuildQualificationWave(raider, shaman);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = raider;
            Debug.Log("[Task13E] Built isolated Crossbow Raider and War Shaman qualification assets.");
        }

        private static GameObject BuildProjectile(Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "CrossbowBolt";
            root.transform.localScale = new Vector3(0.14f, 0.14f, 0.38f);
            UnityEngine.Object.DestroyImmediate(root.GetComponent<Collider>());
            root.GetComponent<Renderer>().sharedMaterial = material;
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.35f;
            trail.startWidth = 0.13f;
            trail.endWidth = 0.01f;
            trail.minVertexDistance = 0.04f;
            root.AddComponent<EnemyCastleProjectile>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildEnemyPrefab(string sourcePath, string destinationPath, string name, Vector3 scale)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) throw new InvalidOperationException("Missing source enemy prefab: " + sourcePath);
            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = name;
            instance.transform.localScale = scale;
            if (instance.GetComponent<EnemySpecialBehavior>() == null) instance.AddComponent<EnemySpecialBehavior>();
            PrefabUtility.SaveAsPrefabAsset(instance, destinationPath);
            return instance;
        }

        private static void AddCrossbowVisual(GameObject root, Material material)
        {
            if (root.transform.Find("PrototypeCrossbow") != null) return;
            GameObject crossbow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossbow.name = "PrototypeCrossbow";
            crossbow.transform.SetParent(root.transform, false);
            crossbow.transform.localPosition = new Vector3(0f, 1.05f, 0.28f);
            crossbow.transform.localScale = new Vector3(0.9f, 0.08f, 0.12f);
            UnityEngine.Object.DestroyImmediate(crossbow.GetComponent<Collider>());
            crossbow.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void AddStaffVisual(GameObject root, Material material)
        {
            if (root.transform.Find("PrototypeStaff") != null) return;
            GameObject staff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            staff.name = "PrototypeStaff";
            staff.transform.SetParent(root.transform, false);
            staff.transform.localPosition = new Vector3(0.55f, 1.05f, 0f);
            staff.transform.localScale = new Vector3(0.08f, 0.75f, 0.08f);
            UnityEngine.Object.DestroyImmediate(staff.GetComponent<Collider>());
            staff.GetComponent<Renderer>().sharedMaterial = material;
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "StaffFocus";
            orb.transform.SetParent(staff.transform, false);
            orb.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            orb.transform.localScale = Vector3.one * 2.2f;
            UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());
            orb.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static EnemyData GetOrCreateEnemyData(string path)
        {
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static void BuildQualificationWave(EnemyData raider, EnemyData shaman)
        {
            WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(QualificationWavePath);
            if (wave == null)
            {
                wave = ScriptableObject.CreateInstance<WaveData>();
                AssetDatabase.CreateAsset(wave, QualificationWavePath);
            }
            wave.waveLabel = "Task 13E Qualification Only";
            wave.spawns = new[]
            {
                new WaveData.SpawnEntry { enemy = raider, count = 6, spawnInterval = 0.7f },
                new WaveData.SpawnEntry { enemy = shaman, count = 1, spawnInterval = 1f }
            };
            EditorUtility.SetDirty(wave);
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
