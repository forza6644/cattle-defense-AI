using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class CreateArtDirectionLab : Editor
{
    [MenuItem("Stonehold/Create Art Direction Lab")]
    public static void CreateLab()
    {
        // 1. Create directory paths
        string scenesDir = "Assets/_Game/Scenes/ArtDirection";
        string materialsDir = "Assets/_Game/Art/StyleLab/Materials";
        
        if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);
        if (!Directory.Exists(materialsDir)) Directory.CreateDirectory(materialsDir);
        
        AssetDatabase.Refresh();

        // 2. Create Materials programmatically
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            urpLit = Shader.Find("Standard");
        }

        // Materials for Bay B (Bright Heroic)
        Material groundB = new Material(urpLit) { color = new Color(0.35f, 0.6f, 0.25f) };
        Material wallB = new Material(urpLit) { color = new Color(0.85f, 0.8f, 0.7f) };
        Material heroB = new Material(urpLit) { color = new Color(0.2f, 0.5f, 0.85f) };
        Material enemyB = new Material(urpLit) { color = new Color(0.3f, 0.5f, 0.2f) };

        // Materials for Bay C (Arcane Night)
        Material groundC = new Material(urpLit) { color = new Color(0.08f, 0.08f, 0.12f) };
        Material wallC = new Material(urpLit) { color = new Color(0.15f, 0.12f, 0.2f) };
        Material heroC = new Material(urpLit) { color = new Color(0.2f, 0.9f, 0.9f) };
        Material enemyC = new Material(urpLit) { color = new Color(0.8f, 0.1f, 0.8f) };

        // VFX / Damage color samples (Common)
        Material fireMat = new Material(urpLit) { color = new Color(1.0f, 0.35f, 0.0f) };
        Material frostMat = new Material(urpLit) { color = new Color(0.3f, 0.7f, 1.0f) };
        Material electricMat = new Material(urpLit) { color = new Color(0.7f, 0.3f, 1.0f) };
        Material poisonMat = new Material(urpLit) { color = new Color(0.2f, 0.8f, 0.2f) };

        AssetDatabase.CreateAsset(groundB, $"{materialsDir}/Ground_BrightHeroic.mat");
        AssetDatabase.CreateAsset(wallB, $"{materialsDir}/Wall_BrightHeroic.mat");
        AssetDatabase.CreateAsset(heroB, $"{materialsDir}/Hero_BrightHeroic.mat");
        AssetDatabase.CreateAsset(enemyB, $"{materialsDir}/Enemy_BrightHeroic.mat");

        AssetDatabase.CreateAsset(groundC, $"{materialsDir}/Ground_ArcaneNight.mat");
        AssetDatabase.CreateAsset(wallC, $"{materialsDir}/Wall_ArcaneNight.mat");
        AssetDatabase.CreateAsset(heroC, $"{materialsDir}/Hero_ArcaneNight.mat");
        AssetDatabase.CreateAsset(enemyC, $"{materialsDir}/Enemy_ArcaneNight.mat");

        AssetDatabase.CreateAsset(fireMat, $"{materialsDir}/VFX_Fire.mat");
        AssetDatabase.CreateAsset(frostMat, $"{materialsDir}/VFX_Frost.mat");
        AssetDatabase.CreateAsset(electricMat, $"{materialsDir}/VFX_Electric.mat");
        AssetDatabase.CreateAsset(poisonMat, $"{materialsDir}/VFX_Poison.mat");

        AssetDatabase.SaveAssets();

        // 3. Create the New Scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        newScene.name = "Stonehold_ArtDirectionLab";

        // Find default elements to cleanup or reposition
        GameObject mainCameraGo = GameObject.Find("Main Camera");
        if (mainCameraGo != null)
        {
            mainCameraGo.transform.position = new Vector3(0, 12, -14);
            mainCameraGo.transform.rotation = Quaternion.Euler(45, 0, 0);
            Camera camera = mainCameraGo.GetComponent<Camera>();
            if (camera != null)
            {
                camera.orthographic = false;
                camera.fieldOfView = 50f;
            }
        }

        GameObject defaultLight = GameObject.Find("Directional Light");
        if (defaultLight != null)
        {
            // Reposition or disable to allow custom lighting per bay
            defaultLight.SetActive(false);
        }

        // --- BAY A: Dark Stone Fantasy (ROOT GameObject ONLY, for Unity AI target) ---
        GameObject bayA = new GameObject("Direction_A_DarkStoneFantasy");
        bayA.transform.position = Vector3.zero;

        // --- BAY B: Bright Heroic Fantasy (offset by +25 X) ---
        GameObject bayB = new GameObject("Direction_B_BrightHeroicFantasy");
        bayB.transform.position = new Vector3(25, 0, 0);
        SetupPresentationBay(bayB, groundB, wallB, heroB, enemyB, fireMat, frostMat, electricMat, poisonMat, false);

        // --- BAY C: Arcane Night Siege (offset by +50 X) ---
        GameObject bayC = new GameObject("Direction_C_ArcaneNightSiege");
        bayC.transform.position = new Vector3(50, 0, 0);
        SetupPresentationBay(bayC, groundC, wallC, heroC, enemyC, fireMat, frostMat, electricMat, poisonMat, true);

        // 4. Save the Scene
        string scenePath = $"{scenesDir}/Stonehold_ArtDirectionLab.unity";
        bool saveSuccess = EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log($"[CreateArtDirectionLab] Scene created and saved to {scenePath}: {saveSuccess}");
    }

    private static void SetupPresentationBay(GameObject bayRoot, Material groundMat, Material wallMat, Material heroMat, Material enemyMat, 
        Material fire, Material frost, Material elec, Material poison, bool isNight)
    {
        // Environment
        GameObject env = new GameObject("Environment");
        env.transform.SetParent(bayRoot.transform, false);

        GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.name = "Ground";
        groundPlane.transform.SetParent(env.transform, false);
        groundPlane.transform.localScale = new Vector3(2, 1, 2);
        groundPlane.GetComponent<Renderer>().sharedMaterial = groundMat;

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "CastleWallProxy";
        wall.transform.SetParent(env.transform, false);
        wall.transform.localPosition = new Vector3(0, 2, 4);
        wall.transform.localScale = new Vector3(8, 4, 1.5f);
        wall.GetComponent<Renderer>().sharedMaterial = wallMat;

        // Hero
        GameObject heroPres = new GameObject("HeroPresentation");
        heroPres.transform.SetParent(bayRoot.transform, false);

        GameObject hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = "HeroProxy_Archer";
        hero.transform.SetParent(heroPres.transform, false);
        hero.transform.localPosition = new Vector3(-2, 1, 0);
        hero.GetComponent<Renderer>().sharedMaterial = heroMat;

        // Enemy
        GameObject enemyPres = new GameObject("EnemyPresentation");
        enemyPres.transform.SetParent(bayRoot.transform, false);

        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        enemy.name = "EnemyProxy_Grunt";
        enemy.transform.SetParent(enemyPres.transform, false);
        enemy.transform.localPosition = new Vector3(2, 1, 0);
        enemy.GetComponent<Renderer>().sharedMaterial = enemyMat;

        // Lighting
        GameObject lighting = new GameObject("Lighting");
        lighting.transform.SetParent(bayRoot.transform, false);

        GameObject keyLightGo = new GameObject("KeyLight");
        keyLightGo.transform.SetParent(lighting.transform, false);
        keyLightGo.transform.localPosition = new Vector3(5, 10, -5);
        keyLightGo.transform.localRotation = Quaternion.Euler(50, -30, 0);
        Light keyLight = keyLightGo.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = isNight ? 0.3f : 1.2f;
        keyLight.color = isNight ? new Color(0.5f, 0.6f, 1.0f) : new Color(1.0f, 0.95f, 0.85f);
        keyLight.shadows = LightShadows.Hard;

        GameObject fillLightGo = new GameObject("FillLight");
        fillLightGo.transform.SetParent(lighting.transform, false);
        fillLightGo.transform.localPosition = new Vector3(-5, 5, -5);
        Light fillLight = fillLightGo.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = isNight ? 0.1f : 0.4f;
        fillLight.color = new Color(0.7f, 0.8f, 1.0f);

        GameObject rimLightGo = new GameObject("RimLight");
        rimLightGo.transform.SetParent(lighting.transform, false);
        rimLightGo.transform.localPosition = new Vector3(0, 5, 8);
        rimLightGo.transform.localRotation = Quaternion.Euler(30, 180, 0);
        Light rimLight = rimLightGo.AddComponent<Light>();
        rimLight.type = LightType.Directional;
        rimLight.intensity = 0.8f;
        rimLight.color = isNight ? new Color(0.8f, 0.4f, 1.0f) : new Color(0.9f, 0.95f, 1.0f);

        // VFX Palette Samples
        GameObject vfxPalette = new GameObject("VfxPaletteSamples");
        vfxPalette.transform.SetParent(bayRoot.transform, false);

        string[] names = { "FireSample", "FrostSample", "ElectricSample", "PoisonSample" };
        Material[] mats = { fire, frost, elec, poison };
        for (int i = 0; i < 4; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = names[i];
            sphere.transform.SetParent(vfxPalette.transform, false);
            sphere.transform.localPosition = new Vector3(-3 + (i * 2), 0.5f, -3);
            sphere.GetComponent<Renderer>().sharedMaterial = mats[i];
        }
    }
}
