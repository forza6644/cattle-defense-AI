using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// One-shot startup validation for critical scene wiring. It reports missing
    /// references clearly without owning gameplay state or checking every frame.
    /// </summary>
    public class SceneReferenceValidator : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            ValidateSceneReferences();
        }

        private void ValidateSceneReferences()
        {
            int errors = 0;

            if (GameManager.Instance == null && FindFirstObjectByType<GameManager>() == null)
            {
                LogMissing("GameManager", ref errors);
            }

            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager == null)
            {
                LogMissing("WaveManager", ref errors);
            }

            if (UIManager.Instance == null && FindFirstObjectByType<UIManager>() == null)
            {
                LogMissing("UIManager", ref errors);
            }

            if (FindFirstObjectByType<Castle>() == null)
            {
                LogMissing("Castle", ref errors);
            }

            if (GameObject.Find("SpawnPoint") == null)
            {
                LogMissing("SpawnPoint GameObject", ref errors);
            }

            GameObject pathObject = GameObject.Find("Path");
            if (pathObject == null)
            {
                LogMissing("WaypointPath GameObject named Path", ref errors);
            }
            else if (pathObject.GetComponent<WaypointPath>() == null)
            {
                LogMissing("WaypointPath component on Path", ref errors);
            }

            HeroSlot[] heroSlots = FindObjectsByType<HeroSlot>(FindObjectsSortMode.None);
            if (CountUniqueSlotNames(heroSlots) == 0)
            {
                LogMissing("HeroSlots", ref errors);
            }

            TowerManager towerManager = FindFirstObjectByType<TowerManager>();
            if (towerManager == null || towerManager.Config == null)
            {
                LogMissing("GameConfig reference", ref errors);
            }

            if (CardDraftManager.Instance == null && FindFirstObjectByType<CardDraftManager>() == null)
            {
                LogMissing("CardDraftManager", ref errors);
            }

            if (HeroRosterManager.Instance == null && FindFirstObjectByType<HeroRosterManager>() == null)
            {
                LogMissing("HeroRosterManager", ref errors);
            }

            if (errors == 0)
            {
                Debug.Log("[SceneReferenceValidator] Critical gameplay references validated.");
            }
        }

        private static int CountUniqueSlotNames(HeroSlot[] heroSlots)
        {
            if (heroSlots == null || heroSlots.Length == 0)
            {
                return 0;
            }

            HashSet<string> names = new HashSet<string>();
            for (int i = 0; i < heroSlots.Length; i++)
            {
                if (heroSlots[i] != null)
                {
                    names.Add(heroSlots[i].name);
                }
            }

            return names.Count;
        }

        private static void LogMissing(string label, ref int errors)
        {
            errors++;
            Debug.LogError($"[SceneReferenceValidator] Missing critical reference: {label}.");
        }
    }
}
