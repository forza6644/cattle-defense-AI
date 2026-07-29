using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Standalone combat controller for the fortress Starter Crystal.
    /// Occupies its own dedicated position above the fortress gate.
    /// Independent of HeroSlots, HeroRosterManager ownership, and character rigs.
    /// </summary>
    public class StarterCrystal : MonoBehaviour
    {
        [SerializeField] private StarterCrystalDefinition definition;
        [SerializeField] private Vector3 launchOffset = new Vector3(0f, 0.5f, 0f);

        private float targetRefreshTimer;
        private float fireCooldown;
        private Enemy currentTarget;
        private Renderer crystalRenderer;
        private MeshFilter crystalMeshFilter;
        private readonly int[] cachedChainHitIds = new int[16];

        public StarterCrystalDefinition Definition => definition;

        private void Awake()
        {
            crystalRenderer = GetComponent<Renderer>();
            if (crystalRenderer == null)
            {
                crystalRenderer = GetComponentInChildren<Renderer>();
            }
            crystalMeshFilter = GetComponent<MeshFilter>();
            if (crystalMeshFilter == null)
            {
                crystalMeshFilter = GetComponentInChildren<MeshFilter>();
            }
        }

        private void Start()
        {
            LoadSelectedCrystal();
        }

        public void Configure(StarterCrystalDefinition crystalDefinition)
        {
            definition = crystalDefinition;
            if (definition == null)
            {
                return;
            }

            if (crystalRenderer != null && definition.crystalMaterial != null)
            {
                crystalRenderer.sharedMaterial = definition.crystalMaterial;
            }

            if (crystalMeshFilter != null && definition.crystalMesh != null)
            {
                crystalMeshFilter.sharedMesh = definition.crystalMesh;
            }

            fireCooldown = 0f;
            targetRefreshTimer = 0f;
        }

        public void LoadSelectedCrystal()
        {
            if (definition != null)
            {
                return;
            }

            string selectedId = SaveManager.SelectedStarterCrystalId;
            if (string.IsNullOrEmpty(selectedId))
            {
                selectedId = "crystal_lightning";
            }

            StarterCrystalDefinition def = Resources.Load<StarterCrystalDefinition>("Crystals/" + selectedId);
            if (def == null)
            {
                // Fallback to Resources or direct load
                StarterCrystalDefinition[] allCrystals = Resources.LoadAll<StarterCrystalDefinition>("");
                for (int i = 0; i < allCrystals.Length; i++)
                {
                    if (allCrystals[i] != null && allCrystals[i].crystalId == selectedId)
                    {
                        def = allCrystals[i];
                        break;
                    }
                }
            }

            if (def != null)
            {
                Configure(def);
            }
            else
            {
                Debug.LogWarning($"[StarterCrystal] Definition for '{selectedId}' not found. Using current configuration.");
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            if (definition == null)
            {
                return;
            }

            targetRefreshTimer -= Time.deltaTime;
            fireCooldown -= Time.deltaTime;

            if (targetRefreshTimer <= 0f)
            {
                currentTarget = EnemyManager.FindTarget(transform.position, 1000f, TargetingMode.ClosestToGoal);
                targetRefreshTimer = 0.15f;
            }

            if (currentTarget == null || !currentTarget.IsTargetable)
            {
                return;
            }

            Vector3 direction = currentTarget.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
            }

            if (fireCooldown <= 0f)
            {
                FireAttack(currentTarget);
                float rate = GetModifiedFireRate();
                fireCooldown = rate > 0f ? 1f / rate : 1f;
            }
        }

        public float GetModifiedDamage()
        {
            float damage = definition != null ? definition.baseDamage : 0f;
            if (MetaUpgradeManager.Instance != null)
            {
                damage *= MetaUpgradeManager.Instance.GetGlobalDamageMultiplier();
            }
            return damage;
        }

        public float GetModifiedFireRate()
        {
            float fireRate = definition != null ? definition.attacksPerSecond : 1f;
            if (MetaUpgradeManager.Instance != null)
            {
                fireRate *= MetaUpgradeManager.Instance.GetGlobalFireRateMultiplier();
            }
            return fireRate;
        }

        public float GetModifiedRange()
        {
            float range = definition != null ? definition.attackRange : 14f;
            if (MetaUpgradeManager.Instance != null)
            {
                range *= MetaUpgradeManager.Instance.GetGlobalRangeMultiplier();
            }
            return range;
        }

        private void FireAttack(Enemy primaryTarget)
        {
            if (primaryTarget == null || !primaryTarget.IsTargetable)
            {
                return;
            }

            Vector3 origin = transform.position + launchOffset;
            float damage = GetModifiedDamage();

            switch (definition.element)
            {
                case CrystalElement.Fire:
                    ExecuteFireAttack(primaryTarget, origin, damage);
                    break;
                case CrystalElement.Ice:
                    ExecuteIceAttack(primaryTarget, origin, damage);
                    break;
                case CrystalElement.Lightning:
                    ExecuteLightningAttack(primaryTarget, origin, damage);
                    break;
                case CrystalElement.Stone:
                    ExecuteStoneAttack(primaryTarget, origin, damage);
                    break;
                case CrystalElement.Shadow:
                    ExecuteShadowAttack(primaryTarget, origin, damage);
                    break;
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroMuzzle(origin, "electric_engineer");
            }
        }

        private void ExecuteFireAttack(Enemy target, Vector3 origin, float damage)
        {
            float applied = target.TakeDamage(damage);
            DamageTracker.RecordDamage(definition.crystalId, applied);

            if (!target.IsDead && definition.damageOverTime > 0f)
            {
                target.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, definition.damageOverTime, definition.damageOverTimeDuration, definition.crystalId));
            }

            if (definition.splashRadius > 0f)
            {
                float radiusSqr = definition.splashRadius * definition.splashRadius;
                var enemies = EnemyManager.All;
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Enemy enemy = enemies[i];
                    if (enemy != null && enemy != target && !enemy.IsDead && (enemy.transform.position - target.transform.position).sqrMagnitude <= radiusSqr)
                    {
                        float splashDmg = enemy.TakeDamage(damage * 0.6f);
                        DamageTracker.RecordDamage(definition.crystalId, splashDmg);
                        if (!enemy.IsDead && definition.damageOverTime > 0f)
                        {
                            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, definition.damageOverTime * 0.6f, definition.damageOverTimeDuration, definition.crystalId));
                        }
                    }
                }
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayAbilityTrace(origin, target.transform.position, "fire_mage", 0.15f);
            }
        }

        private void ExecuteIceAttack(Enemy target, Vector3 origin, float damage)
        {
            float applied = target.TakeDamage(damage);
            DamageTracker.RecordDamage(definition.crystalId, applied);

            if (!target.IsDead)
            {
                float slowMult = Mathf.Clamp(1f - definition.statusMagnitude, 0.1f, 0.9f);
                float duration = definition.statusDuration > 0f ? definition.statusDuration : 3f;
                target.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, slowMult, duration, definition.crystalId));
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayAbilityTrace(origin, target.transform.position, "frost_mage", 0.15f);
                VfxManager.Instance.PlayFrost(target.transform.position, 1.2f);
            }
        }

        private void ExecuteLightningAttack(Enemy primaryTarget, Vector3 origin, float damage)
        {
            int maxChains = Mathf.Max(1, definition.chainTargets);
            Vector3 sourcePos = origin;
            Enemy current = primaryTarget;

            for (int i = 0; i < cachedChainHitIds.Length; i++)
            {
                cachedChainHitIds[i] = 0;
            }

            int hitCount = 0;

            for (int b = 0; b < maxChains && current != null; b++)
            {
                cachedChainHitIds[hitCount++] = current.ActivationId;
                float applied = current.TakeDamage(damage);
                DamageTracker.RecordDamage(definition.crystalId, applied);

                Vector3 targetPos = current.transform.position + Vector3.up * 0.25f;
                if (VfxManager.Instance != null)
                {
                    VfxManager.Instance.PlayAbilityTrace(sourcePos, targetPos, "electric_engineer", 0.10f);
                    VfxManager.Instance.PlayShockImpact(current.transform.position);
                }

                if (!current.IsDead)
                {
                    current.ApplyStatusEffect(new StatusEffect(StatusEffectType.Shock, 1f, 2f, definition.crystalId));
                }

                sourcePos = targetPos;
                current = FindNextChainTarget(current.transform.position, cachedChainHitIds, hitCount, 5.0f);
            }
        }

        private void ExecuteStoneAttack(Enemy target, Vector3 origin, float damage)
        {
            float applied = target.TakeDamage(damage);
            DamageTracker.RecordDamage(definition.crystalId, applied);

            if (definition.splashRadius > 0f)
            {
                float radiusSqr = definition.splashRadius * definition.splashRadius;
                var enemies = EnemyManager.All;
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Enemy enemy = enemies[i];
                    if (enemy != null && enemy != target && !enemy.IsDead && (enemy.transform.position - target.transform.position).sqrMagnitude <= radiusSqr)
                    {
                        float splashDmg = enemy.TakeDamage(damage * 0.5f);
                        DamageTracker.RecordDamage(definition.crystalId, splashDmg);
                    }
                }
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayAbilityTrace(origin, target.transform.position, "bombardier", 0.20f);
                VfxManager.Instance.PlaySniperImpact(target.transform.position);
            }
        }

        private void ExecuteShadowAttack(Enemy target, Vector3 origin, float damage)
        {
            float applied = target.TakeDamage(damage);
            DamageTracker.RecordDamage(definition.crystalId, applied);

            if (!target.IsDead && definition.damageOverTime > 0f)
            {
                float dotDur = definition.damageOverTimeDuration > 0f ? definition.damageOverTimeDuration : 4f;
                target.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, definition.damageOverTime, dotDur, "crystal_shadow"));
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayAbilityTrace(origin, target.transform.position, "sniper", 0.18f);
            }
        }

        private Enemy FindNextChainTarget(Vector3 sourcePos, int[] hitList, int hitCount, float bounceRange)
        {
            Enemy best = null;
            float bestDistSqr = bounceRange * bounceRange;
            var all = EnemyManager.All;
            for (int i = 0; i < all.Count; i++)
            {
                Enemy enemy = all[i];
                if (enemy == null || enemy.IsDead || !enemy.IsTargetable) continue;

                bool alreadyHit = false;
                for (int j = 0; j < hitCount; j++)
                {
                    if (hitList[j] != 0 && hitList[j] == enemy.ActivationId)
                    {
                        alreadyHit = true;
                        break;
                    }
                }
                if (alreadyHit) continue;

                float distSqr = (enemy.transform.position - sourcePos).sqrMagnitude;
                if (distSqr <= bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    best = enemy;
                }
            }
            return best;
        }
    }
}
