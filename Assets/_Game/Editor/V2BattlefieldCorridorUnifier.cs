#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Stonehold.EditorTools
{
    /// <summary>
    /// Unifies the V2 map into a single wide battlefield corridor.
    /// Replaces split lanes and internal dividing obstacles with a single,
    /// broad, textured stone battleground spanning from the castle wall to the 3 spawn portals.
    /// </summary>
    public static class V2BattlefieldCorridorUnifier
    {
        private const string ScenePath = "Assets/_Game/Scenes/V2/GameplayIntegration_V2.unity";

        [MenuItem("Tools/Stonehold/Unify V2 Battlefield Corridor")]
        public static void UnifyV2Battlefield()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[V2BattlefieldCorridorUnifier] Failed to open scene at {ScenePath}");
                return;
            }

            // 1. Remove old lane guide ribbon (curved lane segments)
            var laneGuide = GameObject.Find("Composition_LaneGuide");
            if (laneGuide != null)
            {
                Undo.DestroyObjectImmediate(laneGuide);
                Debug.Log("[V2BattlefieldCorridorUnifier] Removed Composition_LaneGuide.");
            }

            // 2. Remove old prototype spawn zone if present
            var oldSpawnZone = GameObject.Find("Composition_EnemySpawnZone");
            if (oldSpawnZone != null)
            {
                Undo.DestroyObjectImmediate(oldSpawnZone);
                Debug.Log("[V2BattlefieldCorridorUnifier] Removed old Composition_EnemySpawnZone.");
            }

            // Load materials
            Material matGroundGrass = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Ground_GrassDark.mat");
            Material matAshStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/ApprovedStyle/Materials/Approved_Path_AshStone.mat");
            Material matDarkSlate = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/ApprovedStyle/Materials/Approved_Ground_DarkSlate.mat");
            Material matCastleStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/ApprovedStyle/Materials/Approved_Castle_Stone.mat");
            Material matCastleTrim = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/ApprovedStyle/Materials/Approved_Castle_Trim.mat");

            if (matAshStone == null) matAshStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Road_Dirt.mat");
            if (matDarkSlate == null) matDarkSlate = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Stone_Dark.mat");
            if (matCastleStone == null) matCastleStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Stone_Castle.mat");

            // 3. Configure Composition_Battlefield ground
            var battlefieldComp = GameObject.Find("Composition_Battlefield");
            if (battlefieldComp != null)
            {
                var ground = battlefieldComp.transform.Find("Ground");
                if (ground != null)
                {
                    ground.localPosition = new Vector3(0f, -0.25f, 19.5f);
                    ground.localScale = new Vector3(28f, 0.5f, 42f);
                    if (matDarkSlate != null) ground.GetComponent<Renderer>().sharedMaterial = matDarkSlate;
                }

                var leftFlank = battlefieldComp.transform.Find("LeftFlank");
                if (leftFlank != null)
                {
                    leftFlank.localPosition = new Vector3(-17.5f, 1.25f, 19.5f);
                    leftFlank.localScale = new Vector3(7f, 3.0f, 42f);
                }

                var rightFlank = battlefieldComp.transform.Find("RightFlank");
                if (rightFlank != null)
                {
                    rightFlank.localPosition = new Vector3(17.5f, 1.25f, 19.5f);
                    rightFlank.localScale = new Vector3(7f, 3.0f, 42f);
                }
            }

            // 4. Update V2_BattlefieldPresentation_Pass01
            var presPass = GameObject.Find("V2_BattlefieldPresentation_Pass01");
            if (presPass != null)
            {
                // Clean up old split road visuals
                var oldRoadVisuals = presPass.transform.Find("Road_Visuals");
                if (oldRoadVisuals != null)
                {
                    Undo.DestroyObjectImmediate(oldRoadVisuals.gameObject);
                }

                // Create unified wide battlefield road
                GameObject wideRoadContainer = new GameObject("Road_Visuals");
                wideRoadContainer.transform.SetParent(presPass.transform, false);

                // Main Broad Stone Battleground (Wide Highway Corridor across all 3 portals)
                GameObject grandCorridor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                grandCorridor.name = "Unified_Grand_Battlefield_Corridor";
                grandCorridor.transform.SetParent(wideRoadContainer.transform, false);
                grandCorridor.transform.localPosition = new Vector3(0f, 0.04f, 19.5f);
                grandCorridor.transform.localScale = new Vector3(23.5f, 0.06f, 39.0f);
                if (matAshStone != null) grandCorridor.GetComponent<Renderer>().sharedMaterial = matAshStone;
                UnityEngine.Object.DestroyImmediate(grandCorridor.GetComponent<Collider>());

                // Central March Highway Inset (Subtle central stone paving highlight)
                GameObject centralMarch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                centralMarch.name = "Central_March_Paving";
                centralMarch.transform.SetParent(wideRoadContainer.transform, false);
                centralMarch.transform.localPosition = new Vector3(0f, 0.05f, 19.5f);
                centralMarch.transform.localScale = new Vector3(8.5f, 0.06f, 38.6f);
                if (matDarkSlate != null) centralMarch.GetComponent<Renderer>().sharedMaterial = matDarkSlate;
                UnityEngine.Object.DestroyImmediate(centralMarch.GetComponent<Collider>());

                // Outer Stone Curbs (Left & Right framing borders)
                GameObject leftCurb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftCurb.name = "Battlefield_Curb_Left";
                leftCurb.transform.SetParent(wideRoadContainer.transform, false);
                leftCurb.transform.localPosition = new Vector3(-11.8f, 0.08f, 19.5f);
                leftCurb.transform.localScale = new Vector3(0.5f, 0.12f, 39.0f);
                if (matCastleTrim != null) leftCurb.GetComponent<Renderer>().sharedMaterial = matCastleTrim;
                UnityEngine.Object.DestroyImmediate(leftCurb.GetComponent<Collider>());

                GameObject rightCurb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightCurb.name = "Battlefield_Curb_Right";
                rightCurb.transform.SetParent(wideRoadContainer.transform, false);
                rightCurb.transform.localPosition = new Vector3(11.8f, 0.08f, 19.5f);
                rightCurb.transform.localScale = new Vector3(0.5f, 0.12f, 39.0f);
                if (matCastleTrim != null) rightCurb.GetComponent<Renderer>().sharedMaterial = matCastleTrim;
                UnityEngine.Object.DestroyImmediate(rightCurb.GetComponent<Collider>());

                // Castle Approach Frontline Apron
                GameObject frontlineApron = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frontlineApron.name = "Castle_Frontline_Apron";
                frontlineApron.transform.SetParent(wideRoadContainer.transform, false);
                frontlineApron.transform.localPosition = new Vector3(0f, 0.06f, 2.5f);
                frontlineApron.transform.localScale = new Vector3(25.0f, 0.08f, 4.0f);
                if (matCastleStone != null) frontlineApron.GetComponent<Renderer>().sharedMaterial = matCastleStone;
                UnityEngine.Object.DestroyImmediate(frontlineApron.GetComponent<Collider>());

                // Spawn Portal Assembly Plaza
                GameObject spawnPlaza = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spawnPlaza.name = "Spawn_Assembly_Plaza";
                spawnPlaza.transform.SetParent(wideRoadContainer.transform, false);
                spawnPlaza.transform.localPosition = new Vector3(0f, 0.06f, 38.0f);
                spawnPlaza.transform.localScale = new Vector3(25.0f, 0.08f, 4.0f);
                if (matCastleStone != null) spawnPlaza.GetComponent<Renderer>().sharedMaterial = matCastleStone;
                UnityEngine.Object.DestroyImmediate(spawnPlaza.GetComponent<Collider>());

                // 5. Reposition internal rocks to flanking edges to keep arena 100% open
                var nature = presPass.transform.Find("Integrated_URP_Nature");
                if (nature != null)
                {
                    // Move West rocks out to x = -14.5 to -16.0
                    var rw1 = nature.Find("Rock_West_01"); if (rw1 != null) rw1.localPosition = new Vector3(-14.2f, 0.1f, 6.0f);
                    var rw2 = nature.Find("Rock_West_02"); if (rw2 != null) rw2.localPosition = new Vector3(-15.0f, 0.2f, 15.0f);
                    var rw3 = nature.Find("Rock_West_03"); if (rw3 != null) rw3.localPosition = new Vector3(-14.5f, 0.1f, 24.0f);
                    var rw4 = nature.Find("Rock_West_04"); if (rw4 != null) rw4.localPosition = new Vector3(-14.0f, 0.2f, 34.0f);

                    // Move East rocks out to x = 14.5 to 16.0
                    var re1 = nature.Find("Rock_East_01"); if (re1 != null) re1.localPosition = new Vector3(14.2f, 0.1f, 6.0f);
                    var re2 = nature.Find("Rock_East_02"); if (re2 != null) re2.localPosition = new Vector3(15.0f, 0.2f, 15.0f);
                    var re3 = nature.Find("Rock_East_03"); if (re3 != null) re3.localPosition = new Vector3(14.5f, 0.1f, 24.0f);
                    var re4 = nature.Find("Rock_East_04"); if (re4 != null) re4.localPosition = new Vector3(14.0f, 0.2f, 34.0f);

                    // Move trees to outer boundary
                    var tw1 = nature.Find("Tree_West_01"); if (tw1 != null) tw1.localPosition = new Vector3(-15.5f, 0.1f, 8.0f);
                    var tw2 = nature.Find("Tree_West_02"); if (tw2 != null) tw2.localPosition = new Vector3(-16.2f, 0.2f, 19.0f);
                    var tw3 = nature.Find("Tree_West_03"); if (tw3 != null) tw3.localPosition = new Vector3(-15.8f, 0.1f, 29.0f);

                    var te1 = nature.Find("Tree_East_01"); if (te1 != null) te1.localPosition = new Vector3(15.5f, 0.1f, 8.0f);
                    var te2 = nature.Find("Tree_East_02"); if (te2 != null) te2.localPosition = new Vector3(16.2f, 0.2f, 19.0f);
                    var te3 = nature.Find("Tree_East_03"); if (te3 != null) te3.localPosition = new Vector3(15.8f, 0.1f, 29.0f);
                }
            }

            // 6. Verify and calibrate WaypointPath and Portals
            var pathObj = GameObject.Find("Path");
            if (pathObj != null)
            {
                var wp = pathObj.GetComponent<WaypointPath>();
                if (wp == null) wp = pathObj.AddComponent<WaypointPath>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[V2BattlefieldCorridorUnifier] Unified V2 battlefield corridor completed successfully!");
        }
    }
}
#endif
