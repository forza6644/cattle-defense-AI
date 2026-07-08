using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Stonehold.EditorTools
{
    public static class HeroCastleDefenseSetup
    {
        private const string GameScenePath = "Assets/_Game/Scenes/GameScene.unity";
        private const string WeaponsFolder = "Assets/_Game/ScriptableObjects/Weapons";
        private const string HeroesFolder = "Assets/_Game/ScriptableObjects/Heroes";

        [MenuItem("Tools/Stonehold/Setup Hero Castle Defense Slice")]
        public static void Setup()
        {
            EnsureFolders();

            TowerData arrow = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/_Game/ScriptableObjects/Towers/ArrowTowerData.asset");
            TowerData cannon = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/_Game/ScriptableObjects/Towers/CannonTowerData.asset");
            TowerData frost = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/_Game/ScriptableObjects/Towers/FrostTowerData.asset");

            WeaponDefinition arrowWeapon = CreateWeapon("ArcherWeapon", AttackType.SingleTarget, arrow, StatusEffectType.None);
            WeaponDefinition cannonWeapon = CreateWeapon("BombardierWeapon", AttackType.Splash, cannon, StatusEffectType.None);
            WeaponDefinition frostWeapon = CreateWeapon("FrostMageWeapon", AttackType.Slow, frost, StatusEffectType.Slow);

            HeroDefinition archer = CreateHero("ArcherHero", "archer", "Archer", arrow, arrowWeapon);
            HeroDefinition bombardier = CreateHero("BombardierHero", "bombardier", "Bombardier", cannon, cannonWeapon);
            HeroDefinition frostMage = CreateHero("FrostMageHero", "frost_mage", "Frost Mage", frost, frostWeapon);

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            SetupScene(archer, bombardier, frostMage);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Hero castle defense slice setup complete.");
        }

        public static void SetupFromCommandLine()
        {
            Setup();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Game/ScriptableObjects");
            EnsureFolder(WeaponsFolder);
            EnsureFolder(HeroesFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static WeaponDefinition CreateWeapon(string assetName, AttackType attackType, TowerData tower, StatusEffectType status)
        {
            string path = WeaponsFolder + "/" + assetName + ".asset";
            WeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (weapon == null)
            {
                weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(weapon, path);
            }

            weapon.attackType = attackType;
            weapon.projectilePrefab = tower != null ? tower.projectilePrefab : null;
            weapon.splashRadius = attackType == AttackType.Splash && tower != null ? tower.splashRadius : 0f;
            weapon.statusEffectType = status;
            weapon.statusEffectValue = status == StatusEffectType.Slow && tower != null ? tower.slowMultiplier : 0f;
            weapon.statusEffectDuration = status == StatusEffectType.Slow && tower != null ? tower.slowDuration : 0f;
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        private static HeroDefinition CreateHero(string assetName, string id, string displayName, TowerData tower, WeaponDefinition weapon)
        {
            string path = HeroesFolder + "/" + assetName + ".asset";
            HeroDefinition hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(path);
            if (hero == null)
            {
                hero = ScriptableObject.CreateInstance<HeroDefinition>();
                AssetDatabase.CreateAsset(hero, path);
            }

            hero.id = id;
            hero.displayName = displayName;
            hero.icon = null;
            hero.heroPrefab = tower != null ? tower.prefab : null;
            hero.weapon = weapon;
            hero.baseDamage = tower != null ? tower.damage : 1f;
            hero.baseFireRate = tower != null ? tower.fireRate : 1f;
            hero.baseRange = tower != null ? tower.range : 5f;
            EditorUtility.SetDirty(hero);
            return hero;
        }

        private static void SetupScene(HeroDefinition archer, HeroDefinition bombardier, HeroDefinition frostMage)
        {
            GameObject systems = GameObject.Find("_Systems");
            if (systems == null)
            {
                systems = new GameObject("_Systems");
            }

            if (systems.GetComponent<DamageTracker>() == null)
            {
                systems.AddComponent<DamageTracker>();
            }

            TowerManager towerManager = Object.FindFirstObjectByType<TowerManager>();
            if (towerManager != null)
            {
                towerManager.enabled = false;
                EditorUtility.SetDirty(towerManager);
            }

            GameObject existingHeroSlots = GameObject.Find("HeroSlots");
            if (existingHeroSlots != null)
            {
                Object.DestroyImmediate(existingHeroSlots);
            }

            GameObject parentAnchor = GameObject.Find("CastleWall");
            if (parentAnchor == null)
            {
                parentAnchor = GameObject.Find("WallParapet");
            }

            GameObject heroSlots = new GameObject("HeroSlots");
            if (parentAnchor != null)
            {
                heroSlots.transform.SetParent(parentAnchor.transform);
            }

            TowerSlot[] towerSlots = Object.FindObjectsByType<TowerSlot>(FindObjectsSortMode.None);
            System.Array.Sort(towerSlots, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            HeroDefinition[] heroes = { archer, bombardier, frostMage };
            int startIndex = Mathf.Max(0, (towerSlots.Length - heroes.Length) / 2);

            for (int i = 0; i < heroes.Length; i++)
            {
                GameObject slotObject = new GameObject("HeroSlot_" + (i + 1).ToString("00"));
                slotObject.transform.SetParent(heroSlots.transform);

                if (towerSlots.Length > 0)
                {
                    TowerSlot source = towerSlots[Mathf.Clamp(startIndex + i, 0, towerSlots.Length - 1)];
                    slotObject.transform.position = source.transform.position;
                    slotObject.transform.rotation = source.transform.rotation;
                }
                else
                {
                    slotObject.transform.localPosition = Vector3.zero;
                    slotObject.transform.localRotation = Quaternion.identity;
                }

                HeroSlot heroSlot = slotObject.AddComponent<HeroSlot>();
                heroSlot.startingHero = heroes[i];
                EditorUtility.SetDirty(heroSlot);
            }
        }
    }
}
