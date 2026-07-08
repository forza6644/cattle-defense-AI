using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Stonehold.EditorTools
{
    public static class MapIntegrator
    {
        [MenuItem("Tools/Stonehold/Log Lab Positions")]
        public static void LogLabPositions()
        {
            string labScenePath = "Assets/TD catle defence project.unity";
            Scene labScene = EditorSceneManager.OpenScene(labScenePath, OpenSceneMode.Single);

            Debug.Log("=== LAB SCENE ROOT OBJECTS ===");
            GameObject[] roots = labScene.GetRootGameObjects();
            foreach (var root in roots)
            {
                Debug.Log($"Root: {root.name}");
                PrintChildrenRecursive(root.transform, 1);
            }
        }

        private static void PrintChildrenRecursive(Transform t, int depth)
        {
            string indent = new string(' ', depth * 2);
            Debug.Log($"{indent}- {t.name} position={t.position} localPosition={t.localPosition}");
            for (int i = 0; i < t.childCount; i++)
            {
                PrintChildrenRecursive(t.GetChild(i), depth + 1);
            }
        }

        [MenuItem("Tools/Stonehold/Integrate Visual Map from Lab")]
        public static void IntegrateMap()
        {
            // Fallback for visual map from lab
        }

        [MenuItem("Tools/Stonehold/Integrate Castle Map")]
        public static void IntegrateCastleMap()
        {
            // 1. Open GameScene
            string gameScenePath = "Assets/_Game/Scenes/GameScene.unity";
            Scene gameScene = EditorSceneManager.OpenScene(gameScenePath, OpenSceneMode.Single);

            // 2. Disable old visual-only Environment elements
            GameObject environment = GameObject.Find("Environment");
            if (environment != null)
            {
                for (int i = 0; i < environment.transform.childCount; i++)
                {
                    environment.transform.GetChild(i).gameObject.SetActive(false);
                }
                EditorUtility.SetDirty(environment);
            }

            // Enable directional light and set properties
            GameObject dirLight = GameObject.Find("Directional Light");
            if (dirLight != null)
            {
                dirLight.SetActive(true);
                Light lightComp = dirLight.GetComponent<Light>();
                if (lightComp != null)
                {
                    lightComp.intensity = 1.0f;
                    lightComp.color = new Color(1f, 0.95f, 0.85f);
                }
                EditorUtility.SetDirty(dirLight);
            }

            GameObject globVol = GameObject.Find("Global Volume");
            if (globVol != null)
            {
                globVol.SetActive(true);
                EditorUtility.SetDirty(globVol);
            }

            // Remove any bad VisualMap_Integrated left over
            GameObject oldMap = GameObject.Find("VisualMap_Integrated");
            if (oldMap != null)
            {
                Object.DestroyImmediate(oldMap);
            }

            // 3. Create or clean the new visual root
            GameObject mapRoot = GameObject.Find("VisualMap_CastlePrototype");
            if (mapRoot != null)
            {
                Object.DestroyImmediate(mapRoot);
            }
            mapRoot = new GameObject("VisualMap_CastlePrototype");

            // Load project-owned materials
            Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Ground_Grass.mat");
            Material roadMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Road_Dirt.mat");
            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Stone_Castle.mat");
            Material gateMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Wood_Gate.mat");
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Rock.mat");

            // 4. Build Ground (Grass) - longer field
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground_Grass";
            ground.transform.SetParent(mapRoot.transform);
            ground.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            ground.transform.localScale = new Vector3(30f, 0.1f, 40f); // Longer grass field
            if (grassMat != null) ground.GetComponent<Renderer>().material = grassMat;

            // 5. Define Waypoint Path positions (Curved S-shape)
            Vector3 wp0Pos = new Vector3(0f, 0.1f, 9.5f);     // Spawn
            Vector3 wp1Pos = new Vector3(-1.2f, 0.1f, 5.5f);  // Curve Left
            Vector3 wp2Pos = new Vector3(1.2f, 0.1f, 1.5f);   // Curve Right
            Vector3 wp3Pos = new Vector3(-0.6f, 0.1f, -2.5f); // Curve Left
            Vector3 wp4Pos = new Vector3(0f, 0.1f, -6.0f);    // Castle Gate

            // 6. Build Curved Road Segments (Wider path: 4.0 units)
            float roadWidth = 4.0f;
            GameObject roadRoot = new GameObject("Road_Segments");
            roadRoot.transform.SetParent(mapRoot.transform);
            BuildRoadSegment(roadRoot, wp0Pos, wp1Pos, roadWidth, roadMat);
            BuildRoadSegment(roadRoot, wp1Pos, wp2Pos, roadWidth, roadMat);
            BuildRoadSegment(roadRoot, wp2Pos, wp3Pos, roadWidth, roadMat);
            BuildRoadSegment(roadRoot, wp3Pos, wp4Pos, roadWidth, roadMat);

            // 7. Build Castle Wall (Stone wall)
            GameObject castleWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            castleWall.name = "CastleWall_Stone";
            castleWall.transform.SetParent(mapRoot.transform);
            castleWall.transform.localPosition = new Vector3(0f, 1.5f, -6.75f);
            castleWall.transform.localScale = new Vector3(20f, 3.0f, 1.5f);
            if (stoneMat != null) castleWall.GetComponent<Renderer>().material = stoneMat;

            // 8. Build Castle Gate (Wooden gate)
            GameObject castleGate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            castleGate.name = "CastleGate_Wood";
            castleGate.transform.SetParent(mapRoot.transform);
            castleGate.transform.localPosition = new Vector3(0f, 1.0f, -6.05f);
            castleGate.transform.localScale = new Vector3(4.0f, 2.0f, 0.1f); // Fits wider road
            if (gateMat != null) castleGate.GetComponent<Renderer>().material = gateMat;

            // 9. Build decorative rocks on the outer curves
            Vector3[] rockPositions = {
                new Vector3(3.5f, 0.5f, 5.5f),  // Right side of left curve
                new Vector3(-3.5f, 0.5f, 1.5f), // Left side of right curve
                new Vector3(3.5f, 0.5f, -2.5f)  // Right side of left curve
            };
            for (int i = 0; i < rockPositions.Length; i++)
            {
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Rock_Decoration_" + i;
                rock.transform.SetParent(mapRoot.transform);
                rock.transform.localPosition = rockPositions[i];
                rock.transform.localScale = new Vector3(1.5f, 1.2f, 1.5f);
                rock.transform.localRotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
                if (rockMat != null) rock.GetComponent<Renderer>().material = rockMat;
            }

            // 10. Align Gameplay Anchors
            GameObject spawnPointObj = GameObject.Find("Level/SpawnPoint");
            if (spawnPointObj != null)
            {
                spawnPointObj.transform.localPosition = wp0Pos;
                EditorUtility.SetDirty(spawnPointObj);
            }

            GameObject castleObj = GameObject.Find("Level/Castle");
            if (castleObj != null)
            {
                castleObj.transform.localPosition = wp4Pos;
                EditorUtility.SetDirty(castleObj);
            }

            // Waypoints: curved line matching the road segments
            GameObject wp0 = GameObject.Find("Level/Path/Waypoint_00_TopSpawn");
            if (wp0 != null) wp0.transform.localPosition = wp0Pos;

            GameObject wp1 = GameObject.Find("Level/Path/Waypoint_01_UpperLane");
            if (wp1 != null) wp1.transform.localPosition = wp1Pos;

            GameObject wp2 = GameObject.Find("Level/Path/Waypoint_02_MidLane");
            if (wp2 != null) wp2.transform.localPosition = wp2Pos;

            GameObject wp3 = GameObject.Find("Level/Path/Waypoint_03_LowerLane");
            if (wp3 != null) wp3.transform.localPosition = wp3Pos;

            GameObject wp4 = GameObject.Find("Level/Path/Waypoint_04_WallImpact");
            if (wp4 != null) wp4.transform.localPosition = wp4Pos;

            GameObject pathObj = GameObject.Find("Level/Path");
            if (pathObj != null) EditorUtility.SetDirty(pathObj);

            // TowerSlots: place on top of the castle wall (Y = 3.0, Z = -6.75)
            float slotZ = -6.75f;
            float slotY = 3.0f;
            GameObject ts1 = GameObject.Find("Level/TowerSlots/TowerSlot_1");
            if (ts1 != null) ts1.transform.localPosition = new Vector3(-3.6f, slotY, slotZ);

            GameObject ts2 = GameObject.Find("Level/TowerSlots/TowerSlot_2");
            if (ts2 != null) ts2.transform.localPosition = new Vector3(-1.8f, slotY, slotZ);

            GameObject ts3 = GameObject.Find("Level/TowerSlots/TowerSlot_3");
            if (ts3 != null) ts3.transform.localPosition = new Vector3(0f, slotY, slotZ);

            GameObject ts4 = GameObject.Find("Level/TowerSlots/TowerSlot_4");
            if (ts4 != null) ts4.transform.localPosition = new Vector3(1.8f, slotY, slotZ);

            GameObject ts5 = GameObject.Find("Level/TowerSlots/TowerSlot_5");
            if (ts5 != null) ts5.transform.localPosition = new Vector3(3.6f, slotY, slotZ);

            GameObject slotsObj = GameObject.Find("Level/TowerSlots");
            if (slotsObj != null) EditorUtility.SetDirty(slotsObj);

            // Camera Framing - angled slightly more down and further back/up to show full path
            GameObject cameraObj = GameObject.Find("Main Camera");
            if (cameraObj != null)
            {
                cameraObj.transform.localPosition = new Vector3(0f, 15f, -11.5f);
                cameraObj.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
                EditorUtility.SetDirty(cameraObj);
            }

            // Save GameScene
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);

            // 11. Run setup tool to place the 6 centered hero slots on top of the wall
            HeroCastleDefenseSetup.Setup();

            Debug.Log("Castle straight lane map integrated successfully!");
        }

        private static void BuildRoadSegment(GameObject parent, Vector3 start, Vector3 end, float width, Material material)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = "Road_Segment";
            segment.transform.SetParent(parent.transform);

            Vector3 direction = end - start;
            float distance = direction.magnitude;
            segment.transform.position = start + direction * 0.5f + new Vector3(0f, 0.005f, 0f);
            segment.transform.localScale = new Vector3(width, 0.01f, distance + 0.4f); // slight overlap
            segment.transform.rotation = Quaternion.LookRotation(direction);
            if (material != null) segment.GetComponent<Renderer>().material = material;
        }
    }
}
