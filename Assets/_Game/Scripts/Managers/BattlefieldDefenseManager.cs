using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public sealed class BattlefieldDefenseManager : MonoBehaviour
    {
        public static BattlefieldDefenseManager Instance { get; private set; }
        private readonly Dictionary<string, Stack<BattlefieldDefenseRuntime>> inactive = new Dictionary<string, Stack<BattlefieldDefenseRuntime>>();
        public BattlefieldDefenseRuntime ActiveDefense { get; private set; }
        public int CreatedCount { get; private set; }
        public int ReuseCount { get; private set; }
        public int RejectedCount { get; private set; }
        public int StaleTargetCount => 0;

        public bool CanDeploy(BattlefieldDefenseDefinition definition) => definition != null
            && ActiveDefense == null
            && BattlefieldAnchorManager.Instance != null
            && BattlefieldAnchorManager.Instance.HasAvailableAnchor(BattlefieldAnchorType.Defense);

        private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool TryDeploy(BattlefieldDefenseDefinition definition, out BattlefieldDefenseRuntime runtime)
        {
            runtime = null;
            if (!CanDeploy(definition)) { RejectedCount++; return false; }
            runtime = Acquire(definition);
            if (!BattlefieldAnchorManager.Instance.TryClaim(BattlefieldAnchorType.Defense, runtime, out BattlefieldAnchor anchor))
            {
                StoreInactive(definition.stableId, runtime); runtime = null; RejectedCount++; return false;
            }
            ActiveDefense = runtime; runtime.Activate(this, definition, anchor); return true;
        }

        private BattlefieldDefenseRuntime Acquire(BattlefieldDefenseDefinition definition)
        {
            if (inactive.TryGetValue(definition.stableId, out Stack<BattlefieldDefenseRuntime> pool))
            {
                while (pool.Count > 0)
                {
                    BattlefieldDefenseRuntime pooled = pool.Pop();
                    if (pooled == null) continue;
                    ReuseCount++; return pooled;
                }
            }
            GameObject instance = definition.prefab != null ? Instantiate(definition.prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = definition.stableId + " Runtime";
            BattlefieldDefenseRuntime runtime = instance.GetComponent<BattlefieldDefenseRuntime>();
            if (runtime == null) runtime = instance.AddComponent<BattlefieldDefenseRuntime>();
            CreatedCount++; return runtime;
        }

        internal void Return(BattlefieldDefenseRuntime runtime)
        {
            if (runtime == null) return;
            if (ActiveDefense == runtime) ActiveDefense = null;
            string key = runtime.name.Replace(" Runtime", string.Empty); StoreInactive(key, runtime);
        }

        private void StoreInactive(string key, BattlefieldDefenseRuntime runtime)
        {
            runtime.gameObject.SetActive(false);
            if (!inactive.TryGetValue(key, out Stack<BattlefieldDefenseRuntime> pool)) { pool = new Stack<BattlefieldDefenseRuntime>(); inactive[key] = pool; }
            if (!pool.Contains(runtime)) pool.Push(runtime);
        }

        public void ResetForRun() { if (ActiveDefense != null) ActiveDefense.Break(); ActiveDefense = null; RejectedCount = 0; }
    }
}
