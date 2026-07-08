using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public class DamageTracker : MonoBehaviour
    {
        public static DamageTracker Instance { get; private set; }

        private readonly Dictionary<string, float> damageByHeroId = new Dictionary<string, float>();

        public IReadOnlyDictionary<string, float> DamageByHeroId => damageByHeroId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            damageByHeroId.Clear();
        }

        public static void RecordDamage(string heroId, float damageAmount)
        {
            if (string.IsNullOrEmpty(heroId) || damageAmount <= 0f)
            {
                return;
            }

            var inst = Instance != null ? Instance : FindFirstObjectByType<DamageTracker>();
            if (inst == null)
            {
                return;
            }

            if (!inst.damageByHeroId.ContainsKey(heroId))
            {
                inst.damageByHeroId[heroId] = 0f;
            }

            inst.damageByHeroId[heroId] += damageAmount;
        }

        public float GetTotalDamage()
        {
            float total = 0f;
            foreach (var val in damageByHeroId.Values)
            {
                total += val;
            }
            return total;
        }

        public float GetDamagePercentage(string heroId)
        {
            float total = GetTotalDamage();
            if (total <= 0f) return 0f;
            if (damageByHeroId.TryGetValue(heroId, out float dmg))
            {
                return (dmg / total) * 100f;
            }
            return 0f;
        }

        public void ClearTracker()
        {
            damageByHeroId.Clear();
            Debug.Log("[DamageTracker] Cleared damage records.");
        }
    }
}
