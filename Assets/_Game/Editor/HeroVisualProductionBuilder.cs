#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stonehold.EditorTools
{
    /// <summary>
    /// Phase 2: Full Hero Visual Production Builder.
    /// Builds and polishes all 10 heroes to the approved Archer benchmark standard.
    /// </summary>
    public static class HeroVisualProductionBuilder
    {
        private const string ModelsFolder = "Assets/_Game/Art/Characters/QuaterniusRPG/Models/";
        private const string ControllersFolder = "Assets/_Game/Art/Characters/QuaterniusRPG/Controllers/";
        private const string MaterialsFolder = "Assets/_Game/Art/Materials/Heroes/";
        private const string AdaptersFolder = "Assets/_Game/Prefabs/ArtAdapters/";
        private const string ProjectilesFolder = "Assets/_Game/Prefabs/Projectiles/";
        private const string HeroSOFolder = "Assets/_Game/ScriptableObjects/Heroes/";
        private const string HeroResourcesFolder = "Assets/_Game/Resources/Heroes/";
        private const string WeaponSOFolder = "Assets/_Game/ScriptableObjects/Weapons/";
        private const string WeaponResourcesFolder = "Assets/_Game/Resources/Weapons/";

        [MenuItem("Tools/Stonehold/Phase 2 - Build All Heroes Visuals")]
        public static void BuildAllHeroesVisuals()
        {
            EnsureDirectories();
            BuildAllMaterials();
            BuildAllProjectilePrefabs();
            BuildAllHeroAdapters();
            UpdateWeaponAndHeroDefinitions();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HeroVisualProductionBuilder] Full hero visual production complete!");
        }

        private static void EnsureDirectories()
        {
            EnsureFolder("Assets/_Game/Art/Materials/Heroes");
            EnsureFolder("Assets/_Game/Art/Icons/Heroes");
            EnsureFolder("Assets/_Game/Prefabs/Projectiles");
            EnsureFolder("Assets/_Game/Prefabs/ArtAdapters");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static Shader GetURPLitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return shader;
        }

        public static Material CreateHeroMaterial(string matName, Color baseColor, float metallic, float smoothness, Texture mainTex = null)
        {
            string path = MaterialsFolder + matName + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(GetURPLitShader());
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = GetURPLitShader();
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (mainTex != null)
            {
                mat.mainTexture = mainTex;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static void BuildAllMaterials()
        {
            // Base textures
            Texture2D texWarrior = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Characters/QuaterniusRPG/Textures/Warrior_Texture.png");
            Texture2D texCleric = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Characters/QuaterniusRPG/Textures/Cleric_Texture.png");
            Texture2D texWizard = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Characters/QuaterniusRPG/Textures/Wizard_Texture.png");
            Texture2D texMonk = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Characters/QuaterniusRPG/Textures/Monk_Texture.png");
            Texture2D texRogue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Characters/QuaterniusRPG/Textures/Rogue_Texture.png");
            Texture2D texRanger = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Characters/QuaterniusRPG/Textures/Ranger_Texture.png");

            // 1. Archer (Benchmark)
            CreateHeroMaterial("Mat_Archer_Body", Color.white, 0.05f, 0.12f, texRanger);
            CreateHeroMaterial("Mat_Archer_Weapon", Color.white, 0.15f, 0.20f, AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Characters/QuaterniusRPG/Textures/Ranger_Bow_Texture.png"));

            // 2. Bombardier (Dark steel plate, bronze trim, dark leather)
            CreateHeroMaterial("Mat_Bombardier_Body", new Color(0.62f, 0.60f, 0.65f), 0.35f, 0.28f, texWarrior);
            CreateHeroMaterial("Mat_Bombardier_Weapon", new Color(0.85f, 0.68f, 0.28f), 0.70f, 0.50f, null);

            // 3. Frost Mage (Sapphire blue, crystalline cyan, glacial white)
            CreateHeroMaterial("Mat_FrostMage_Body", new Color(0.55f, 0.75f, 1.0f), 0.08f, 0.25f, texCleric);
            CreateHeroMaterial("Mat_FrostMage_Weapon", new Color(0.40f, 0.88f, 1.0f), 0.25f, 0.75f, AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Characters/QuaterniusRPG/Textures/Cleric_Staff_Texture.png"));

            // 4. Fire Mage (Crimson, fiery orange, charcoal, molten gold)
            CreateHeroMaterial("Mat_FireMage_Body", new Color(1.0f, 0.55f, 0.45f), 0.12f, 0.22f, texWizard);
            CreateHeroMaterial("Mat_FireMage_Weapon", new Color(1.0f, 0.48f, 0.15f), 0.55f, 0.60f, AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Characters/QuaterniusRPG/Textures/Wizard_Staff_Texture.png"));

            // 5. Electric Engineer (Copper, brass, gunmetal, electric cyan)
            CreateHeroMaterial("Mat_ElectricEngineer_Body", new Color(0.92f, 0.78f, 0.52f), 0.40f, 0.35f, texMonk);
            CreateHeroMaterial("Mat_ElectricEngineer_Weapon", new Color(0.95f, 0.65f, 0.25f), 0.85f, 0.60f, null);

            // 6. Sniper (Graphite slate, dark navy, precision brass)
            CreateHeroMaterial("Mat_Sniper_Body", new Color(0.58f, 0.62f, 0.72f), 0.22f, 0.28f, texRogue);
            CreateHeroMaterial("Mat_Sniper_Weapon", new Color(0.35f, 0.38f, 0.45f), 0.75f, 0.55f, null);

            // 7. Plague Doctor (Dark emerald, toxic green, dark leather, antique brass)
            CreateHeroMaterial("Mat_PlagueDoctor_Body", new Color(0.45f, 0.82f, 0.55f), 0.15f, 0.22f, texWizard);
            CreateHeroMaterial("Mat_PlagueDoctor_Weapon", new Color(0.25f, 0.95f, 0.40f), 0.20f, 0.70f, null);

            // 8. Radiant Paladin (Ivory plate, radiant gold, celestial blue)
            CreateHeroMaterial("Mat_RadiantPaladin_Body", new Color(1.0f, 0.96f, 0.88f), 0.55f, 0.48f, texWarrior);
            CreateHeroMaterial("Mat_RadiantPaladin_Weapon", new Color(1.0f, 0.85f, 0.25f), 0.85f, 0.65f, AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Characters/QuaterniusRPG/Textures/Warrior_Sword_Texture.png"));

            // 9. Shadow Assassin (Midnight obsidian, deep violet, glowing void purple)
            CreateHeroMaterial("Mat_ShadowAssassin_Body", new Color(0.52f, 0.42f, 0.68f), 0.22f, 0.25f, texRogue);
            CreateHeroMaterial("Mat_ShadowAssassin_Weapon", new Color(0.68f, 0.28f, 0.95f), 0.45f, 0.60f, AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Characters/QuaterniusRPG/Textures/Rogue_Dagger_Texture.png"));

            // 10. Storm Druid (Tempest teal, storm gray, storm cyan, aged wood)
            CreateHeroMaterial("Mat_StormDruid_Body", new Color(0.45f, 0.85f, 0.82f), 0.12f, 0.22f, texMonk);
            CreateHeroMaterial("Mat_StormDruid_Weapon", new Color(0.18f, 0.88f, 0.92f), 0.35f, 0.55f, null);
        }

        public static void BuildAllProjectilePrefabs()
        {
            GameObject baseProj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Projectile.prefab");
            if (baseProj == null)
            {
                Debug.LogError("Base Projectile.prefab not found!");
                return;
            }

            var configs = new[] {
                new { id = "bombardier", color = new Color(1.0f, 0.45f, 0.08f, 1f), scale = 1.35f, trailWidth = 0.22f },
                new { id = "frost_mage", color = new Color(0.35f, 0.85f, 1.0f, 1f), scale = 1.0f, trailWidth = 0.16f },
                new { id = "fire_mage", color = new Color(1.0f, 0.32f, 0.05f, 1f), scale = 1.15f, trailWidth = 0.20f },
                new { id = "electric_engineer", color = new Color(0.20f, 0.88f, 1.0f, 1f), scale = 0.95f, trailWidth = 0.14f },
                new { id = "sniper", color = new Color(0.85f, 0.40f, 1.0f, 1f), scale = 0.85f, trailWidth = 0.12f },
                new { id = "plague_doctor", color = new Color(0.25f, 0.95f, 0.35f, 1f), scale = 1.1f, trailWidth = 0.18f },
                new { id = "radiant_paladin", color = new Color(1.0f, 0.88f, 0.25f, 1f), scale = 1.15f, trailWidth = 0.18f },
                new { id = "shadow_assassin", color = new Color(0.72f, 0.22f, 0.95f, 1f), scale = 0.95f, trailWidth = 0.15f },
                new { id = "storm_druid", color = new Color(0.15f, 0.92f, 0.92f, 1f), scale = 1.1f, trailWidth = 0.18f }
            };

            foreach (var cfg in configs)
            {
                string prefabPath = $"{ProjectilesFolder}Projectile_{cfg.id}.prefab";
                GameObject instance = UnityEngine.Object.Instantiate(baseProj);
                instance.name = $"Projectile_{cfg.id}";

                var trail = instance.GetComponent<TrailRenderer>();
                if (trail != null)
                {
                    trail.startColor = cfg.color;
                    trail.endColor = new Color(cfg.color.r, cfg.color.g, cfg.color.b, 0f);
                    trail.startWidth = cfg.trailWidth;
                    trail.endWidth = 0.02f;
                    trail.time = 0.22f;
                }

                var r = instance.GetComponent<Renderer>();
                if (r != null)
                {
                    Material pMat = new Material(GetURPLitShader());
                    pMat.SetColor("_BaseColor", cfg.color);
                    pMat.SetFloat("_Metallic", 0.3f);
                    pMat.SetFloat("_Smoothness", 0.6f);
                    string matPath = $"{MaterialsFolder}Mat_Projectile_{cfg.id}.mat";
                    AssetDatabase.CreateAsset(pMat, matPath);
                    r.sharedMaterial = pMat;
                }

                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        public static void BuildAllHeroAdapters()
        {
            var heroSpecs = new[]
            {
                new {
                    id = "archer",
                    fbx = "Ranger.fbx",
                    controller = "Ranger_Idle.controller",
                    scale = 1.0f,
                    bodyMat = "Mat_Archer_Body",
                    weaponMat = "Mat_Archer_Weapon",
                    muzzlePos = new Vector3(0f, 0.85f, 0.25f),
                    abilityPos = new Vector3(0f, 0.85f, 0.25f),
                    impactPos = new Vector3(0f, 0.5f, 0f),
                    weaponType = "Bow"
                },
                new {
                    id = "bombardier",
                    fbx = "Warrior.fbx",
                    controller = "Warrior_Idle.controller",
                    scale = 1.05f,
                    bodyMat = "Mat_Bombardier_Body",
                    weaponMat = "Mat_Bombardier_Weapon",
                    muzzlePos = new Vector3(0.28f, 0.82f, 0.55f),
                    abilityPos = new Vector3(0f, 1.15f, 0.35f),
                    impactPos = new Vector3(0f, 0.55f, 0f),
                    weaponType = "Cannon"
                },
                new {
                    id = "frost_mage",
                    fbx = "Cleric.fbx",
                    controller = "Cleric_Idle.controller",
                    scale = 1.0f,
                    bodyMat = "Mat_FrostMage_Body",
                    weaponMat = "Mat_FrostMage_Weapon",
                    muzzlePos = new Vector3(0.32f, 1.25f, 0.32f),
                    abilityPos = new Vector3(0f, 1.35f, 0.25f),
                    impactPos = new Vector3(0f, 0.50f, 0f),
                    weaponType = "FrostStaff"
                },
                new {
                    id = "fire_mage",
                    fbx = "Wizard.fbx",
                    controller = "Wizard_Idle.controller",
                    scale = 1.0f,
                    bodyMat = "Mat_FireMage_Body",
                    weaponMat = "Mat_FireMage_Weapon",
                    muzzlePos = new Vector3(0.32f, 1.28f, 0.32f),
                    abilityPos = new Vector3(0f, 1.35f, 0.25f),
                    impactPos = new Vector3(0f, 0.50f, 0f),
                    weaponType = "FireStaff"
                },
                new {
                    id = "electric_engineer",
                    fbx = "Monk.fbx",
                    controller = "Monk_Idle.controller",
                    scale = 1.0f,
                    bodyMat = "Mat_ElectricEngineer_Body",
                    weaponMat = "Mat_ElectricEngineer_Weapon",
                    muzzlePos = new Vector3(0.28f, 0.95f, 0.42f),
                    abilityPos = new Vector3(0f, 1.22f, 0.20f),
                    impactPos = new Vector3(0f, 0.50f, 0f),
                    weaponType = "Tesla"
                },
                new {
                    id = "sniper",
                    fbx = "Rogue.fbx",
                    controller = "Rogue_Idle.controller",
                    scale = 0.95f,
                    bodyMat = "Mat_Sniper_Body",
                    weaponMat = "Mat_Sniper_Weapon",
                    muzzlePos = new Vector3(0.28f, 0.88f, 0.78f),
                    abilityPos = new Vector3(0f, 0.98f, 0.45f),
                    impactPos = new Vector3(0f, 0.48f, 0f),
                    weaponType = "SniperRifle"
                },
                new {
                    id = "plague_doctor",
                    fbx = "Wizard.fbx",
                    controller = "Wizard_Idle.controller",
                    scale = 0.98f,
                    bodyMat = "Mat_PlagueDoctor_Body",
                    weaponMat = "Mat_PlagueDoctor_Weapon",
                    muzzlePos = new Vector3(0.30f, 0.92f, 0.38f),
                    abilityPos = new Vector3(0f, 1.18f, 0.25f),
                    impactPos = new Vector3(0f, 0.50f, 0f),
                    weaponType = "PlagueFlasks"
                },
                new {
                    id = "radiant_paladin",
                    fbx = "Warrior.fbx",
                    controller = "Warrior_Idle.controller",
                    scale = 1.10f,
                    bodyMat = "Mat_RadiantPaladin_Body",
                    weaponMat = "Mat_RadiantPaladin_Weapon",
                    muzzlePos = new Vector3(0.35f, 0.98f, 0.42f),
                    abilityPos = new Vector3(0f, 1.15f, 0.30f),
                    impactPos = new Vector3(0f, 0.55f, 0f),
                    weaponType = "SwordAndShield"
                },
                new {
                    id = "shadow_assassin",
                    fbx = "Rogue.fbx",
                    controller = "Rogue_Idle.controller",
                    scale = 0.95f,
                    bodyMat = "Mat_ShadowAssassin_Body",
                    weaponMat = "Mat_ShadowAssassin_Weapon",
                    muzzlePos = new Vector3(0.32f, 0.86f, 0.38f),
                    abilityPos = new Vector3(0f, 0.95f, 0.22f),
                    impactPos = new Vector3(0f, 0.48f, 0f),
                    weaponType = "DualDaggers"
                },
                new {
                    id = "storm_druid",
                    fbx = "Monk.fbx",
                    controller = "Monk_Idle.controller",
                    scale = 1.02f,
                    bodyMat = "Mat_StormDruid_Body",
                    weaponMat = "Mat_StormDruid_Weapon",
                    muzzlePos = new Vector3(0.30f, 1.22f, 0.36f),
                    abilityPos = new Vector3(0f, 1.35f, 0.22f),
                    impactPos = new Vector3(0f, 0.50f, 0f),
                    weaponType = "StormTotem"
                }
            };

            foreach (var spec in heroSpecs)
            {
                string prefabPath = AdaptersFolder + spec.id + "_Adapter.prefab";
                GameObject root = new GameObject(spec.id + "_Adapter");
                var procAnim = root.AddComponent<ProceduralAnimator>();
                var adapter = root.AddComponent<ArtAdapter>();

                GameObject visRoot = new GameObject("VisualRoot");
                visRoot.transform.SetParent(root.transform, false);
                adapter.visualRoot = visRoot.transform;

                string fbxPath = ModelsFolder + spec.fbx;
                GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (fbxModel == null)
                {
                    Debug.LogError("FBX not found: " + fbxPath);
                    UnityEngine.Object.DestroyImmediate(root);
                    continue;
                }

                GameObject modelInstance = UnityEngine.Object.Instantiate(fbxModel, visRoot.transform);
                modelInstance.name = spec.fbx.Replace(".fbx", "_Model");
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;

                Animator animator = modelInstance.GetComponent<Animator>();
                if (animator == null) animator = modelInstance.AddComponent<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllersFolder + spec.controller);
                animator.applyRootMotion = false;
                adapter.animatorReference = animator;

                adapter.visualScale = Vector3.one * spec.scale;
                adapter.visualRotation = Vector3.zero;
                adapter.visualOffset = Vector3.zero;

                GameObject muzzle = new GameObject("Muzzle");
                muzzle.transform.SetParent(root.transform, false);
                muzzle.transform.localPosition = spec.muzzlePos;
                adapter.muzzleTransform = muzzle.transform;

                GameObject abilityOrigin = new GameObject("AbilityOrigin");
                abilityOrigin.transform.SetParent(root.transform, false);
                abilityOrigin.transform.localPosition = spec.abilityPos;
                adapter.abilityOrigin = abilityOrigin.transform;

                GameObject impact = new GameObject("ImpactPoint");
                impact.transform.SetParent(root.transform, false);
                impact.transform.localPosition = spec.impactPos;
                adapter.impactPoint = impact.transform;

                Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + spec.bodyMat + ".mat");
                Material weaponMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + spec.weaponMat + ".mat");

                // Assign materials to base model renderers
                Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
                foreach (var rend in renderers)
                {
                    bool isWeapon = rend.name.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0
                        || rend.name.IndexOf("Sword", StringComparison.OrdinalIgnoreCase) >= 0
                        || rend.name.IndexOf("Dagger", StringComparison.OrdinalIgnoreCase) >= 0
                        || rend.name.IndexOf("Staff", StringComparison.OrdinalIgnoreCase) >= 0
                        || rend.name.IndexOf("Bow", StringComparison.OrdinalIgnoreCase) >= 0;
                    rend.sharedMaterial = isWeapon && weaponMat != null ? weaponMat : bodyMat;
                }

                // Attach custom class props based on weaponType
                AttachCustomClassProps(modelInstance, spec.weaponType, bodyMat, weaponMat);

                // Wire up ProceduralAnimator serialized model property
                SerializedObject so = new SerializedObject(procAnim);
                SerializedProperty modelProp = so.FindProperty("model");
                if (modelProp != null)
                {
                    modelProp.objectReferenceValue = visRoot.transform;
                }
                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                Debug.Log($"[HeroVisualProductionBuilder] Built {spec.id}_Adapter.prefab");
            }
        }

        private static void AttachCustomClassProps(GameObject modelInstance, string weaponType, Material bodyMat, Material weaponMat)
        {
            const float boneSpaceFromMeters = 0.01f;
            Transform weaponR = FindTransformRecursive(modelInstance.transform, "Weapon.R");
            Transform fistL = FindTransformRecursive(modelInstance.transform, "Fist.L");
            Transform torso = FindTransformRecursive(modelInstance.transform, "Torso");

            if (weaponType == "Cannon")
            {
                Transform sword = FindTransformRecursive(modelInstance.transform, "Warrior_Sword");
                if (sword != null) sword.gameObject.SetActive(false);

                if (weaponR != null)
                {
                    GameObject cannon = new GameObject("Bombardier_ArtilleryCannon");
                    AttachMeterContainerToBone(cannon.transform, weaponR, new Vector3(0f, 0.05f, 0.15f), Quaternion.Euler(15f, 0f, 0f), boneSpaceFromMeters);

                    GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    barrel.name = "Barrel";
                    barrel.transform.SetParent(cannon.transform, false);
                    barrel.transform.localPosition = new Vector3(0f, 0.18f, 0f);
                    barrel.transform.localScale = new Vector3(0.16f, 0.22f, 0.16f);
                    barrel.GetComponent<Renderer>().sharedMaterial = weaponMat;
                    UnityEngine.Object.DestroyImmediate(barrel.GetComponent<Collider>());

                    GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    ring.name = "BrassMuzzleRing";
                    ring.transform.SetParent(cannon.transform, false);
                    ring.transform.localPosition = new Vector3(0f, 0.38f, 0f);
                    ring.transform.localScale = new Vector3(0.20f, 0.04f, 0.20f);
                    ring.GetComponent<Renderer>().sharedMaterial = weaponMat;
                    UnityEngine.Object.DestroyImmediate(ring.GetComponent<Collider>());
                }

                if (torso != null)
                {
                    GameObject ammoPack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ammoPack.name = "AmmoCanisterPack";
                    AttachMeterPrimitiveToBone(ammoPack.transform, torso, new Vector3(0f, 0.08f, -0.16f), Quaternion.identity, new Vector3(0.24f, 0.28f, 0.14f), boneSpaceFromMeters);
                    ammoPack.GetComponent<Renderer>().sharedMaterial = weaponMat;
                    UnityEngine.Object.DestroyImmediate(ammoPack.GetComponent<Collider>());
                }
            }
            else if (weaponType == "SniperRifle")
            {
                Transform dagger = FindTransformRecursive(modelInstance.transform, "Rogue_Dagger");
                if (dagger != null) dagger.gameObject.SetActive(false);

                if (weaponR != null)
                {
                    GameObject rifle = new GameObject("Sniper_PrecisionRifle");
                    AttachMeterContainerToBone(rifle.transform, weaponR, new Vector3(0f, 0.02f, 0.10f), Quaternion.identity, boneSpaceFromMeters);

                    GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    barrel.name = "RifleBarrel";
                    barrel.transform.SetParent(rifle.transform, false);
                    barrel.transform.localPosition = new Vector3(0f, 0.28f, 0f);
                    barrel.transform.localScale = new Vector3(0.04f, 0.35f, 0.04f);
                    barrel.GetComponent<Renderer>().sharedMaterial = weaponMat;
                    UnityEngine.Object.DestroyImmediate(barrel.GetComponent<Collider>());

                    GameObject scope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    scope.name = "OpticalScope";
                    scope.transform.SetParent(rifle.transform, false);
                    scope.transform.localPosition = new Vector3(0f, 0.22f, 0.06f);
                    scope.transform.localScale = new Vector3(0.06f, 0.14f, 0.06f);
                    scope.GetComponent<Renderer>().sharedMaterial = weaponMat;
                    UnityEngine.Object.DestroyImmediate(scope.GetComponent<Collider>());
                }
            }
            else if (weaponType == "Tesla")
            {
                if (torso != null)
                {
                    GameObject teslaBackpack = new GameObject("TeslaCapacitorRig");
                    AttachMeterContainerToBone(teslaBackpack.transform, torso, new Vector3(0f, 0.12f, -0.15f), Quaternion.identity, boneSpaceFromMeters);

                    for (int side = -1; side <= 1; side += 2)
                    {
                        GameObject coil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        coil.name = side == -1 ? "TeslaCoil.L" : "TeslaCoil.R";
                        coil.transform.SetParent(teslaBackpack.transform, false);
                        coil.transform.localPosition = new Vector3(side * 0.14f, 0.10f, 0f);
                        coil.transform.localScale = new Vector3(0.08f, 0.20f, 0.08f);
                        coil.GetComponent<Renderer>().sharedMaterial = weaponMat;
                        UnityEngine.Object.DestroyImmediate(coil.GetComponent<Collider>());

                        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        head.name = "ElectrodeHead";
                        head.transform.SetParent(coil.transform, false);
                        head.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                        head.transform.localScale = new Vector3(1.6f, 0.8f, 1.6f);
                        head.GetComponent<Renderer>().sharedMaterial = weaponMat;
                        UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
                    }
                }
            }
            else if (weaponType == "SwordAndShield")
            {
                if (fistL != null)
                {
                    GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shield.name = "Paladin_HolyKiteShield";
                    AttachMeterPrimitiveToBone(shield.transform, fistL, new Vector3(-0.05f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.06f, 0.48f, 0.32f), boneSpaceFromMeters);
                    shield.GetComponent<Renderer>().sharedMaterial = weaponMat;
                    UnityEngine.Object.DestroyImmediate(shield.GetComponent<Collider>());
                }
            }
            else if (weaponType == "DualDaggers")
            {
                if (fistL != null)
                {
                    Transform rDagger = FindTransformRecursive(modelInstance.transform, "Rogue_Dagger");
                    if (rDagger != null)
                    {
                        GameObject lDagger = UnityEngine.Object.Instantiate(rDagger.gameObject, fistL);
                        lDagger.name = "Shadow_Dagger.L";
                        lDagger.transform.localPosition = new Vector3(0f, 0.02f, 0.05f);
                        lDagger.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                        lDagger.transform.localScale = Vector3.one;
                        var rend = lDagger.GetComponent<Renderer>();
                        if (rend != null) rend.sharedMaterial = weaponMat;
                    }
                }
            }
            else if (weaponType == "FrostStaff")
            {
                Transform staff = FindTransformRecursive(modelInstance.transform, "Cleric_Staff");
                if (staff != null)
                {
                    GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    crystal.name = "GlacialIceCrystal";
                    AttachMeterPrimitiveToBone(crystal.transform, staff, new Vector3(0f, 0.95f, 0f), Quaternion.identity, new Vector3(0.22f, 0.28f, 0.22f), boneSpaceFromMeters);
                    crystal.GetComponent<Renderer>().sharedMaterial = weaponMat;
                    UnityEngine.Object.DestroyImmediate(crystal.GetComponent<Collider>());
                }
            }
            else if (weaponType == "FireStaff")
            {
                Transform staff = FindTransformRecursive(modelInstance.transform, "Wizard_Staff");
                if (staff != null)
                {
                    GameObject brazier = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    brazier.name = "EmberFurnaceCore";
                    AttachMeterPrimitiveToBone(brazier.transform, staff, new Vector3(0f, 0.92f, 0f), Quaternion.identity, new Vector3(0.24f, 0.24f, 0.24f), boneSpaceFromMeters);
                    brazier.GetComponent<Renderer>().sharedMaterial = weaponMat;
                    UnityEngine.Object.DestroyImmediate(brazier.GetComponent<Collider>());
                }
            }
            else if (weaponType == "PlagueFlasks")
            {
                if (torso != null)
                {
                    GameObject flasks = new GameObject("ToxicFlasksBandolier");
                    AttachMeterContainerToBone(flasks.transform, torso, new Vector3(0.08f, -0.05f, 0.12f), Quaternion.identity, boneSpaceFromMeters);

                    for (int i = 0; i < 3; i++)
                    {
                        GameObject flask = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        flask.name = $"PoisonFlask_{i + 1}";
                        flask.transform.SetParent(flasks.transform, false);
                        flask.transform.localPosition = new Vector3(i * 0.10f - 0.10f, 0f, 0f);
                        flask.transform.localScale = new Vector3(0.09f, 0.12f, 0.09f);
                        flask.GetComponent<Renderer>().sharedMaterial = weaponMat;
                        UnityEngine.Object.DestroyImmediate(flask.GetComponent<Collider>());
                    }
                }
            }
            else if (weaponType == "StormTotem")
            {
                if (weaponR != null)
                {
                    GameObject totem = new GameObject("StormTotemStaff");
                    AttachMeterContainerToBone(totem.transform, weaponR, new Vector3(0f, 0.10f, 0.05f), Quaternion.identity, boneSpaceFromMeters);

                    GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    shaft.name = "TotemShaft";
                    shaft.transform.SetParent(totem.transform, false);
                    shaft.transform.localScale = new Vector3(0.06f, 0.65f, 0.06f);
                    shaft.GetComponent<Renderer>().sharedMaterial = bodyMat;
                    UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());

                    GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    head.name = "TempestVortexGem";
                    head.transform.SetParent(totem.transform, false);
                    head.transform.localPosition = new Vector3(0f, 0.70f, 0f);
                    head.transform.localScale = new Vector3(0.20f, 0.26f, 0.20f);
                    head.GetComponent<Renderer>().sharedMaterial = weaponMat;
                    UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
                }
            }
        }

        private static void AttachMeterContainerToBone(Transform propRoot, Transform bone, Vector3 localPosMeters, Quaternion localRot, float boneSpaceFromMeters)
        {
            propRoot.SetParent(bone, false);
            propRoot.localPosition = localPosMeters * boneSpaceFromMeters;
            propRoot.localRotation = localRot;
            propRoot.localScale = Vector3.one * boneSpaceFromMeters;
        }

        private static void AttachMeterPrimitiveToBone(Transform primitive, Transform bone, Vector3 localPosMeters, Quaternion localRot, Vector3 localScaleMeters, float boneSpaceFromMeters)
        {
            primitive.SetParent(bone, false);
            primitive.localPosition = localPosMeters * boneSpaceFromMeters;
            primitive.localRotation = localRot;
            primitive.localScale = localScaleMeters * boneSpaceFromMeters;
        }

        public static void UpdateWeaponAndHeroDefinitions()
        {
            var heroMappings = new[]
            {
                new { id = "archer", heroAsset = "ArcherHero.asset", weaponAsset = "ArcherWeapon.asset", proj = "Projectile" },
                new { id = "bombardier", heroAsset = "BombardierHero.asset", weaponAsset = "BombardierWeapon.asset", proj = "Projectile_bombardier" },
                new { id = "frost_mage", heroAsset = "FrostMageHero.asset", weaponAsset = "FrostMageWeapon.asset", proj = "Projectile_frost_mage" },
                new { id = "fire_mage", heroAsset = "FireMageHero.asset", weaponAsset = "FireMageWeapon.asset", proj = "Projectile_fire_mage" },
                new { id = "electric_engineer", heroAsset = "ElectricEngineerHero.asset", weaponAsset = "ElectricEngineerWeapon.asset", proj = "Projectile_electric_engineer" },
                new { id = "sniper", heroAsset = "SniperHero.asset", weaponAsset = "SniperWeapon.asset", proj = "Projectile_sniper" },
                new { id = "plague_doctor", heroAsset = "PlagueDoctorHero.asset", weaponAsset = "PlagueDoctorWeapon.asset", proj = "Projectile_plague_doctor" },
                new { id = "radiant_paladin", heroAsset = "RadiantPaladinHero.asset", weaponAsset = "RadiantPaladinWeapon.asset", proj = "Projectile_radiant_paladin" },
                new { id = "shadow_assassin", heroAsset = "ShadowAssassinHero.asset", weaponAsset = "ShadowAssassinWeapon.asset", proj = "Projectile_shadow_assassin" },
                new { id = "storm_druid", heroAsset = "StormDruidHero.asset", weaponAsset = "StormDruidWeapon.asset", proj = "Projectile_storm_druid" }
            };

            string[] heroFolders = { HeroSOFolder, HeroResourcesFolder };
            string[] weaponFolders = { WeaponSOFolder, WeaponResourcesFolder };

            foreach (var m in heroMappings)
            {
                string adapterPath = $"{AdaptersFolder}{m.id}_Adapter.prefab";
                GameObject adapterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(adapterPath);
                string iconPath = $"Assets/_Game/Art/Icons/Heroes/Icon_{m.id}.png";
                Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

                // Update HeroDefinitions
                foreach (var hf in heroFolders)
                {
                    string hPath = hf + m.heroAsset;
                    var heroDef = AssetDatabase.LoadAssetAtPath<HeroDefinition>(hPath);
                    if (heroDef != null)
                    {
                        heroDef.heroPrefab = adapterPrefab;
                        if (iconSprite != null) heroDef.icon = iconSprite;
                        EditorUtility.SetDirty(heroDef);
                    }
                }

                // Update WeaponDefinitions with dedicated projectile prefabs
                string projPath = m.proj == "Projectile" ? "Assets/_Game/Prefabs/Projectile.prefab" : $"{ProjectilesFolder}{m.proj}.prefab";
                GameObject projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);

                foreach (var wf in weaponFolders)
                {
                    string wPath = wf + m.weaponAsset;
                    var weaponDef = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(wPath);
                    if (weaponDef != null)
                    {
                        if (projPrefab != null) weaponDef.projectilePrefab = projPrefab;
                        EditorUtility.SetDirty(weaponDef);
                    }
                }
            }
        }

        private static Transform FindTransformRecursive(Transform parent, string targetName)
        {
            if (parent.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindTransformRecursive(parent.GetChild(i), targetName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
