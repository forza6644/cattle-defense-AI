using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Manages player-triggered active Fortress Ultimates and Castle Defense Weapons,
    /// tracking energy resource pools, cooldowns, and executing targeted artillery and barrier bursts.
    /// </summary>
    public class CastleAbilityManager : MonoBehaviour
    {
        public static CastleAbilityManager Instance { get; private set; }

        public static event Action<CastleAbilityDefinition> OnAbilityCast;
        public static event Action<float, float> OnEnergyChanged;

        [Header("Energy Settings")]
        public float maxEnergy = 100f;
        public float currentEnergy = 50f;
        public float energyRegenPerSecond = 3.0f;

        private readonly List<CastleAbilityDefinition> allAbilities = new List<CastleAbilityDefinition>();
        private readonly Dictionary<string, float> cooldownTimers = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<CastleAbilityDefinition> AllAbilities => allAbilities;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            OnAbilityCast = null;
            OnEnergyChanged = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadAbilities();
        }

        private void Update()
        {
            // Update cooldowns
            List<string> keys = new List<string>(cooldownTimers.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string k = keys[i];
                if (cooldownTimers[k] > 0f)
                {
                    cooldownTimers[k] -= Time.deltaTime;
                    if (cooldownTimers[k] < 0f) cooldownTimers[k] = 0f;
                }
            }

            // Passive energy regen during gameplay
            if (currentEnergy < maxEnergy)
            {
                AddEnergy(energyRegenPerSecond * Time.deltaTime);
            }
        }

        public void LoadAbilities()
        {
            allAbilities.Clear();
            cooldownTimers.Clear();

            var loaded = Resources.LoadAll<CastleAbilityDefinition>("CastleAbilities");
            if (loaded != null && loaded.Length > 0)
            {
                for (int i = 0; i < loaded.Length; i++)
                {
                    allAbilities.Add(loaded[i]);
                    cooldownTimers[loaded[i].id] = 0f;
                }
            }

            if (allAbilities.Count == 0)
            {
                CreateDefaultAbilities();
            }
        }

        public CastleAbilityDefinition GetAbility(string id)
        {
            for (int i = 0; i < allAbilities.Count; i++)
            {
                if (string.Equals(allAbilities[i].id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return allAbilities[i];
                }
            }
            return null;
        }

        public float GetCooldownRemaining(string id)
        {
            return cooldownTimers.TryGetValue(id, out var t) ? t : 0f;
        }

        public bool IsReady(string id)
        {
            var ability = GetAbility(id);
            if (ability == null) return false;
            return GetCooldownRemaining(id) <= 0f && currentEnergy >= ability.energyCost;
        }

        public bool CastAbility(string id, Vector3 targetPosition)
        {
            var ability = GetAbility(id);
            if (ability == null) return false;

            if (!IsReady(id)) return false;

            currentEnergy -= ability.energyCost;
            cooldownTimers[id] = ability.cooldown;
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

            ExecuteAbilityEffect(ability, targetPosition);
            OnAbilityCast?.Invoke(ability);
            HapticFeedbackManager.TriggerMedium();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCastleAbilitySfx(ability.id);
            }

            Debug.Log($"[CastleAbilityManager] ⚔️ CAST FORTRESS ULTIMATE: {ability.displayName} at {targetPosition}!");
            return true;
        }

        public void AddEnergy(float amount)
        {
            if (amount == 0f) return;
            currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }

        private void ExecuteAbilityEffect(CastleAbilityDefinition ability, Vector3 targetPos)
        {
            switch (ability.abilityType)
            {
                case CastleAbilityType.ArcaneMortar:
                    ExecuteMortarStrike(targetPos, ability.radius, ability.damage);
                    break;

                case CastleAbilityType.FortressAegis:
                    ExecuteFortressAegis(ability.shieldAmount);
                    break;

                case CastleAbilityType.CallMilitia:
                    ExecuteCallMilitia(targetPos);
                    break;
            }
        }

        private void ExecuteMortarStrike(Vector3 targetPos, float radius, float damage)
        {
            var colliders = Physics.OverlapSphere(targetPos, radius);
            for (int i = 0; i < colliders.Length; i++)
            {
                var enemy = colliders[i].GetComponentInParent<Enemy>();
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.TakeDamage(damage);
                    CombatTelemetryManager.RecordDamage("CastleMortar", damage, false);
                }
            }
        }

        private void ExecuteFortressAegis(float shieldAmount)
        {
            var castle = FindFirstObjectByType<Castle>();
            if (castle != null)
            {
                castle.AddShield(shieldAmount);
            }
        }

        private void ExecuteCallMilitia(Vector3 targetPos)
        {
            Debug.Log($"[CastleAbilityManager] 🛡️ Militia Vanguard deployed at {targetPos}!");
            // Deploys barricade shockwave around point
            var colliders = Physics.OverlapSphere(targetPos, 4.0f);
            for (int i = 0; i < colliders.Length; i++)
            {
                var enemy = colliders[i].GetComponentInParent<Enemy>();
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, 0.5f, 5.0f, "Militia"));
                }
            }
        }

        private void CreateDefaultAbilities()
        {
            AddDefault("ability_mortar", "Arcane Mortar Strike", "Targeted heavy artillery shell detonating for 350 AOE blast damage.", CastleAbilityType.ArcaneMortar, 20f, 30f, 350f, 5f, 0f, "☄️", new Color(1f, 0.45f, 0.2f));
            AddDefault("ability_barrier", "Fortress Kinetic Aegis", "Overcharges castle generators, granting +300 Kinetic Shield.", CastleAbilityType.FortressAegis, 30f, 40f, 0f, 0f, 300f, "🛡️", new Color(0.3f, 0.75f, 1f));
            AddDefault("ability_militia", "Call the Militia", "Rallies defensive militia to fortify the lane and slow incoming invaders.", CastleAbilityType.CallMilitia, 25f, 35f, 100f, 4f, 0f, "⚔️", new Color(0.85f, 0.75f, 0.3f));
        }

        private void AddDefault(string id, string name, string desc, CastleAbilityType type, float cd, float energy, float dmg, float radius, float shield, string icon, Color color)
        {
            var ability = ScriptableObject.CreateInstance<CastleAbilityDefinition>();
            ability.id = id;
            ability.displayName = name;
            ability.description = desc;
            ability.abilityType = type;
            ability.cooldown = cd;
            ability.energyCost = energy;
            ability.damage = dmg;
            ability.radius = radius;
            ability.shieldAmount = shield;
            ability.iconBadge = icon;
            ability.themeColor = color;
            allAbilities.Add(ability);
            cooldownTimers[id] = 0f;
        }
    }
}
