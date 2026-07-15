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
            ResetController();
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
            if (enemy == null || enemy.IsDead)
            {
                ResetController();
                return;
            }

            bool slowChanged = false;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                StatusEffect effect = activeEffects[i];
                effect.RemainingTime -= Time.deltaTime;

                if (effect.RemainingTime <= 0f)
                {
                    if (effect.EffectType == StatusEffectType.Slow || effect.EffectType == StatusEffectType.Stun)
                    {
                        slowChanged = true;
                    }
                    activeEffects.RemoveAt(i);
                    continue;
                }

                // Handle Burn DoT tick (deals damage once per second)
                if (effect.EffectType == StatusEffectType.Burn)
                {
                    effect.TickTimer -= Time.deltaTime;
                    if (effect.TickTimer <= 0f)
                    {
                        effect.TickTimer = 1.0f; // Reset tick timer

                        float damageAmount = effect.Value;
                        float appliedDamage = enemy.TakeDamage(damageAmount);
                        DamageTracker.RecordDamage(effect.SourceHeroId, appliedDamage);
                    }
                }
            }

            if (slowChanged)
            {
                UpdateEnemySlowMultiplier();
            }

            UpdateVisualTint();
            UpdateStatusParticles();
        }

        private void UpdateStatusParticles()
        {
            StatusEffectType[] typesToCheck = { StatusEffectType.Slow, StatusEffectType.Burn, StatusEffectType.Shock, StatusEffectType.Stun };

            foreach (var type in typesToCheck)
            {
                bool hasEffect = activeEffects.Exists(e => e.EffectType == type);
                
                bool isFreeze = false;
                if (type == StatusEffectType.Slow && hasEffect)
                {
                    var slowEffect = activeEffects.Find(e => e.EffectType == StatusEffectType.Slow);
                    if (slowEffect != null && slowEffect.Value <= 0.05f)
                    {
                        isFreeze = true;
                    }
                }

                if (hasEffect)
                {
                    if (!activeParticles.ContainsKey(type))
                    {
                        ParticleSystem ps = VfxManager.Instance?.GetStatusEffectParticle(type, transform, isFreeze);
                        if (ps != null)
                        {
                            activeParticles[type] = ps;
                        }
                    }
                }
                else
                {
                    if (activeParticles.TryGetValue(type, out ParticleSystem ps))
                    {
                        VfxManager.Instance?.ReturnStatusEffectParticle(type, ps);
                        activeParticles.Remove(type);
                    }
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

            // Priority: Stun > Shock > Burn > Slow
            Color targetTint = Color.white;
            bool hasTint = false;

            if (activeEffects.Exists(e => e.EffectType == StatusEffectType.Stun))
            {
                targetTint = StunTint;
                hasTint = true;
            }
            else if (activeEffects.Exists(e => e.EffectType == StatusEffectType.Shock))
            {
                targetTint = ShockTint;
                hasTint = true;
            }
            else if (activeEffects.Exists(e => e.EffectType == StatusEffectType.Burn))
            {
                targetTint = BurnTint;
                hasTint = true;
            }
            else if (activeEffects.Exists(e => e.EffectType == StatusEffectType.Slow))
            {
                var slow = activeEffects.Find(e => e.EffectType == StatusEffectType.Slow);
                if (slow != null && slow.Value <= 0.05f)
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

            StatusEffect activeSlow = activeEffects.Find(e => e.EffectType == StatusEffectType.Slow);
            StatusEffect activeStun = activeEffects.Find(e => e.EffectType == StatusEffectType.Stun);
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
            return activeEffects.Exists(e => e.EffectType == StatusEffectType.Shock);
        }
    }
}
