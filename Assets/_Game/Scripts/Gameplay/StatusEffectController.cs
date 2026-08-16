using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Component placed on or attached to an Enemy to track, update, and apply status effects (Slow, Burn, Shock).
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class StatusEffectController : MonoBehaviour
    {
        private Enemy enemy;
        private readonly List<StatusEffect> activeEffects = new List<StatusEffect>();
        private readonly Dictionary<StatusEffectType, ParticleSystem> activeParticles = new Dictionary<StatusEffectType, ParticleSystem>();

        private Renderer[] renderers;
        private Color[] baseColors;
        private MaterialPropertyBlock mpb;
        private bool tintApplied;
        private bool hasSlow;
        private bool hasBurn;
        private bool hasShock;
        private bool hasStun;
        private bool hasPoison;
        private bool slowIsFreeze;
        private StatusEffect activeSlow;
        private StatusEffect activeStun;
        private StatusEffect activePoison;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly Color SlowTint = new Color(0.4f, 0.8f, 1f, 1f);
        private static readonly Color BurnTint = new Color(1f, 0.45f, 0.15f, 1f);
        private static readonly Color ShockTint = new Color(1f, 0.95f, 0.3f, 1f);
        private static readonly Color StunTint = new Color(0.7f, 0.7f, 0.7f, 1f);
        private static readonly Color PoisonTint = new Color(0.25f, 0.95f, 0.3f, 1f);
        private const float TintStrength = 0.45f;

        public static event System.Action<ElementalReactionType, Vector3, string> OnElementalReaction;

        public static void TriggerReactionEvent(ElementalReactionType reaction, Vector3 pos, string sourceHeroId)
        {
            OnElementalReaction?.Invoke(reaction, pos, sourceHeroId);
            CombatTelemetryManager.RecordReaction(reaction, 0f);
        }

        public IReadOnlyList<StatusEffect> ActiveEffects => activeEffects;
        public bool IsVulnerableToShatter => hasSlow || slowIsFreeze || hasStun;

        public bool HasEffect(StatusEffectType type)
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].EffectType == type) return true;
            }
            return false;
        }

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            CacheRenderers();
        }

        private void OnEnable()
        {
            if (activeEffects.Count == 0)
            {
                ResetController();
                enabled = false;
            }
        }

        private void OnDisable()
        {
            ResetController();
        }

        public void ResetController()
        {
            foreach (var kvp in activeParticles)
            {
                if (kvp.Value != null)
                {
                    VfxManager.Instance?.ReturnStatusEffectParticle(kvp.Key, kvp.Value);
                }
            }
            activeParticles.Clear();
            activeEffects.Clear();
            hasSlow = false;
            hasBurn = false;
            hasShock = false;
            hasStun = false;
            slowIsFreeze = false;
            activeSlow = null;
            activeStun = null;

            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    renderers[i].SetPropertyBlock(null);
                }
            }
            tintApplied = false;
        }

        private void CacheRenderers()
        {
            if (renderers != null) return;
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material shared = renderers[i].sharedMaterial;
                baseColors[i] = shared != null && shared.HasProperty(BaseColorId)
                    ? shared.GetColor(BaseColorId)
                    : Color.white;
            }
            mpb = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Applies a status effect to the enemy.
        /// </summary>
        public void ApplyEffect(StatusEffect effect)
        {
            if (effect == null || effect.EffectType == StatusEffectType.None)
            {
                return;
            }

            bool hadSlow = hasSlow;
            bool hadBurn = hasBurn;
            bool hadShock = hasShock;
            bool hadPoison = hasPoison;

            if (effect.EffectType == StatusEffectType.Burn)
            {
                if (hadSlow) TriggerThermalShock(effect);
                if (hadShock) TriggerOverload(effect);
                if (hadPoison) TriggerCorrosiveBlast(effect);
            }
            else if (effect.EffectType == StatusEffectType.Slow)
            {
                if (hadBurn) TriggerThermalShock(effect);
                if (hadPoison) TriggerBrittleBlight(effect);
            }
            else if (effect.EffectType == StatusEffectType.Shock)
            {
                if (hadBurn) TriggerOverload(effect);
                if (hadPoison) TriggerNeurotoxin(effect);
            }
            else if (effect.EffectType == StatusEffectType.Poison)
            {
                if (hadBurn) TriggerCorrosiveBlast(effect);
                if (hadShock) TriggerNeurotoxin(effect);
                if (hadSlow) TriggerBrittleBlight(effect);
            }

            switch (effect.EffectType)
            {
                case StatusEffectType.Slow:
                    HandleApplySlow(effect);
                    break;
                case StatusEffectType.Burn:
                    HandleApplyBurn(effect);
                    break;
                case StatusEffectType.Shock:
                    HandleApplyShock(effect);
                    break;
                case StatusEffectType.Stun:
                    HandleApplyStun(effect);
                    break;
                case StatusEffectType.Poison:
                    HandleApplyPoison(effect);
                    break;
            }

            RebuildActiveState(true);
            enabled = activeEffects.Count > 0;
        }

        private float GetTotalReactionMultiplier(string heroId)
        {
            float mult = 1f;
            if (RunModifierManager.Instance != null && !string.IsNullOrEmpty(heroId))
            {
                mult *= RunModifierManager.Instance.GetElementalReactionMultiplier(heroId);
            }
            if (RelicManager.Instance != null)
            {
                mult *= RelicManager.Instance.GetElementalReactionMultiplier();
            }
            return mult;
        }

        private void TriggerCorrosiveBlast(StatusEffect incomingEffect)
        {
            if (enemy == null || enemy.IsDead) return;

            StatusEffect pEffect = activeEffects.Find(e => e.EffectType == StatusEffectType.Poison);
            float poisonVal = pEffect != null ? pEffect.Value : (incomingEffect.EffectType == StatusEffectType.Poison ? incomingEffect.Value : 15f);
            float baseBurst = Mathf.Max(25f, poisonVal * 2.0f);

            string heroId = incomingEffect.SourceHeroId;
            float reactionMultiplier = GetTotalReactionMultiplier(heroId);
            if (RunModifierManager.Instance != null && !string.IsNullOrEmpty(heroId))
            {
                if (RunModifierManager.Instance.HasBehavior(heroId, HeroBehaviorEffectType.CorrosiveArmorShred))
                {
                    reactionMultiplier += 0.5f;
                }
            }

            float finalDamage = baseBurst * reactionMultiplier;
            float applied = enemy.TakeDamage(finalDamage, true);
            if (!string.IsNullOrEmpty(heroId))
            {
                DamageTracker.RecordDamage(heroId, applied);
            }

            var allEnemies = EnemyManager.All;
            if (allEnemies != null)
            {
                for (int i = 0; i < allEnemies.Count; i++)
                {
                    Enemy other = allEnemies[i];
                    if (other != null && other != enemy && !other.IsDead)
                    {
                        if (Vector3.Distance(transform.position, other.transform.position) <= 4.5f)
                        {
                            float splash = other.TakeDamage(finalDamage * 0.5f, false);
                            if (!string.IsNullOrEmpty(heroId))
                            {
                                DamageTracker.RecordDamage(heroId, splash);
                            }
                        }
                    }
                }
            }

            OnElementalReaction?.Invoke(ElementalReactionType.CorrosiveBlast, transform.position, heroId);
            VfxManager.Instance?.PlayHeroProjectileImpact(transform.position, "plague_doctor", true);
        }

        private void TriggerNeurotoxin(StatusEffect incomingEffect)
        {
            if (enemy == null || enemy.IsDead) return;

            StatusEffect pEffect = activeEffects.Find(e => e.EffectType == StatusEffectType.Poison);
            float poisonVal = pEffect != null ? pEffect.Value : (incomingEffect.EffectType == StatusEffectType.Poison ? incomingEffect.Value : 15f);
            float baseBurst = Mathf.Max(22f, poisonVal * 1.6f);

            string heroId = incomingEffect.SourceHeroId;
            float reactionMultiplier = GetTotalReactionMultiplier(heroId);

            float finalDamage = baseBurst * reactionMultiplier;
            float applied = enemy.TakeDamage(finalDamage, true);
            if (!string.IsNullOrEmpty(heroId))
            {
                DamageTracker.RecordDamage(heroId, applied);
            }

            HandleApplyStun(new StatusEffect(StatusEffectType.Stun, 0f, 1.5f, heroId));

            var allEnemies = EnemyManager.All;
            int chained = 0;
            int maxChained = 2 + (RelicManager.Instance != null ? RelicManager.Instance.GetShockChainBonus() : 0);
            if (allEnemies != null)
            {
                for (int i = 0; i < allEnemies.Count && chained < maxChained; i++)
                {
                    Enemy other = allEnemies[i];
                    if (other != null && other != enemy && !other.IsDead)
                    {
                        if (Vector3.Distance(transform.position, other.transform.position) <= 5.0f)
                        {
                            StatusEffectController otherSec = other.GetComponent<StatusEffectController>() ?? other.gameObject.AddComponent<StatusEffectController>();
                            otherSec.ApplyEffect(new StatusEffect(StatusEffectType.Shock, 1f, 3f, heroId));
                            chained++;
                        }
                    }
                }
            }

            OnElementalReaction?.Invoke(ElementalReactionType.Neurotoxin, transform.position, heroId);
            VfxManager.Instance?.PlayHeroProjectileImpact(transform.position, "electric_engineer", false);
        }

        private void TriggerBrittleBlight(StatusEffect incomingEffect)
        {
            if (enemy == null || enemy.IsDead) return;

            StatusEffect pEffect = activeEffects.Find(e => e.EffectType == StatusEffectType.Poison);
            float poisonVal = pEffect != null ? pEffect.Value : (incomingEffect.EffectType == StatusEffectType.Poison ? incomingEffect.Value : 12f);
            float baseBurst = Mathf.Max(20f, poisonVal * 1.5f);

            string heroId = incomingEffect.SourceHeroId;
            float reactionMultiplier = GetTotalReactionMultiplier(heroId);

            float finalDamage = baseBurst * reactionMultiplier;
            float applied = enemy.TakeDamage(finalDamage, true);
            if (!string.IsNullOrEmpty(heroId))
            {
                DamageTracker.RecordDamage(heroId, applied);
            }

            OnElementalReaction?.Invoke(ElementalReactionType.BrittleBlight, transform.position, heroId);
            VfxManager.Instance?.PlayHeroProjectileImpact(transform.position, "frost_mage", false);
        }

        private void TriggerThermalShock(StatusEffect incomingEffect)
        {
            if (enemy == null || enemy.IsDead) return;

            StatusEffect activeBurn = activeEffects.Find(e => e.EffectType == StatusEffectType.Burn);
            float burnVal = activeBurn != null ? activeBurn.Value : (incomingEffect.EffectType == StatusEffectType.Burn ? incomingEffect.Value : 15f);
            float baseBurst = Mathf.Max(20f, burnVal * 1.8f);

            string heroId = incomingEffect.SourceHeroId;
            float reactionMultiplier = GetTotalReactionMultiplier(heroId);
            if (RunModifierManager.Instance != null && !string.IsNullOrEmpty(heroId))
            {
                if (RunModifierManager.Instance.HasBehavior(heroId, HeroBehaviorEffectType.ThermalShockMastery))
                {
                    reactionMultiplier += 0.5f;
                }
            }

            float finalDamage = baseBurst * reactionMultiplier;
            float applied = enemy.TakeDamage(finalDamage, true);
            if (!string.IsNullOrEmpty(heroId))
            {
                DamageTracker.RecordDamage(heroId, applied);
            }

            OnElementalReaction?.Invoke(ElementalReactionType.ThermalShock, transform.position, heroId);
            VfxManager.Instance?.PlayHeroProjectileImpact(transform.position, "frost_mage", false);
        }

        private void TriggerOverload(StatusEffect incomingEffect)
        {
            if (enemy == null || enemy.IsDead) return;

            StatusEffect activeBurn = activeEffects.Find(e => e.EffectType == StatusEffectType.Burn);
            float burnVal = activeBurn != null ? activeBurn.Value : (incomingEffect.EffectType == StatusEffectType.Burn ? incomingEffect.Value : 10f);
            float baseBurst = Mathf.Max(25f, burnVal * 1.5f);

            string heroId = incomingEffect.SourceHeroId;
            float reactionMultiplier = GetTotalReactionMultiplier(heroId);
            if (RunModifierManager.Instance != null && !string.IsNullOrEmpty(heroId))
            {
                if (RunModifierManager.Instance.HasBehavior(heroId, HeroBehaviorEffectType.SuperconductorDischarge))
                {
                    reactionMultiplier += 0.5f;
                }
            }

            float finalDamage = baseBurst * reactionMultiplier;
            float applied = enemy.TakeDamage(finalDamage, true);
            if (!string.IsNullOrEmpty(heroId))
            {
                DamageTracker.RecordDamage(heroId, applied);
            }

            // Arc to up to 2 nearby registered enemies within 5.0m
            var allEnemies = EnemyManager.All;
            int chained = 0;
            if (allEnemies != null)
            {
                for (int i = 0; i < allEnemies.Count && chained < 2; i++)
                {
                    Enemy other = allEnemies[i];
                    if (other != null && other != enemy && !other.IsDead)
                    {
                        float dist = Vector3.Distance(transform.position, other.transform.position);
                        if (dist <= 5.0f)
                        {
                            float chainDmg = other.TakeDamage(finalDamage * 0.6f, true);
                            if (!string.IsNullOrEmpty(heroId))
                            {
                                DamageTracker.RecordDamage(heroId, chainDmg);
                            }
                            chained++;
                        }
                    }
                }
            }

            OnElementalReaction?.Invoke(ElementalReactionType.Overload, transform.position, heroId);
            VfxManager.Instance?.PlayHeroProjectileImpact(transform.position, "electric_engineer", false);
        }


        public void RemoveEffectsFromSource(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId)) return;
            bool slowChanged = false;
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].SourceHeroId != sourceId) continue;
                slowChanged |= activeEffects[i].EffectType == StatusEffectType.Slow || activeEffects[i].EffectType == StatusEffectType.Stun;
                activeEffects.RemoveAt(i);
            }
            if (slowChanged) UpdateEnemySlowMultiplier();
            RebuildActiveState(true);
            enabled = activeEffects.Count > 0;
        }

        private void HandleApplySlow(StatusEffect newEffect)
        {
            StatusEffect existingSlow = activeEffects.Find(e => e.EffectType == StatusEffectType.Slow);
            if (existingSlow != null)
            {
                // Rule: refresh duration if same or stronger slow (new slow multiplier <= existing slow multiplier)
                if (newEffect.Value <= existingSlow.Value)
                {
                    existingSlow.Value = newEffect.Value;
                    existingSlow.Duration = newEffect.Duration;
                    existingSlow.RemainingTime = newEffect.Duration;
                    existingSlow.SourceHeroId = newEffect.SourceHeroId;
                }
            }
            else
            {
                activeEffects.Add(newEffect);
            }
            UpdateEnemySlowMultiplier();
        }

        private void HandleApplyBurn(StatusEffect newEffect)
        {
            // Rule: refresh duration/value if same source hero
            StatusEffect existingBurn = activeEffects.Find(e => e.EffectType == StatusEffectType.Burn && e.SourceHeroId == newEffect.SourceHeroId);
            if (existingBurn != null)
            {
                existingBurn.Value = newEffect.Value; // Update Burn damage per tick
                existingBurn.Duration = newEffect.Duration;
                existingBurn.RemainingTime = newEffect.Duration;
            }
            else
            {
                activeEffects.Add(newEffect);
            }
        }

        private void HandleApplyShock(StatusEffect newEffect)
        {
            // Rule: refresh duration of shock
            StatusEffect existingShock = activeEffects.Find(e => e.EffectType == StatusEffectType.Shock);
            if (existingShock != null)
            {
                existingShock.Duration = newEffect.Duration;
                existingShock.RemainingTime = newEffect.Duration;
            }
            else
            {
                activeEffects.Add(newEffect);
            }
        }

        private void HandleApplyStun(StatusEffect newEffect)
        {
            StatusEffect existingStun = activeEffects.Find(e => e.EffectType == StatusEffectType.Stun);
            if (existingStun != null)
            {
                existingStun.Duration = newEffect.Duration;
                existingStun.RemainingTime = newEffect.Duration;
            }
            else
            {
                activeEffects.Add(newEffect);
            }
            UpdateEnemySlowMultiplier();
        }

        private void HandleApplyPoison(StatusEffect newEffect)
        {
            StatusEffect existingPoison = activeEffects.Find(e => e.EffectType == StatusEffectType.Poison && e.SourceHeroId == newEffect.SourceHeroId);
            if (existingPoison != null)
            {
                existingPoison.Value = Mathf.Max(existingPoison.Value, newEffect.Value);
                existingPoison.Duration = newEffect.Duration;
                existingPoison.RemainingTime = newEffect.Duration;
            }
            else
            {
                activeEffects.Add(newEffect);
            }
        }

        private void Update()
        {
            ProcessEffects(Time.deltaTime);
        }

        internal void ProcessEffects(float deltaTime)
        {
            if (enemy == null || enemy.IsDead)
            {
                ResetController();
                enabled = false;
                return;
            }

            if (activeEffects.Count == 0)
            {
                enabled = false;
                return;
            }

            bool slowChanged = false;
            bool stateChanged = false;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                StatusEffect effect = activeEffects[i];
                float activeDelta = Mathf.Min(Mathf.Max(0f, effect.RemainingTime), deltaTime);

                if (effect.EffectType == StatusEffectType.Burn)
                {
                    effect.TickTimer -= activeDelta;
                    int tickSafety = 0;
                    while (effect.TickTimer <= 0f && tickSafety < 16)
                    {
                        float appliedDamage = enemy.TakeDamage(effect.Value);
                        DamageTracker.RecordDamage(effect.SourceHeroId, appliedDamage);
                        effect.TickTimer += 1f;
                        tickSafety++;

                        if (enemy.IsDead)
                        {
                            break;
                        }
                    }
                }
                else if (effect.EffectType == StatusEffectType.Poison)
                {
                    effect.TickTimer -= activeDelta;
                    int tickSafety = 0;
                    while (effect.TickTimer <= 0f && tickSafety < 16)
                    {
                        float appliedDamage = enemy.TakeDamage(effect.Value);
                        DamageTracker.RecordDamage(effect.SourceHeroId, appliedDamage);
                        effect.TickTimer += 0.5f;
                        tickSafety++;

                        if (enemy.IsDead)
                        {
                            break;
                        }
                    }
                }

                effect.RemainingTime -= deltaTime;
                if (effect.RemainingTime <= 0f || enemy.IsDead)
                {
                    if (effect.EffectType == StatusEffectType.Slow || effect.EffectType == StatusEffectType.Stun)
                    {
                        slowChanged = true;
                    }
                    activeEffects.RemoveAt(i);
                    stateChanged = true;
                }

                if (enemy.IsDead)
                {
                    OnEnemyDeath();
                    break;
                }
            }

            if (stateChanged)
            {
                RebuildActiveState(true);
            }
            else if (slowChanged)
            {
                RebuildActiveState(false);
            }

            UpdateVisualTint();
            if (activeEffects.Count == 0)
            {
                enabled = false;
            }
        }

        private void RebuildActiveState(bool refreshParticles)
        {
            hasSlow = false;
            hasBurn = false;
            hasShock = false;
            hasStun = false;
            hasPoison = false;
            slowIsFreeze = false;
            activeSlow = null;
            activeStun = null;
            activePoison = null;

            for (int i = 0; i < activeEffects.Count; i++)
            {
                StatusEffect effect = activeEffects[i];
                switch (effect.EffectType)
                {
                    case StatusEffectType.Slow:
                        hasSlow = true;
                        activeSlow = effect;
                        slowIsFreeze = effect.Value <= 0.05f;
                        break;
                    case StatusEffectType.Burn:
                        hasBurn = true;
                        break;
                    case StatusEffectType.Shock:
                        hasShock = true;
                        break;
                    case StatusEffectType.Stun:
                        hasStun = true;
                        activeStun = effect;
                        break;
                    case StatusEffectType.Poison:
                        hasPoison = true;
                        activePoison = effect;
                        break;
                }
            }

            UpdateEnemySlowMultiplier();
            if (refreshParticles)
            {
                SyncStatusParticle(StatusEffectType.Slow, hasSlow, slowIsFreeze);
                SyncStatusParticle(StatusEffectType.Burn, hasBurn, false);
                SyncStatusParticle(StatusEffectType.Shock, hasShock, false);
                SyncStatusParticle(StatusEffectType.Stun, hasStun, false);
            }
        }

        private void SyncStatusParticle(StatusEffectType type, bool isActive, bool isFreeze)
        {
            if (isActive)
            {
                if (!activeParticles.ContainsKey(type))
                {
                    ParticleSystem ps = VfxManager.Instance?.GetStatusEffectParticle(type, transform, isFreeze);
                    if (ps != null)
                    {
                        activeParticles[type] = ps;
                    }
                }
                return;
            }

            if (activeParticles.TryGetValue(type, out ParticleSystem activeParticle))
            {
                VfxManager.Instance?.ReturnStatusEffectParticle(type, activeParticle);
                activeParticles.Remove(type);
            }
        }

        public void OnEnemyDeath()
        {
            if (hasPoison)
            {
                string pSource = activePoison != null ? activePoison.SourceHeroId : "plague_doctor";
                if (RunModifierManager.Instance != null && RunModifierManager.Instance.HasBehavior(pSource, HeroBehaviorEffectType.EpidemicSpread))
                {
                    var nearby = EnemyManager.All;
                    if (nearby != null)
                    {
                        int spreadCount = 0;
                        for (int n = 0; n < nearby.Count && spreadCount < 3; n++)
                        {
                            Enemy nEnemy = nearby[n];
                            if (nEnemy != null && nEnemy != enemy && !nEnemy.IsDead)
                            {
                                if (Vector3.Distance(transform.position, nEnemy.transform.position) <= 4.0f)
                                {
                                    StatusEffectController nSec = nEnemy.GetComponent<StatusEffectController>() ?? nEnemy.gameObject.AddComponent<StatusEffectController>();
                                    nSec.ApplyEffect(new StatusEffect(StatusEffectType.Poison, activePoison != null ? activePoison.Value : 10f, 4f, pSource));
                                    spreadCount++;
                                }
                            }
                        }
                    }
                }

                if (pSource == "plague_doctor" || (RunModifierManager.Instance != null && RunModifierManager.Instance.HasBehavior(pSource, HeroBehaviorEffectType.ArmyOfTheDead)))
                {
                    SpectralMinion.Spawn(transform.position, 15f, 10f, 50f, pSource);
                }
            }
        }

        private void UpdateVisualTint()
        {
            if (renderers == null)
            {
                CacheRenderers();
                if (renderers == null) return;
            }

            // Priority: Stun > Shock > Burn > Poison > Slow
            Color targetTint = Color.white;
            bool hasTint = false;

            if (hasStun)
            {
                targetTint = StunTint;
                hasTint = true;
            }
            else if (hasShock)
            {
                targetTint = ShockTint;
                hasTint = true;
            }
            else if (hasBurn)
            {
                targetTint = BurnTint;
                hasTint = true;
            }
            else if (hasPoison)
            {
                targetTint = PoisonTint;
                hasTint = true;
            }
            else if (hasSlow)
            {
                if (slowIsFreeze)
                {
                    // Freeze feedback: icy cyan tint
                    targetTint = new Color(0.2f, 0.7f, 1f, 1f);
                }
                else
                {
                    targetTint = SlowTint;
                }
                hasTint = true;
            }

            if (!hasTint && !tintApplied)
            {
                return; // nothing tinted and nothing to clean up
            }

            // Re-assert the tint every frame while active (the hit flash also writes
            // _BaseColor and clears its block when it ends), and restore base colors
            // exactly once when the last effect expires. Always read-modify-write the
            // block - never SetPropertyBlock(null) - so unrelated property values
            // written by other systems survive.
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].GetPropertyBlock(mpb);
                Color final = hasTint
                    ? Color.Lerp(baseColors[i], targetTint, TintStrength)
                    : baseColors[i];
                mpb.SetColor(BaseColorId, final);
                renderers[i].SetPropertyBlock(mpb);
            }

            tintApplied = hasTint;
        }

        private void UpdateEnemySlowMultiplier()
        {
            if (enemy == null) return;

            if (activeStun != null)
            {
                enemy.SlowMultiplier = 0f;
                enemy.SlowTimer = activeStun.RemainingTime;
            }
            else if (activeSlow != null)
            {
                enemy.SlowMultiplier = activeSlow.Value;
                enemy.SlowTimer = activeSlow.RemainingTime;
            }
            else
            {
                enemy.SlowMultiplier = 1f;
                enemy.SlowTimer = 0f;
            }
        }

        /// <summary>
        /// Query if the enemy is currently shocked.
        /// </summary>
        public bool IsShocked()
        {
            return hasShock;
        }
    }
}
