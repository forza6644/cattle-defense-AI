using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    [System.Serializable]
    public struct HeroCombatReport
    {
        public string heroId;
        public string displayName;
        public float totalDamage;
        public float damagePercentage;
        public float dps;
        public int critCount;
        public Color themeColor;
    }

    /// <summary>
    /// Captures deep real-time combat telemetry: hero DPS, damage contribution %,
    /// critical hit rate, elemental reaction breakdowns, and castle absorption metrics.
    /// </summary>
    public class CombatTelemetryManager : MonoBehaviour
    {
        public static CombatTelemetryManager Instance { get; private set; }

        private readonly Dictionary<string, float> damageByHero = new Dictionary<string, float>();
        private readonly Dictionary<string, int> critsByHero = new Dictionary<string, int>();
        private readonly Dictionary<ElementalReactionType, int> reactionCounts = new Dictionary<ElementalReactionType, int>();
        private readonly Dictionary<ElementalReactionType, float> reactionDamage = new Dictionary<ElementalReactionType, float>();

        public IReadOnlyDictionary<string, float> DamageByHero => damageByHero;
        public IReadOnlyDictionary<string, int> CritsByHero => critsByHero;
        public IReadOnlyDictionary<ElementalReactionType, int> ReactionCounts => reactionCounts;
        public IReadOnlyDictionary<ElementalReactionType, float> ReactionDamage => reactionDamage;

        public float TotalCastleDamageTaken { get; private set; }
        public float TotalShieldAbsorbed { get; private set; }
        public int TotalEnemiesKilled { get; private set; }
        public float ElapsedCombatTime { get; private set; }

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
            ResetTelemetry();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
            {
                ElapsedCombatTime += Time.deltaTime;
            }
        }

        public void ResetTelemetry()
        {
            damageByHero.Clear();
            critsByHero.Clear();
            reactionCounts.Clear();
            reactionDamage.Clear();
            TotalCastleDamageTaken = 0f;
            TotalShieldAbsorbed = 0f;
            TotalEnemiesKilled = 0;
            ElapsedCombatTime = 0f;
        }

        public static CombatTelemetryManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var inst = FindFirstObjectByType<CombatTelemetryManager>();
            if (inst != null)
            {
                Instance = inst;
                return Instance;
            }

            GameObject host = DamageTracker.Instance != null ? DamageTracker.Instance.gameObject : (GameManager.Instance != null ? GameManager.Instance.gameObject : null);
            if (host != null)
            {
                inst = host.AddComponent<CombatTelemetryManager>();
            }
            else
            {
                GameObject go = new GameObject("CombatTelemetryManager");
                inst = go.AddComponent<CombatTelemetryManager>();
            }
            Instance = inst;
            return Instance;
        }

        public static void RecordDamage(string sourceId, float damage, bool isCrit = false)
        {
            if (string.IsNullOrEmpty(sourceId) || damage <= 0f) return;

            var inst = EnsureInstance();
            if (inst == null) return;

            if (!inst.damageByHero.ContainsKey(sourceId))
            {
                inst.damageByHero[sourceId] = 0f;
            }
            inst.damageByHero[sourceId] += damage;

            if (isCrit)
            {
                if (!inst.critsByHero.ContainsKey(sourceId))
                {
                    inst.critsByHero[sourceId] = 0;
                }
                inst.critsByHero[sourceId]++;
                AchievementManager.Instance?.AddProgress("achv_crit_100", 1);
                HapticFeedbackManager.TriggerLight();
            }

            AchievementManager.Instance?.SetProgress("achv_million_damage", inst.GetTotalDamage());
        }

        private Coroutine hitstopCoroutine;

        public static void TriggerMicroHitstop(float duration = 0.035f)
        {
            var inst = EnsureInstance();
            if (inst == null || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (inst.hitstopCoroutine != null)
            {
                inst.StopCoroutine(inst.hitstopCoroutine);
            }
            inst.hitstopCoroutine = inst.StartCoroutine(inst.HitstopRoutine(duration));
        }

        private System.Collections.IEnumerator HitstopRoutine(float duration)
        {
            float originalEffectiveSpeed = GameManager.Instance != null ? GameManager.Instance.EffectiveGameSpeed : 1f;
            Time.timeScale = Mathf.Min(0.08f, originalEffectiveSpeed * 0.1f);
            yield return new WaitForSecondsRealtime(duration);
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
            {
                Time.timeScale = GameManager.Instance.EffectiveGameSpeed;
            }
            hitstopCoroutine = null;
        }

        public static void RecordReaction(ElementalReactionType reactionType, float burstDamage)
        {
            var inst = EnsureInstance();
            if (inst == null) return;

            if (!inst.reactionCounts.ContainsKey(reactionType))
            {
                inst.reactionCounts[reactionType] = 0;
                inst.reactionDamage[reactionType] = 0f;
            }
            inst.reactionCounts[reactionType]++;
            inst.reactionDamage[reactionType] += burstDamage;

            HapticFeedbackManager.TriggerMedium();
            TriggerMicroHitstop(0.035f);

            AchievementManager.Instance?.AddProgress("achv_synergy_50", 1);
            switch (reactionType)
            {
                case ElementalReactionType.ThermalShock:
                    AchievementManager.Instance?.AddProgress("achv_thermal_shock", 1);
                    break;
                case ElementalReactionType.Overload:
                    AchievementManager.Instance?.AddProgress("achv_overload", 1);
                    break;
                case ElementalReactionType.CorrosiveBlast:
                    AchievementManager.Instance?.AddProgress("achv_corrosive", 1);
                    break;
                case ElementalReactionType.Shatter:
                    AchievementManager.Instance?.AddProgress("achv_shatter", 1);
                    break;
            }
        }

        public static void RecordCastleDamage(float damage, float absorbedByShield)
        {
            var inst = EnsureInstance();
            if (inst == null) return;

            inst.TotalCastleDamageTaken += damage;
            inst.TotalShieldAbsorbed += absorbedByShield;
            HapticFeedbackManager.TriggerHeavy();
        }

        public static void RecordKill()
        {
            var inst = EnsureInstance();
            if (inst == null) return;

            inst.TotalEnemiesKilled++;
        }

        public float GetTotalDamage()
        {
            float sum = 0f;
            foreach (var val in damageByHero.Values)
            {
                sum += val;
            }
            return sum;
        }

        public float GetHeroDamage(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return 0f;
            return damageByHero.TryGetValue(heroId, out float dmg) ? dmg : 0f;
        }

        public float GetHeroPercentage(string heroId)
        {
            float total = GetTotalDamage();
            if (total <= 0f) return 0f;
            return (GetHeroDamage(heroId) / total) * 100f;
        }

        public float GetHeroDps(string heroId)
        {
            float time = Mathf.Max(1f, ElapsedCombatTime);
            return GetHeroDamage(heroId) / time;
        }

        public HeroCombatReport GetMvpReport()
        {
            var reports = GetAllHeroReports();
            if (reports.Count == 0)
            {
                return new HeroCombatReport
                {
                    heroId = "none",
                    displayName = "Fortress Keep",
                    totalDamage = 0f,
                    damagePercentage = 0f,
                    dps = 0f,
                    themeColor = Color.white
                };
            }
            return reports[0];
        }

        public List<HeroCombatReport> GetAllHeroReports()
        {
            var list = new List<HeroCombatReport>();
            float total = GetTotalDamage();
            float time = Mathf.Max(1f, ElapsedCombatTime);

            foreach (var kvp in damageByHero)
            {
                string id = kvp.Key;
                float dmg = kvp.Value;
                float pct = total > 0f ? (dmg / total) * 100f : 0f;
                int crits = critsByHero.TryGetValue(id, out int c) ? c : 0;

                list.Add(new HeroCombatReport
                {
                    heroId = id,
                    displayName = FormatHeroName(id),
                    totalDamage = dmg,
                    damagePercentage = pct,
                    dps = dmg / time,
                    critCount = crits,
                    themeColor = GetHeroThemeColor(id)
                });
            }

            list.Sort((a, b) => b.totalDamage.CompareTo(a.totalDamage));
            return list;
        }

        public static Color GetHeroThemeColor(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return Color.white;
            switch (heroId.ToLowerInvariant())
            {
                case "frost_mage": return new Color(0.35f, 0.85f, 1f, 1f);
                case "fire_mage": return new Color(1f, 0.45f, 0.15f, 1f);
                case "electric_engineer": return new Color(1f, 0.9f, 0.2f, 1f);
                case "plague_doctor": return new Color(0.4f, 0.95f, 0.35f, 1f);
                case "radiant_paladin": return new Color(1f, 0.85f, 0.4f, 1f);
                case "archer": return new Color(0.85f, 0.95f, 0.85f, 1f);
                case "bombardier": return new Color(1f, 0.6f, 0.3f, 1f);
                case "sniper": return new Color(0.85f, 0.5f, 1f, 1f);
                default:
                    if (heroId.StartsWith("crystal")) return new Color(0.3f, 0.7f, 1f, 1f);
                    return new Color(0.75f, 0.8f, 0.9f, 1f);
            }
        }

        private static string FormatHeroName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Unknown";
            string formatted = System.Text.RegularExpressions.Regex.Replace(id, @"_([a-z])", m => " " + m.Groups[1].Value.ToUpper());
            return char.ToUpper(formatted[0]) + formatted.Substring(1);
        }
    }
}
