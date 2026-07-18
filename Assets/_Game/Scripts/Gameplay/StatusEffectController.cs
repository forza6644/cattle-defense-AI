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
        private bool slowIsFreeze;
        private StatusEffect activeSlow;
        private StatusEffect activeStun;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly Color SlowTint = new Color(0.4f, 0.8f, 1f, 1f);
        private static readonly Color BurnTint = new Color(1f, 0.45f, 0.15f, 1f);
        private static readonly Color ShockTint = new Color(1f, 0.95f, 0.3f, 1f);
        private static readonly Color StunTint = new Color(0.7f, 0.7f, 0.7f, 1f);
        private const float TintStrength = 0.45f;

        public IReadOnlyList<StatusEffect> ActiveEffects => activeEffects;

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
            }

            RebuildActiveState(true);
            enabled = activeEffects.Count > 0;
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
            slowIsFreeze = false;
            activeSlow = null;
            activeStun = null;

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

        private void UpdateVisualTint()
        {
            if (renderers == null)
            {
                CacheRenderers();
                if (renderers == null) return;
            }

            // Priority: Stun > Shock > Burn > Slow
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
