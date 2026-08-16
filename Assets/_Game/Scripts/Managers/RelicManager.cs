using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Central manager for run-defining Relics and Artifacts.
    /// Handles relic inventory, passive effects, elite drop rolls, and hook queries.
    /// </summary>
    public class RelicManager : MonoBehaviour
    {
        public static RelicManager Instance { get; private set; }

        public static event Action<RelicDefinition> OnRelicAcquired;
        public static event Action OnRelicsUpdated;

        private readonly List<RelicDefinition> activeRelics = new List<RelicDefinition>();
        public IReadOnlyList<RelicDefinition> ActiveRelics => activeRelics;

        private List<RelicDefinition> availableRelicPool = new List<RelicDefinition>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadRelicPool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadRelicPool()
        {
            availableRelicPool.Clear();
            RelicDefinition[] loaded = Resources.LoadAll<RelicDefinition>("Relics");
            if (loaded != null && loaded.Length > 0)
            {
                availableRelicPool.AddRange(loaded);
            }
        }

        public void RegisterRelicToPool(RelicDefinition relic)
        {
            if (relic != null && !availableRelicPool.Contains(relic))
            {
                availableRelicPool.Add(relic);
            }
        }

        public void RegisterAllAvailableRelics(IEnumerable<RelicDefinition> relics)
        {
            availableRelicPool.Clear();
            if (relics != null)
            {
                availableRelicPool.AddRange(relics);
            }
        }

        public void ResetForRun()
        {
            activeRelics.Clear();
            OnRelicsUpdated?.Invoke();
        }

        public void AddRelic(RelicDefinition relic)
        {
            if (relic == null) return;
            activeRelics.Add(relic);
            OnRelicAcquired?.Invoke(relic);
            OnRelicsUpdated?.Invoke();

            // Special on-acquire hooks (e.g. Castle energy shield)
            if (relic.effectType == RelicEffectType.CastleShieldRecharge && Castle.Instance != null)
            {
                Castle.Instance.AddKineticShield(relic.effectValue);
            }
        }

        public bool HasRelic(string relicId)
        {
            if (string.IsNullOrEmpty(relicId)) return false;
            for (int i = 0; i < activeRelics.Count; i++)
            {
                if (activeRelics[i] != null && activeRelics[i].id == relicId)
                {
                    return true;
                }
            }
            return false;
        }

        public float GetEffectValue(RelicEffectType effectType)
        {
            float total = 0f;
            for (int i = 0; i < activeRelics.Count; i++)
            {
                if (activeRelics[i] != null && activeRelics[i].effectType == effectType)
                {
                    total += activeRelics[i].effectValue;
                }
            }
            return total;
        }

        public float GetCooldownMultiplier()
        {
            float cdr = GetEffectValue(RelicEffectType.CooldownReductionGlobal);
            return Mathf.Clamp(1.0f - cdr, 0.25f, 1.0f);
        }

        public float GetElementalReactionMultiplier()
        {
            return 1.0f + GetEffectValue(RelicEffectType.ElementalReactionBoost);
        }

        public float GetCastleVampirismPercent()
        {
            return GetEffectValue(RelicEffectType.CritCastleVampirism);
        }

        public float GetEliteBossGoldMultiplier()
        {
            return 1.0f + GetEffectValue(RelicEffectType.EliteBossGoldBonus);
        }

        public int GetShockChainBonus()
        {
            return Mathf.RoundToInt(GetEffectValue(RelicEffectType.ShockChainBonus));
        }

        public float GetSlowDamageMultiplier()
        {
            return 1.0f + GetEffectValue(RelicEffectType.SlowDamageAmp);
        }

        public float GetMinionDurationMultiplier()
        {
            return 1.0f + GetEffectValue(RelicEffectType.SpectralMinionMastery);
        }

        public float GetExecuteThreshold()
        {
            return GetEffectValue(RelicEffectType.ExecuteThreshold);
        }

        public List<RelicDefinition> RollRelicDraft(int count = 3)
        {
            List<RelicDefinition> candidates = new List<RelicDefinition>();
            for (int i = 0; i < availableRelicPool.Count; i++)
            {
                RelicDefinition r = availableRelicPool[i];
                if (r != null && !HasRelic(r.id))
                {
                    candidates.Add(r);
                }
            }

            List<RelicDefinition> choices = new List<RelicDefinition>();
            int picks = Mathf.Min(count, candidates.Count);
            for (int i = 0; i < picks; i++)
            {
                int idx = UnityEngine.Random.Range(0, candidates.Count);
                choices.Add(candidates[idx]);
                candidates.RemoveAt(idx);
            }
            return choices;
        }
    }
}
