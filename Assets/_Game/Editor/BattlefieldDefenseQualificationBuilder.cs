using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Editor
{
    public static class BattlefieldDefenseQualificationBuilder
    {
        public const string Root = "Assets/_Game/ScriptableObjects/BattlefieldDefenseQualification";
        public const string CardRoot = Root + "/Cards";
        public const string PrefabRoot = "Assets/_Game/Prefabs/BattlefieldDefenseQualification";
        public const string MaterialRoot = "Assets/_Game/Art/Materials/BattlefieldDefenseQualification";
        public const string CaltropsPath = Root + "/Caltrops.asset";
        public const string OilPath = Root + "/BurningOil.asset";
        public const string BarricadePath = Root + "/WoodenBarricade.asset";
        public const string PoolPath = Root + "/Task13FQualificationPool.asset";

        [MenuItem("Stonehold/Expansion/Build Task 13F Battlefield Defenses")]
        public static void Build()
        {
            EnsureFolder("Assets/_Game/ScriptableObjects", "BattlefieldDefenseQualification");
            EnsureFolder(Root, "Cards");
            EnsureFolder("Assets/_Game/Prefabs", "BattlefieldDefenseQualification");
            EnsureFolder("Assets/_Game/Art/Materials", "BattlefieldDefenseQualification");

            GameObject caltropsPrefab = BuildDiscPrefab(PrefabRoot + "/Caltrops.prefab", "Caltrops", new Color(0.32f, 0.34f, 0.38f), true);
            GameObject oilPrefab = BuildDiscPrefab(PrefabRoot + "/BurningOil.prefab", "BurningOil", new Color(0.07f, 0.045f, 0.02f), false);
            GameObject barricadePrefab = BuildBarricadePrefab();

            TrapDefinition caltrops = GetOrCreate<TrapDefinition>(CaltropsPath);
            SetBase(caltrops, "trap_caltrops", "Caltrops", "Fixed lane spikes that slow groups and deal bounded physical damage.", caltropsPrefab, CardRarity.Common, 18f, 2f, 2f, 0.9f, BattlefieldEffectType.Slow, StatusEffectType.Slow, 0.72f, 1.2f);
            caltrops.runtimeType = TrapRuntimeType.Caltrops; caltrops.maxActive = 2; caltrops.maxTicksPerEnemy = 20; caltrops.ignitionDelay = 0f; caltrops.burningDuration = 0f; caltrops.retriggerCooldown = 0f;

            TrapDefinition oil = GetOrCreate<TrapDefinition>(OilPath);
            SetBase(oil, "trap_burning_oil", "Burning Oil", "A dark oil patch that ignites after an enemy enters and burns grouped targets.", oilPrefab, CardRarity.Rare, 20f, 3f, 2.5f, 0.75f, BattlefieldEffectType.Burn, StatusEffectType.Burn, 3f, 1.1f);
            oil.runtimeType = TrapRuntimeType.BurningOil; oil.maxActive = 1; oil.maxTicksPerEnemy = 8; oil.ignitionDelay = 0.65f; oil.burningDuration = 5f; oil.retriggerCooldown = 0f;

            BattlefieldDefenseDefinition barricade = GetOrCreate<BattlefieldDefenseDefinition>(BarricadePath);
            SetBase(barricade, "defense_wooden_barricade", "Wooden Barricade", "A fixed temporary blocker that buys time without attacking.", barricadePrefab, CardRarity.Rare, 0f, 0f, 0f, 0f, BattlefieldEffectType.Block, StatusEffectType.None, 0f, 0f);
            barricade.health = 90f; barricade.armor = 2f; barricade.maxActive = 1; barricade.placementMode = PlacementMode.LaneAnchor;

            CardDefinition c1 = BuildCard(CardRoot + "/DeployCaltrops.asset", "deploy_caltrops", "Deploy Caltrops", "Automatically deploy Caltrops at the first open trap anchor.", CardCategory.Trap, CardRarity.Common, caltrops, null);
            CardDefinition c2 = BuildCard(CardRoot + "/DeployBurningOil.asset", "deploy_burning_oil", "Deploy Burning Oil", "Automatically deploy Burning Oil at the first open trap anchor.", CardCategory.Trap, CardRarity.Rare, oil, null);
            CardDefinition c3 = BuildCard(CardRoot + "/DeployWoodenBarricade.asset", "deploy_wooden_barricade", "Deploy Wooden Barricade", "Automatically deploy a barricade at the fixed defense anchor.", CardCategory.BattlefieldDefense, CardRarity.Rare, null, barricade);
            BuildPool(c1, c2, c3);

            EditorUtility.SetDirty(caltrops); EditorUtility.SetDirty(oil); EditorUtility.SetDirty(barricade);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); Selection.activeObject = caltrops;
            Debug.Log("[Task13F] Built isolated traps, barricade, qualification cards and pool.");
        }

        private static void SetBase(BattlefieldContentDefinition value, string id, string title, string description, GameObject prefab, CardRarity rarity, float duration, float damage, float radius, float interval, BattlefieldEffectType effect, StatusEffectType status, float statusValue, float statusDuration)
        {
            value.stableId = id; value.displayName = title; value.description = description; value.prefab = prefab; value.rarity = rarity;
            value.placementMode = PlacementMode.LaneAnchor; value.duration = duration; value.charges = 1; value.damage = damage; value.effectRadius = radius;
            value.triggerInterval = interval; value.effectType = effect; value.statusEffectType = status; value.statusEffectValue = statusValue; value.statusEffectDuration = statusDuration;
        }

        private static CardDefinition BuildCard(string path, string id, string title, string description, CardCategory category, CardRarity rarity, TrapDefinition trap, BattlefieldDefenseDefinition defense)
        {
            CardDefinition card = GetOrCreate<CardDefinition>(path); card.id = id; card.displayName = title; card.description = description; card.cardCategory = category;
            card.rarity = rarity; card.weight = 1f; card.maxStacks = 1; card.targetType = CardTargetType.Global; card.trapDefinition = trap; card.battlefieldDefenseDefinition = defense;
            EditorUtility.SetDirty(card); AssetDatabase.SaveAssetIfDirty(card); return card;
        }

        private static void BuildPool(params CardDefinition[] cards)
        {
            CardPoolDefinition pool = GetOrCreate<CardPoolDefinition>(PoolPath); pool.stableId = "task13f_qualification"; pool.displayName = "Task 13F Qualification";
            pool.description = "Isolated controlled card pool for fixed battlefield anchors."; pool.startingHeroId = "archer"; pool.supportedHeroIds = new[] { "archer" };
            pool.expectedCardCount = 3; pool.recruitOptionPolicy = RecruitOptionPolicy.Weighted; pool.allowedCategories = new[] { CardCategory.Trap, CardCategory.BattlefieldDefense };
            pool.allowedRarities = new[] { CardRarity.Common, CardRarity.Rare }; pool.cards = new List<CardPoolEntry>();
            for (int i = 0; i < cards.Length; i++) pool.cards.Add(new CardPoolEntry { card = cards[i], rarity = cards[i].rarity, weight = 1f });
            EditorUtility.SetDirty(pool);
        }

        private static GameObject BuildDiscPrefab(string path, string name, Color color, bool spikes)
        {
            Material material = GetMaterial(MaterialRoot + "/" + name + ".mat", color);
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder); root.name = name; root.transform.localScale = new Vector3(1f, 0.05f, 1f);
            UnityEngine.Object.DestroyImmediate(root.GetComponent<Collider>()); root.GetComponent<Renderer>().sharedMaterial = material;
            if (spikes) for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f; GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube); spike.name = "Spike" + i; spike.transform.SetParent(root.transform, false);
                spike.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.55f, 2.2f, Mathf.Sin(angle) * 0.55f); spike.transform.localScale = new Vector3(0.08f, 3f, 0.08f); spike.transform.localRotation = Quaternion.Euler(25f, -i * 45f, 25f);
                UnityEngine.Object.DestroyImmediate(spike.GetComponent<Collider>()); spike.GetComponent<Renderer>().sharedMaterial = material;
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root); return prefab;
        }

        private static GameObject BuildBarricadePrefab()
        {
            string path = PrefabRoot + "/WoodenBarricade.prefab"; Material material = GetMaterial(MaterialRoot + "/WoodenBarricade.mat", new Color(0.42f, 0.22f, 0.08f));
            GameObject root = new GameObject("WoodenBarricade");
            for (int i = 0; i < 4; i++) { GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube); plank.name = "Plank" + i; plank.transform.SetParent(root.transform, false); plank.transform.localPosition = new Vector3(0f, 0.35f + i * 0.48f, 0f); plank.transform.localScale = new Vector3(3.2f, 0.38f, 0.38f); UnityEngine.Object.DestroyImmediate(plank.GetComponent<Collider>()); plank.GetComponent<Renderer>().sharedMaterial = material; }
            BoxCollider collider = root.AddComponent<BoxCollider>(); collider.size = new Vector3(3.3f, 2.2f, 0.5f); collider.center = new Vector3(0f, 1f, 0f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root); return prefab;
        }

        private static Material GetMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.color = color; EditorUtility.SetDirty(material); return material;
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path); if (value != null) return value;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
            value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); return value;
        }

        private static void EnsureFolder(string parent, string child) { string path = parent + "/" + child; if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child); }
    }
}
