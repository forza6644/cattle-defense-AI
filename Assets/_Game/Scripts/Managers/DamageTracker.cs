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
            if (string.IsNullOrEmpty(heroId) || damageAmount <= 0f || Instance == null)
            {
                return;
            }

            if (!Instance.damageByHeroId.ContainsKey(heroId))
            {
                Instance.damageByHeroId[heroId] = 0f;
            }

            Instance.damageByHeroId[heroId] += damageAmount;
        }
    }
}
