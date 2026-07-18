using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public sealed class TrapRuntimeManager : MonoBehaviour
    {
        public static TrapRuntimeManager Instance { get; private set; }
        private readonly List<TrapRuntimeZone> active = new List<TrapRuntimeZone>();
        private readonly Dictionary<string, Stack<TrapRuntimeZone>> inactive = new Dictionary<string, Stack<TrapRuntimeZone>>();
        public IReadOnlyList<TrapRuntimeZone> ActiveTraps => active;
        public int CreatedCount { get; private set; }
        public int ReuseCount { get; private set; }
        public int RejectedCount { get; private set; }
        public int StaleTickCount => 0;

        public bool CanDeploy(TrapDefinition definition)
        {
            if (definition == null || BattlefieldAnchorManager.Instance == null
                || !BattlefieldAnchorManager.Instance.HasAvailableAnchor(BattlefieldAnchorType.Trap)) return false;
            int sameType = 0;
            for (int i = 0; i < active.Count; i++) if (active[i] != null && active[i].Definition == definition) sameType++;
            return sameType < Mathf.Max(1, definition.maxActive);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool TryDeploy(TrapDefinition definition, out TrapRuntimeZone zone)
        {
            zone = null;
            if (!CanDeploy(definition)) { RejectedCount++; return false; }
            zone = Acquire(definition);
            if (!BattlefieldAnchorManager.Instance.TryClaim(BattlefieldAnchorType.Trap, zone, out BattlefieldAnchor anchor))
            {
                StoreInactive(definition.stableId, zone); zone = null; RejectedCount++; return false;
            }
            active.Add(zone); zone.Activate(this, definition, anchor); return true;
        }

        private TrapRuntimeZone Acquire(TrapDefinition definition)
        {
            if (inactive.TryGetValue(definition.stableId, out Stack<TrapRuntimeZone> pool))
            {
                while (pool.Count > 0)
                {
                    TrapRuntimeZone pooled = pool.Pop();
                    if (pooled == null) continue;
                    ReuseCount++; return pooled;
                }
            }
            GameObject instance = definition.prefab != null ? Instantiate(definition.prefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            instance.name = definition.stableId + " Runtime";
            TrapRuntimeZone zone = instance.GetComponent<TrapRuntimeZone>();
            if (zone == null) zone = instance.AddComponent<TrapRuntimeZone>();
            CreatedCount++; return zone;
        }

        internal void Return(TrapRuntimeZone zone)
        {
            if (zone == null) return;
            active.Remove(zone);
            string key = zone.name.Replace(" Runtime", string.Empty);
            StoreInactive(key, zone);
        }

        private void StoreInactive(string key, TrapRuntimeZone zone)
        {
            if (zone == null) return;
            zone.gameObject.SetActive(false);
            if (!inactive.TryGetValue(key, out Stack<TrapRuntimeZone> pool)) { pool = new Stack<TrapRuntimeZone>(); inactive[key] = pool; }
            if (!pool.Contains(zone)) pool.Push(zone);
        }

        public void ResetForRun()
        {
            for (int i = active.Count - 1; i >= 0; i--) if (active[i] != null) active[i].Deactivate();
            active.Clear(); RejectedCount = 0;
        }
    }
}
