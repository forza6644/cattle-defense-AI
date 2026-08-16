using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Reanimated spectral minion spawned when a poisoned enemy is slain.
    /// Fights for the defense, attacks nearby enemies with poison strikes, and blocks enemies on the lane.
    /// </summary>
    public class SpectralMinion : MonoBehaviour
    {
        private static readonly List<SpectralMinion> activeMinions = new List<SpectralMinion>();
        public static IReadOnlyList<SpectralMinion> ActiveMinions => activeMinions;

        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private float attackInterval = 1.0f;
        [SerializeField] private float stopDistance = 1.2f;

        private float lifetimeRemaining;
        private float maxLifetime;
        private float currentHealth;
        private float maxHealth;
        private float attackDamage;
        private float attackTimer;
        private string sourceHeroId = "plague_doctor";
        private bool isDead;

        private GameObject visualRoot;
        private Renderer meshRenderer;
        private static Material spectralMaterial;

        public bool IsDead => isDead;
        public float CurrentHealth => currentHealth;
        public float LifetimeRemaining => lifetimeRemaining;
        public float StopDistance => stopDistance;

        public static void ClearAll()
        {
            for (int i = activeMinions.Count - 1; i >= 0; i--)
            {
                if (activeMinions[i] != null)
                {
                    Destroy(activeMinions[i].gameObject);
                }
            }
            activeMinions.Clear();
        }

        public static SpectralMinion Spawn(Vector3 position, float damage = 15f, float duration = 10f, float health = 50f, string heroId = "plague_doctor")
        {
            // Limit max active minions for performance
            int maxCap = 6;
            if (RunModifierManager.Instance != null && RunModifierManager.Instance.HasBehavior(heroId, HeroBehaviorEffectType.ArmyOfTheDead))
            {
                maxCap = 12;
            }

            if (activeMinions.Count >= maxCap && activeMinions.Count > 0)
            {
                // Recycle oldest minion
                SpectralMinion oldest = activeMinions[0];
                if (oldest != null)
                {
                    oldest.Despawn();
                }
            }

            GameObject go = new GameObject("SpectralMinion");
            go.transform.position = position;
            SpectralMinion minion = go.AddComponent<SpectralMinion>();
            minion.Initialize(damage, duration, health, heroId);
            return minion;
        }

        private void Initialize(float damage, float duration, float health, string heroId)
        {
            attackDamage = damage;
            maxLifetime = duration;
            lifetimeRemaining = duration;
            maxHealth = health;
            currentHealth = health;
            sourceHeroId = heroId;
            isDead = false;
            attackTimer = 0.2f;

            BuildVisuals();
            activeMinions.Add(this);

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHit(transform.position + Vector3.up * 0.5f, new Color(0.2f, 0.9f, 0.3f));
            }
        }

        private void BuildVisuals()
        {
            visualRoot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visualRoot.name = "MinionBody";
            visualRoot.transform.SetParent(transform, false);
            visualRoot.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            visualRoot.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);

            // Remove collider so it doesn't block physics raycasts incorrectly
            Collider col = visualRoot.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            meshRenderer = visualRoot.GetComponent<Renderer>();
            if (meshRenderer != null)
            {
                if (spectralMaterial == null)
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                    spectralMaterial = new Material(s);
                    spectralMaterial.color = new Color(0.25f, 0.95f, 0.4f, 0.75f);
                }
                meshRenderer.material = spectralMaterial;
            }

            // Head / Glowing Eyes
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "MinionHead";
            head.transform.SetParent(visualRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.7f, 0.1f);
            head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            Collider headCol = head.GetComponent<Collider>();
            if (headCol != null) Destroy(headCol);
        }

        private void Update()
        {
            if (isDead) return;

            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f)
            {
                Despawn();
                return;
            }

            // Subtle spectral bobbing
            if (visualRoot != null)
            {
                float bob = Mathf.Sin(Time.time * 4f) * 0.08f;
                visualRoot.transform.localPosition = new Vector3(0f, 0.6f + bob, 0f);
            }

            // Attack logic
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = attackInterval;
                PerformMinionAttack();
            }
        }

        private void PerformMinionAttack()
        {
            Enemy target = EnemyManager.FindTarget(transform.position, attackRange, TargetingMode.Weakest);
            if (target == null || target.IsDead)
            {
                target = EnemyManager.FindTarget(transform.position, attackRange, TargetingMode.ClosestToGoal);
            }

            if (target != null && !target.IsDead)
            {
                // Face target
                Vector3 dir = target.transform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(dir);
                }

                // Deal attack damage
                float applied = target.TakeDamage(attackDamage, false);
                DamageTracker.RecordDamage(sourceHeroId, applied);

                // Apply poison effect
                StatusEffectController sec = target.GetComponent<StatusEffectController>();
                if (sec == null)
                {
                    sec = target.gameObject.AddComponent<StatusEffectController>();
                }
                sec.ApplyEffect(new StatusEffect(StatusEffectType.Poison, attackDamage * 0.4f, 4f, sourceHeroId));

                if (VfxManager.Instance != null)
                {
                    VfxManager.Instance.PlayHit(target.transform.position + Vector3.up * 0.5f, new Color(0.2f, 0.9f, 0.3f));
                }
            }
        }

        public float TakeDamage(float amount)
        {
            if (isDead || amount <= 0f) return 0f;

            currentHealth -= amount;
            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHit(transform.position + Vector3.up * 0.5f, new Color(0.1f, 0.6f, 0.2f));
            }

            if (currentHealth <= 0f)
            {
                Despawn();
            }
            return amount;
        }

        public void Despawn()
        {
            if (isDead) return;
            isDead = true;
            activeMinions.Remove(this);

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHit(transform.position + Vector3.up * 0.5f, new Color(0.1f, 0.8f, 0.3f));
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            activeMinions.Remove(this);
        }
    }
}
