#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Stonehold
{
    public static class ProjectSetupTools
    {
        private const string ModelsFolder = "Assets/_Game/Art/Characters/QuaterniusRPG/Models/";
        private const string ControllersFolder = "Assets/_Game/Art/Characters/QuaterniusRPG/Controllers/";
        private const string MaterialsFolder = "Assets/_Game/Art/Characters/QuaterniusRPG/Materials/";
        private const string EnemyMaterialsFolder = MaterialsFolder + "Enemies";
        private const string PrefabsFolder = "Assets/_Game/Prefabs/Enemies/";
        private const string HeroSOFolder = "Assets/_Game/ScriptableObjects/Heroes/";

        [MenuItem("Stonehold/Setup Animator Controllers and Prefabs")]
        public static void RunSetup()
        {
            Debug.Log("[ProjectSetupTools] Starting project setup...");

            // 1. Setup Hero Animator Controllers
            SetupHeroControllers();

            // 2. Create Enemy Animator Controllers
            SetupEnemyControllers();

            // 3. Upgrade Enemy Prefabs
            UpgradeEnemyPrefabs();

            // 4. Tune Hero ScriptableObject Ranges
            TuneHeroRanges();

            // 5. Assign Hero Definitions to Main Menu
            AssignHeroDefinitionsToMainMenu();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProjectSetupTools] Project setup completed successfully!");
        }

        private static void SetupHeroControllers()
        {
            SetupHeroController("Ranger_Idle.controller", "Ranger.fbx", "CharacterArmature|Idle", "CharacterArmature|Bow_Attack_Shoot", "CharacterArmature|Bow_Attack_Draw");
            SetupHeroController("Warrior_Idle.controller", "Warrior.fbx", "CharacterArmature|Idle", "CharacterArmature|Sword_Attack", "CharacterArmature|Sword_AttackFast");
            SetupHeroController("Cleric_Idle.controller", "Cleric.fbx", "CharacterArmature|Idle", "CharacterArmature|Spell1", "CharacterArmature|Spell2");
            SetupHeroController("Wizard_Idle.controller", "Wizard.fbx", "CharacterArmature|Idle", "CharacterArmature|Spell1", "CharacterArmature|Spell2");
            SetupHeroController("Monk_Idle.controller", "Monk.fbx", "CharacterArmature|Idle", "CharacterArmature|Attack", "CharacterArmature|Attack2");
            SetupHeroController("Rogue_Idle.controller", "Rogue.fbx", "CharacterArmature|Idle", "CharacterArmature|Dagger_Attack", "CharacterArmature|Dagger_Attack2");
        }

        private static void SetupHeroController(string controllerName, string fbxName, string idleClip, string attackClip, string abilityClip)
        {
            string controllerPath = ControllersFolder + controllerName;
            string fbxPath = ModelsFolder + fbxName;

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogWarning($"[ProjectSetupTools] Controller not found: {controllerPath}");
                return;
            }

            AnimationClip idle = FindClip(fbxPath, idleClip);
            AnimationClip attack = FindClip(fbxPath, attackClip);
            AnimationClip ability = FindClip(fbxPath, abilityClip);
            AnimationClip death = FindClip(fbxPath, "CharacterArmature|Death");

            if (idle == null || attack == null)
            {
                Debug.LogWarning($"[ProjectSetupTools] Core clips not found in {fbxPath}");
                return;
            }

            var rootStateMachine = controller.layers[0].stateMachine;

            // Clear old states
            for (int i = rootStateMachine.states.Length - 1; i >= 0; i--)
            {
                rootStateMachine.RemoveState(rootStateMachine.states[i].state);
            }

            // Create states
            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idle;

            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attack;

            var abilityState = rootStateMachine.AddState("Ability");
            abilityState.motion = ability != null ? ability : attack;

            var deathState = rootStateMachine.AddState("Death");
            deathState.motion = death;

            rootStateMachine.defaultState = idleState;
            EditorUtility.SetDirty(controller);
            Debug.Log($"[ProjectSetupTools] Configured hero controller: {controllerName}");
        }

        private static void SetupEnemyControllers()
        {
            // We create standalone enemy controllers
            CreateEnemyController("Enemy_Grunt.controller", "Monk.fbx", "CharacterArmature|Walk", "CharacterArmature|Attack");
            CreateEnemyController("Enemy_Runner.controller", "Rogue.fbx", "CharacterArmature|Run", "CharacterArmature|Dagger_Attack");
            CreateEnemyController("Enemy_Brute.controller", "Warrior.fbx", "CharacterArmature|Walk", "CharacterArmature|Sword_Attack");
            CreateEnemyController("Enemy_Shield.controller", "Warrior.fbx", "CharacterArmature|Walk", "CharacterArmature|Sword_Attack");
            CreateEnemyController("Enemy_Boss.controller", "Wizard.fbx", "CharacterArmature|Walk", "CharacterArmature|Spell1");
        }

        private static void CreateEnemyController(string controllerName, string fbxName, string walkClip, string attackClip)
        {
            string path = ControllersFolder + controllerName;
            string fbxPath = ModelsFolder + fbxName;

            AnimationClip walk = FindClip(fbxPath, walkClip);
            AnimationClip attack = FindClip(fbxPath, attackClip);
            AnimationClip idle = FindClip(fbxPath, "CharacterArmature|Idle");
            AnimationClip death = FindClip(fbxPath, "CharacterArmature|Death");

            if (walk == null || attack == null)
            {
                Debug.LogWarning($"[ProjectSetupTools] Core clips not found in {fbxPath} for enemy controller {controllerName}");
                return;
            }

            // Create or load controller
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            var rootStateMachine = controller.layers[0].stateMachine;

            // Clear old states
            for (int i = rootStateMachine.states.Length - 1; i >= 0; i--)
            {
                rootStateMachine.RemoveState(rootStateMachine.states[i].state);
            }

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idle;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walk;

            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attack;

            var deathState = rootStateMachine.AddState("Death");
            deathState.motion = death;

            rootStateMachine.defaultState = walkState;
            EditorUtility.SetDirty(controller);
            Debug.Log($"[ProjectSetupTools] Created/Configured enemy controller: {controllerName}");
        }

        private static void UpgradeEnemyPrefabs()
        {
            UpgradeEnemyPrefab("Grunt.prefab", "Monk.fbx", "Enemy_Grunt.controller", 0.52f, new Color(0.35f, 0.72f, 0.3f));
            UpgradeEnemyPrefab("RunnerEnemy.prefab", "Rogue.fbx", "Enemy_Runner.controller", 0.42f, new Color(0.9f, 0.3f, 0.25f));
            UpgradeEnemyPrefab("Brute.prefab", "Warrior.fbx", "Enemy_Brute.controller", 0.8f, new Color(0.3f, 0.42f, 0.75f));
            UpgradeEnemyPrefab("ArmoredEnemy.prefab", "Warrior.fbx", "Enemy_Shield.controller", 0.6f, new Color(0.58f, 0.63f, 0.7f), addShield: true);
            UpgradeEnemyPrefab("BossEnemy.prefab", "Wizard.fbx", "Enemy_Boss.controller", 0.65f, new Color(0.52f, 0.18f, 0.68f));
        }

        private static void UpgradeEnemyPrefab(string prefabName, string fbxName, string controllerName, float scaleMultiplier, Color tintColor, bool addShield = false)
        {
            string prefabPath = PrefabsFolder + prefabName;
            string fbxPath = ModelsFolder + fbxName;
            string controllerPath = ControllersFolder + controllerName;

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogWarning($"[ProjectSetupTools] Failed to load prefab: {prefabPath}");
                return;
            }

            // Find or create ModelGroup
            Transform modelGroup = root.transform.Find("ModelGroup");
            if (modelGroup == null)
            {
                GameObject mgGo = new GameObject("ModelGroup");
                mgGo.transform.SetParent(root.transform, false);
                modelGroup = mgGo.transform;
            }

            // Destroy old primitive child objects under root (except ModelGroup and other script GameObjects)
            List<GameObject> toDestroy = new List<GameObject>();
            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform child = root.transform.GetChild(i);
                if (child != modelGroup && child.name != "Canvas" && child.name != "HealthBar" && child.name != "UI")
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in toDestroy)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            // Destroy old children inside ModelGroup
            toDestroy.Clear();
            for (int i = 0; i < modelGroup.childCount; i++)
            {
                toDestroy.Add(modelGroup.GetChild(i).gameObject);
            }
            foreach (var go in toDestroy)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            // Load model FBX
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (modelPrefab == null)
            {
                Debug.LogWarning($"[ProjectSetupTools] Model not found: {fbxPath}");
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            GameObject modelInstance = UnityEngine.Object.Instantiate(modelPrefab, modelGroup);
            modelInstance.name = fbxName.Replace(".fbx", "_Model");
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one * scaleMultiplier;

            string modelName = fbxName.Replace(".fbx", string.Empty);
            Material bodySource = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + modelName + "_Body.mat");
            Material weaponSource = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + modelName + "_Weapon.mat");
            Material bodyMaterial = GetOrCreateEnemyMaterial(prefabName, "Body", bodySource, tintColor);
            Material weaponMaterial = GetOrCreateEnemyMaterial(prefabName, "Weapon", weaponSource, tintColor);

            // Assign persistent URP materials. Transient `new Material` instances are
            // not serialized into prefab assets and previously produced magenta models.
            Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                bool isWeapon = rend.name.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0
                    || rend.name.IndexOf("Sword", StringComparison.OrdinalIgnoreCase) >= 0
                    || rend.name.IndexOf("Dagger", StringComparison.OrdinalIgnoreCase) >= 0
                    || rend.name.IndexOf("Staff", StringComparison.OrdinalIgnoreCase) >= 0;
                Material assigned = isWeapon && weaponMaterial != null ? weaponMaterial : bodyMaterial;
                if (assigned == null)
                {
                    assigned = weaponMaterial;
                }
                if (assigned != null)
                {
                    rend.sharedMaterial = assigned;
                }
            }

            // Add shield if requested
            if (addShield)
            {
                Transform leftHand = FindTransformRecursive(modelInstance.transform, "LeftHand");
                if (leftHand == null) leftHand = FindTransformRecursive(modelInstance.transform, "Hand_L");
                if (leftHand == null) leftHand = FindTransformRecursive(modelInstance.transform, "hand_l");
                if (leftHand == null) leftHand = modelInstance.transform; // fallback to root

                GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shield.name = "ShieldProp";
                shield.transform.SetParent(leftHand, false);
                shield.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                shield.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                shield.transform.localScale = new Vector3(0.08f, 0.7f, 0.5f);

                // Remove collider so it doesn't affect physics
                var col = shield.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);

                var shieldRend = shield.GetComponent<Renderer>();
                if (shieldRend != null)
                {
                    Material shieldSource = bodySource != null ? bodySource : weaponSource;
                    shieldRend.sharedMaterial = GetOrCreateEnemyMaterial(
                        prefabName,
                        "Shield",
                        shieldSource,
                        new Color(0.45f, 0.48f, 0.52f));
                }
            }

            // Setup Animator on the model instance
            Animator anim = modelInstance.GetComponent<Animator>();
            if (anim == null)
            {
                anim = modelInstance.AddComponent<Animator>();
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;

            // Make sure the main ProceduralAnimator script on root points to ModelGroup
            ProceduralAnimator procAnim = root.GetComponent<ProceduralAnimator>();
            if (procAnim != null)
            {
                // We will modify the field using serialized properties
                SerializedObject so = new SerializedObject(procAnim);
                SerializedProperty modelProp = so.FindProperty("model");
                if (modelProp != null)
                {
                    modelProp.objectReferenceValue = modelGroup;
                }
                so.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[ProjectSetupTools] Upgraded enemy prefab: {prefabName}");
        }

        private static Material GetOrCreateEnemyMaterial(
            string prefabName,
            string role,
            Material source,
            Color tintColor)
        {
            if (source == null)
            {
                return null;
            }

            if (!AssetDatabase.IsValidFolder(EnemyMaterialsFolder))
            {
                AssetDatabase.CreateFolder(MaterialsFolder.TrimEnd('/'), "Enemies");
            }

            string enemyName = prefabName.Replace(".prefab", string.Empty);
            string path = EnemyMaterialsFolder + "/" + enemyName + "_" + role + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                EditorUtility.CopySerialized(source, material);
            }

            material.name = enemyName + "_" + role;
            int baseColorId = Shader.PropertyToID("_BaseColor");
            if (material.HasProperty(baseColorId))
            {
                Color original = material.GetColor(baseColorId);
                material.SetColor(baseColorId, Color.Lerp(original, tintColor, 0.5f));
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void TuneHeroRanges()
        {
            TuneRange("ArcherHero.asset", 14f);
            TuneRange("BombardierHero.asset", 12f);
            TuneRange("FrostMageHero.asset", 11f);
            TuneRange("FireMageHero.asset", 14f);
            TuneRange("ElectricEngineerHero.asset", 12.5f);
            TuneRange("SniperHero.asset", 20f);
        }

        private static void TuneRange(string assetName, float range)
        {
            string path = HeroSOFolder + assetName;
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(path);
            if (hero != null)
            {
                hero.baseRange = range;
                EditorUtility.SetDirty(hero);
                Debug.Log($"[ProjectSetupTools] Tuned range for {assetName} to {range}");
            }
            else
            {
                Debug.LogWarning($"[ProjectSetupTools] Hero definition asset not found: {path}");
            }
        }

        private static AnimationClip FindClip(string fbxPath, string name)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && clip.name == name)
                {
                    return clip;
                }
            }
            return null;
        }

        private static Transform FindTransformRecursive(Transform parent, string name)
        {
            if (parent.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return parent;
            }
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindTransformRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void AssignHeroDefinitionsToMainMenu()
        {
            string mainMenuPath = "Assets/_Game/Scenes/MainMenu.unity";
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(mainMenuPath);
            MainMenuUI mainMenuUI = UnityEngine.Object.FindAnyObjectByType<MainMenuUI>();
            if (mainMenuUI != null)
            {
                string[] guids = AssetDatabase.FindAssets("t:HeroDefinition", new string[] { "Assets/_Game/ScriptableObjects/Heroes" });
                List<HeroDefinition> heroes = new List<HeroDefinition>();
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    HeroDefinition hd = AssetDatabase.LoadAssetAtPath<HeroDefinition>(path);
                    if (hd != null)
                    {
                        heroes.Add(hd);
                    }
                }
                heroes.Sort((a, b) => {
                    List<string> order = new List<string> { "archer", "bombardier", "frost_mage", "fire_mage", "electric_engineer", "sniper" };
                    int indexA = order.IndexOf(a.id);
                    int indexB = order.IndexOf(b.id);
                    return indexA.CompareTo(indexB);
                });

                mainMenuUI.SetHeroDefinitions(heroes.ToArray());
                EditorUtility.SetDirty(mainMenuUI);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Debug.Log($"[ProjectSetupTools] Successfully assigned {heroes.Count} HeroDefinitions to MainMenuUI in scene and saved.");
            }
            else
            {
                Debug.LogError("[ProjectSetupTools] MainMenuUI not found in MainMenu.unity scene!");
            }
        }
    }
}
#endif
