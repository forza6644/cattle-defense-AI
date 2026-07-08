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
            TowerData iceMage = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/_Game/ScriptableObjects/Towers/IceMageData.asset");
            TowerData machineGun = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/_Game/ScriptableObjects/Towers/MachineGunSoldierData.asset");
            TowerData sniperData = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/_Game/ScriptableObjects/Towers/SniperData.asset");

            WeaponDefinition arrowWeapon = CreateWeapon("ArcherWeapon", AttackType.SingleTarget, arrow, StatusEffectType.None);
            WeaponDefinition cannonWeapon = CreateWeapon("BombardierWeapon", AttackType.Splash, cannon, StatusEffectType.None);
            WeaponDefinition frostWeapon = CreateWeapon("FrostMageWeapon", AttackType.Slow, frost, StatusEffectType.Slow);
            WeaponDefinition fireWeapon = CreateWeapon("FireMageWeapon", AttackType.DoT, iceMage, StatusEffectType.Burn, 3f, 3f);
            WeaponDefinition electricWeapon = CreateWeapon("ElectricEngineerWeapon", AttackType.Chain, machineGun, StatusEffectType.Shock, 1f, 3f);
            WeaponDefinition sniperWeapon = CreateWeapon("SniperWeapon", AttackType.SingleTarget, sniperData, StatusEffectType.None);

            HeroDefinition archer = CreateHero("ArcherHero", "archer", "Archer", arrow, arrowWeapon);
            HeroDefinition bombardier = CreateHero("BombardierHero", "bombardier", "Bombardier", cannon, cannonWeapon);
            HeroDefinition frostMage = CreateHero("FrostMageHero", "frost_mage", "Frost Mage", frost, frostWeapon);
            HeroDefinition fireMage = CreateHero("FireMageHero", "fire_mage", "Fire Mage", iceMage, fireWeapon);
            HeroDefinition electricEngineer = CreateHero("ElectricEngineerHero", "electric_engineer", "Electric Engineer", machineGun, electricWeapon);
            HeroDefinition sniper = CreateHero("SniperHero", "sniper", "Sniper", sniperData, sniperWeapon);

            // Override specific parameters for the MVP heroes
            fireMage.baseDamage = 8f;
            fireMage.baseFireRate = 1.0f;
            fireMage.baseRange = 7f;
            EditorUtility.SetDirty(fireMage);

            electricEngineer.baseDamage = 5f;
            electricEngineer.baseFireRate = 1.2f;
            electricEngineer.baseRange = 7.5f;
            EditorUtility.SetDirty(electricEngineer);

            sniper.baseDamage = 30f;
            sniper.baseFireRate = 0.4f;
            sniper.baseRange = 13f;
            EditorUtility.SetDirty(sniper);

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            SetupScene(archer, bombardier, frostMage, fireMage, electricEngineer, sniper);
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

        private static WeaponDefinition CreateWeapon(string assetName, AttackType attackType, TowerData tower, StatusEffectType status, float statusValue = 0f, float statusDuration = 0f)
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

            if (status == StatusEffectType.Slow && tower != null)
            {
                weapon.statusEffectValue = tower.slowMultiplier;
                weapon.statusEffectDuration = tower.slowDuration;
            }
            else
            {
                weapon.statusEffectValue = statusValue;
                weapon.statusEffectDuration = statusDuration;
            }

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

        private static void SetupScene(
            HeroDefinition archer, HeroDefinition bombardier, HeroDefinition frostMage,
            HeroDefinition fireMage, HeroDefinition electricEngineer, HeroDefinition sniper)
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

            HeroDefinition[] heroes = { archer, bombardier, frostMage, fireMage, electricEngineer, sniper };

            for (int i = 0; i < heroes.Length; i++)
            {
                GameObject slotObject = new GameObject("HeroSlot_" + (i + 1).ToString("00"));
                slotObject.transform.SetParent(heroSlots.transform);

                if (towerSlots.Length > 0)
                {
                    int midIndex = towerSlots.Length / 2;
                    TowerSlot centerSlot = towerSlots[midIndex];

                    float spacing = 1.8f;
                    float offsetIndex = i - (heroes.Length - 1) / 2.0f;

                    slotObject.transform.position = centerSlot.transform.position + centerSlot.transform.right * (offsetIndex * spacing);
                    slotObject.transform.rotation = centerSlot.transform.rotation;
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
