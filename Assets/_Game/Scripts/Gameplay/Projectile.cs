using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// A shot fired by a tower. Flies straight at its target and, on contact, deals
    /// its damage (splash and slow included) and asks the VfxManager/AudioManager for
    /// the right impact effect. Pooled per prefab (Spawn/Return) so firing never
    /// allocates or destroys at runtime.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float hitDistance = 0.3f;

        private Enemy target;
        private int targetActivationId;
        private float damage;
        private float splashRadius;
        private float slowMultiplier = 1f;
        private float slowDuration;
        private string sourceHeroId;
        private TrailRenderer trail;
        private Renderer projectileRenderer;
        private Transform projectileVisual;
        private Renderer projectileVisualRenderer;
        private GameObject sourcePrefab;
        private Color impactColor = Color.white;
        private bool isCrit;
        public bool IsAbility { get; set; }

        private StatusEffectType statusEffectType = StatusEffectType.None;
        private float statusEffectValue;
        private float statusEffectDuration;

        private Vector3 baseScale;
        private Vector3 targetLastPosition;
        private Vector3 startPosition;
        private float travelTime;
        private float elapsedTravelTime;
        private bool useArc;

        // Behavior upgrades fields (pierce)
        private int maxPierces;
        private int currentPierces;
        private int additionalPierces;
        private bool hitPrimaryTarget;
        private float distanceTraveled;
        private Vector3 moveDirection;
        private float secondaryValue;
        private bool isSecondaryCluster;
        private readonly int[] hitEnemyActivationIds = new int[4];

        private static readonly Dictionary<GameObject, Queue<Projectile>> pools = new Dictionary<GameObject, Queue<Projectile>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            pools.Clear();
        }

        private void Awake()
        {
            baseScale = transform.localScale;
            trail = GetComponent<TrailRenderer>();
            projectileRenderer = GetComponent<Renderer>();
            CreateProjectileVisual();
        }

        /// <summary>Gets a pooled projectile (or instantiates one) at the position.</summary>
        public static Projectile Spawn(GameObject prefab, Vector3 position)
        {
            Projectile projectile = null;
            if (pools.TryGetValue(prefab, out Queue<Projectile> pool) && pool.Count > 0)
            {
                projectile = pool.Dequeue();
            }

            if (projectile == null)
            {
                GameObject go = Instantiate(prefab, position, Quaternion.identity);
                projectile = go.GetComponent<Projectile>();
                projectile.sourcePrefab = prefab;
                projectile.baseScale = go.transform.localScale;
                go.SetActive(true);
            }
            else
            {
                projectile.transform.position = position;
                projectile.transform.rotation = Quaternion.identity;
                projectile.transform.localScale = projectile.baseScale;
                projectile.gameObject.SetActive(true);
            }

            projectile.IsAbility = false;
            return projectile;
        }

        /// <summary>Called by the tower right after this projectile is spawned.</summary>
        public void Init(Enemy targetEnemy, float damageAmount, float splash, float slowMult, float slowDur, Color trailColor)
        {
            Init(targetEnemy, damageAmount, splash, slowMult, slowDur, trailColor, null, false);
        }

        public void Init(Enemy targetEnemy, float damageAmount, float splash, float slowMult, float slowDur, Color trailColor, string damageSourceHeroId, bool isCritical = false)
        {
            speed = (damageSourceHeroId == "sniper") ? 60f : 12f;
            target = targetEnemy;
            targetActivationId = targetEnemy != null ? targetEnemy.ActivationId : 0;
            damage = damageAmount;
            splashRadius = splash;
            slowMultiplier = slowMult;
            slowDuration = slowDur;
            sourceHeroId = damageSourceHeroId;
            impactColor = trailColor;
            isCrit = isCritical;
            ResetBehaviorState();

            targetLastPosition = target != null ? GetTargetPosition(target) : transform.position;
            startPosition = transform.position;
            OrientPresentation(targetLastPosition - startPosition);
            useArc = (damageSourceHeroId == "bombardier");
            if (useArc)
            {
                float distance = Vector3.Distance(startPosition, targetLastPosition);
                travelTime = Mathf.Max(0.1f, distance / speed);
                elapsedTravelTime = 0f;
            }

            if (slowMult < 1f)
            {
                statusEffectType = StatusEffectType.Slow;
                statusEffectValue = slowMult;
                statusEffectDuration = slowDur;
            }
            else
            {
                statusEffectType = StatusEffectType.None;
                statusEffectValue = 0f;
                statusEffectDuration = 0f;
            }

            if (trail == null)
            {
                trail = GetComponent<TrailRenderer>();
            }

            ConfigureTrailPresentation(trailColor);
        }

        public void InitWithStatusEffect(
            Enemy targetEnemy,
            float damageAmount,
            float splash,
            Color trailColor,
            string damageSourceHeroId,
            StatusEffectType effectType,
            float effectValue,
            float effectDuration,
            bool isCritical = false)
        {
            speed = (damageSourceHeroId == "sniper") ? 60f : 12f;
            target = targetEnemy;
            targetActivationId = targetEnemy != null ? targetEnemy.ActivationId : 0;
            damage = damageAmount;
            splashRadius = splash;
            sourceHeroId = damageSourceHeroId;
            statusEffectType = effectType;
            statusEffectValue = effectValue;
            statusEffectDuration = effectDuration;
            impactColor = trailColor;
            isCrit = isCritical;
            ResetBehaviorState();

            targetLastPosition = target != null ? target.transform.position : transform.position;
            startPosition = transform.position;
            OrientPresentation(targetLastPosition - startPosition);
            useArc = (damageSourceHeroId == "bombardier");
            if (useArc)
            {
                float distance = Vector3.Distance(startPosition, targetLastPosition);
                travelTime = Mathf.Max(0.1f, distance / speed);
                elapsedTravelTime = 0f;
            }

            if (effectType == StatusEffectType.Slow)
            {
                slowMultiplier = effectValue;
                slowDuration = effectDuration;
            }
            else
            {
                slowMultiplier = 1f;
                slowDuration = 0f;
            }

            if (trail == null)
            {
                trail = GetComponent<TrailRenderer>();
            }

            ConfigureTrailPresentation(trailColor);
        }

        public void ConfigurePiercing(int additionalTargets, Vector3 direction, float damageReductionPerPierce)
        {
            maxPierces = Mathf.Clamp(additionalTargets, 0, hitEnemyActivationIds.Length - 1);
            moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            secondaryValue = Mathf.Clamp01(damageReductionPerPierce);
        }

        private void Update()
        {
            if (maxPierces > 0)
            {
                Vector3 lastPos = transform.position;
                transform.position += moveDirection * speed * Time.deltaTime;
                distanceTraveled += Vector3.Distance(lastPos, transform.position);

                var allEnemies = EnemyManager.All;
                for (int i = 0; i < allEnemies.Count; i++)
                {
                    Enemy enemy = allEnemies[i];
                    if (enemy == null || enemy.IsDead || !enemy.IsTargetable) continue;

                    bool alreadyHit = false;
                    for (int j = 0; j < currentPierces; j++)
                    {
                        if (hitEnemyActivationIds[j] == enemy.ActivationId)
                        {
                            alreadyHit = true;
                            break;
                        }
                    }
                    if (alreadyHit) continue;

                    Vector3 planarOffset = transform.position - enemy.transform.position;
                    planarOffset.y = 0f;
                    if (planarOffset.sqrMagnitude <= 0.6f * 0.6f)
                    {
                        bool isPrimaryTarget = enemy.MatchesActivation(targetActivationId);
                        if (!isPrimaryTarget && additionalPierces >= maxPierces)
                        {
                            continue;
                        }

                        float pierceDamage = isPrimaryTarget ? damage : damage * Mathf.Max(0.1f, 1f - secondaryValue);
                        if (!isPrimaryTarget)
                        {
                            additionalPierces++;
                        }
                        else
                        {
                            hitPrimaryTarget = true;
                        }

                        float appliedDamage = enemy.TakeDamage(pierceDamage, false, isCrit, sourceHeroId);
                        DamageTracker.RecordDamage(sourceHeroId, appliedDamage);

                        if (statusEffectType != StatusEffectType.None && statusEffectDuration > 0f && !enemy.IsDead)
                        {
                            enemy.ApplyStatusEffect(new StatusEffect(statusEffectType, statusEffectValue, statusEffectDuration, sourceHeroId));
                        }

                        hitEnemyActivationIds[currentPierces] = enemy.ActivationId;
                        currentPierces++;

                        if (VfxManager.Instance != null)
                        {
                            VfxManager.Instance.PlayHit(enemy.transform.position, impactColor);
                        }
                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlayHeroImpact(sourceHeroId, isCrit || IsAbility);
                        }

                        if (hitPrimaryTarget && additionalPierces >= maxPierces)
                        {
                            Return();
                            return;
                        }
                    }
                }

                if (distanceTraveled >= 20f)
                {
                    Return();
                    return;
                }
            }
            else
            {
                if (IsCurrentTargetValid())
                {
                    targetLastPosition = GetTargetPosition(target);
                }

                Vector3 dest = targetLastPosition;

                if (useArc)
                {
                    elapsedTravelTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTravelTime / travelTime);

                    Vector3 currentPos = Vector3.Lerp(startPosition, dest, t);
                    float arcHeight = 2.4f;
                    float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
                    currentPos.y += height;

                    OrientPresentation(currentPos - transform.position);

                    transform.position = currentPos;

                    if (t >= 1.0f)
                    {
                        Impact(dest);
                        Return();
                    }
                }
                else
                {
                    OrientPresentation(dest - transform.position);
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        dest,
                        speed * Time.deltaTime);

                    if (Vector3.Distance(transform.position, dest) <= hitDistance)
                    {
                        Impact(dest);
                        Return();
                    }
                }
            }
        }

        private void Impact(Vector3 impactPoint)
        {
            if (VfxManager.Instance != null)
            {
                if (!string.IsNullOrEmpty(sourceHeroId))
                {
                    VfxManager.Instance.PlayHeroProjectileImpact(impactPoint, sourceHeroId, isCrit);
                }
                else if (isCrit)
                {
                    VfxManager.Instance.PlayCriticalImpact(impactPoint);
                }
                else if (splashRadius > 0f)
                {
                    VfxManager.Instance.PlayExplosion(impactPoint, IsAbility);
                }
                else if (statusEffectType == StatusEffectType.Slow)
                {
                    VfxManager.Instance.PlayFrost(impactPoint);
                }
                else if (statusEffectType == StatusEffectType.Burn)
                {
                    VfxManager.Instance.PlayFireImpact(impactPoint, IsAbility);
                }
                else if (statusEffectType == StatusEffectType.Shock)
                {
                    VfxManager.Instance.PlayShockImpact(impactPoint);
                }
                else if (sourceHeroId == "sniper")
                {
                    VfxManager.Instance.PlaySniperImpact(impactPoint);
                }
                else
                {
                    VfxManager.Instance.PlayHit(impactPoint, impactColor);
                }
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHeroImpact(sourceHeroId, isCrit || IsAbility);
            }

            if (splashRadius > 0f)
            {
                var all = EnemyManager.All;
                for (int i = all.Count - 1; i >= 0; i--)
                {
                    if (i >= all.Count)
                    {
                        continue;
                    }

                    Enemy enemy = all[i];
                    if (enemy != null && Vector3.Distance(impactPoint, enemy.transform.position) <= splashRadius)
                    {
                        HitEnemy(enemy);
                    }
                }

                // SplitProjectile behavior for Bombardier
                if (sourceHeroId == "bombardier" && !IsAbility && !isSecondaryCluster && RunModifierManager.Instance != null && RunModifierManager.Instance.HasBehavior("bombardier", HeroBehaviorEffectType.SplitProjectile))
                {
                    int stacks = RunModifierManager.Instance.GetBehaviorStacks("bombardier", HeroBehaviorEffectType.SplitProjectile);
                    int clusterCount = 1 + stacks; // e.g. 2 for stack 1, 3 for stack 2
                    float reducedDamage = damage * 0.4f;
                    float reducedRadius = splashRadius * 0.6f;

                    for (int c = 0; c < clusterCount; c++)
                    {
                        float angle = (360f / clusterCount) * c * Mathf.Deg2Rad;
                        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.5f;

                        Projectile sub = Spawn(sourcePrefab, impactPoint);
                        if (sub != null)
                        {
                            sub.InitSecondaryCluster(impactPoint + offset, reducedDamage, reducedRadius, impactColor, sourceHeroId);
                        }
                    }
                }
            }
            else
            {
                if (IsCurrentTargetValid())
                {
                    HitEnemy(target);
                }
            }
        }

        private void ConfigureTrailPresentation(Color fallbackColor, float intensity = 1f)
        {
            if (trail == null)
            {
                trail = GetComponent<TrailRenderer>();
            }

            Color color = GetProjectilePresentationColor(fallbackColor);
            if (trail != null)
            {
                GetTrailProfile(out float startWidth, out float endWidth, out float lifetime);
                Color start = color;
                start.a = 0.84f;
                Color end = color;
                end.a = 0f;

                trail.Clear();
                trail.emitting = true;
                trail.startColor = start;
                trail.endColor = end;
                trail.startWidth = startWidth * intensity;
                trail.endWidth = endWidth * intensity;
                trail.time = lifetime;
            }

            if (projectileRenderer == null)
            {
                projectileRenderer = GetComponent<Renderer>();
            }
            ConfigureProjectileVisual(color);
        }

        private void CreateProjectileVisual()
        {
            if (projectileVisual != null)
            {
                return;
            }

            GameObject visual = new GameObject("ProjectileVisual");
            visual.name = "ProjectileVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            MeshFilter sourceMeshFilter = GetComponent<MeshFilter>();
            MeshFilter visualMeshFilter = visual.AddComponent<MeshFilter>();
            if (sourceMeshFilter != null)
            {
                visualMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;
            }

            projectileVisual = visual.transform;
            projectileVisualRenderer = visual.AddComponent<MeshRenderer>();
            if (projectileVisualRenderer != null && projectileRenderer != null)
            {
                projectileVisualRenderer.sharedMaterial = projectileRenderer.sharedMaterial;
            }
            visual.SetActive(false);
        }

        private void ConfigureProjectileVisual(Color color)
        {
            CreateProjectileVisual();
            bool isHeroProjectile = IsHeroPresentationId(sourceHeroId);
            if (!isHeroProjectile)
            {
                if (projectileRenderer != null) projectileRenderer.enabled = true;
                if (projectileVisual != null) projectileVisual.gameObject.SetActive(false);
                return;
            }

            if (projectileVisual == null)
            {
                return;
            }

            if (projectileRenderer != null) projectileRenderer.enabled = false;
            projectileVisual.gameObject.SetActive(true);

            Vector3 visualScale;
            Quaternion visualRotation = Quaternion.identity;
            switch (sourceHeroId)
            {
                case "archer":
                    visualScale = new Vector3(0.055f, 0.055f, 0.34f);
                    break;
                case "bombardier":
                    visualScale = new Vector3(0.21f, 0.21f, 0.30f);
                    break;
                case "frost_mage":
                    visualScale = new Vector3(0.13f, 0.23f, 0.13f);
                    visualRotation = Quaternion.Euler(45f, 45f, 45f);
                    break;
                case "fire_mage":
                    visualScale = Vector3.one * 0.22f;
                    break;
                case "sniper":
                    visualScale = new Vector3(0.035f, 0.035f, 0.20f);
                    break;
                default:
                    visualScale = Vector3.one * 0.18f;
                    break;
            }

            projectileVisual.localScale = visualScale;
            projectileVisual.localRotation = visualRotation;
            if (projectileVisualRenderer != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                projectileVisualRenderer.GetPropertyBlock(block);
                block.SetColor(Shader.PropertyToID("_BaseColor"), color);
                projectileVisualRenderer.SetPropertyBlock(block);
            }
        }

        private void OrientPresentation(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private static bool IsHeroPresentationId(string heroId)
        {
            switch (heroId)
            {
                case "archer":
                case "bombardier":
                case "frost_mage":
                case "fire_mage":
                case "sniper":
                    return true;
                default:
                    return false;
            }
        }

        private void GetTrailProfile(out float startWidth, out float endWidth, out float lifetime)
        {
            switch (sourceHeroId)
            {
                case "archer":
                    startWidth = 0.09f;
                    endWidth = 0.014f;
                    lifetime = 0.14f;
                    break;
                case "bombardier":
                    startWidth = 0.24f;
                    endWidth = 0.045f;
                    lifetime = 0.20f;
                    break;
                case "frost_mage":
                    startWidth = 0.14f;
                    endWidth = 0.024f;
                    lifetime = 0.17f;
                    break;
                case "fire_mage":
                    startWidth = 0.17f;
                    endWidth = 0.030f;
                    lifetime = 0.18f;
                    break;
                case "sniper":
                    startWidth = 0.055f;
                    endWidth = 0.010f;
                    lifetime = 0.075f;
                    break;
                default:
                    startWidth = splashRadius > 0f ? 0.28f : 0.12f;
                    endWidth = 0.025f;
                    lifetime = 0.18f;
                    break;
            }
        }

        private Color GetProjectilePresentationColor(Color fallbackColor)
        {
            switch (sourceHeroId)
            {
                case "archer": return new Color(0.58f, 0.78f, 0.28f, 1f);
                case "bombardier": return new Color(1f, 0.48f, 0.10f, 1f);
                case "frost_mage": return new Color(0.28f, 0.86f, 1f, 1f);
                case "fire_mage": return new Color(1f, 0.25f, 0.05f, 1f);
                case "sniper": return new Color(0.72f, 0.60f, 0.96f, 1f);
                default: return fallbackColor;
            }
        }

        private void HitEnemy(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead) return;
            float appliedDamage = enemy.TakeDamage(damage, false, isCrit, sourceHeroId);
            DamageTracker.RecordDamage(sourceHeroId, appliedDamage);

            if (statusEffectType != StatusEffectType.None && statusEffectDuration > 0f)
            {
                enemy.ApplyStatusEffect(new StatusEffect(statusEffectType, statusEffectValue, statusEffectDuration, sourceHeroId));
            }
        }

        public void InitSecondaryCluster(Vector3 targetPos, float damageAmount, float splash, Color trailColor, string damageSourceHeroId)
        {
            speed = 8f;
            target = null;
            targetActivationId = 0;
            damage = damageAmount;
            splashRadius = splash;
            sourceHeroId = damageSourceHeroId;
            impactColor = trailColor;
            isCrit = false;
            IsAbility = false;
            isSecondaryCluster = true;

            targetLastPosition = targetPos;
            startPosition = transform.position;
            OrientPresentation(targetLastPosition - startPosition);
            useArc = true;
            float distance = Vector3.Distance(startPosition, targetLastPosition);
            travelTime = Mathf.Max(0.1f, distance / speed);
            elapsedTravelTime = 0f;

            statusEffectType = StatusEffectType.None;
            statusEffectValue = 0f;
            statusEffectDuration = 0f;

            maxPierces = 0;
            currentPierces = 0;
            additionalPierces = 0;
            hitPrimaryTarget = false;
            distanceTraveled = 0f;
            secondaryValue = 0f;
            for (int i = 0; i < hitEnemyActivationIds.Length; i++)
            {
                hitEnemyActivationIds[i] = 0;
            }

            ConfigureTrailPresentation(trailColor, 0.65f);
        }

        private void ResetBehaviorState()
        {
            maxPierces = 0;
            currentPierces = 0;
            additionalPierces = 0;
            hitPrimaryTarget = false;
            distanceTraveled = 0f;
            moveDirection = Vector3.zero;
            secondaryValue = 0f;
            isSecondaryCluster = false;
            for (int i = 0; i < hitEnemyActivationIds.Length; i++)
            {
                hitEnemyActivationIds[i] = 0;
            }
        }

        private void Return()
        {
            target = null;
            targetActivationId = 0;
            sourceHeroId = null;
            maxPierces = 0;
            currentPierces = 0;
            additionalPierces = 0;
            hitPrimaryTarget = false;
            distanceTraveled = 0f;
            secondaryValue = 0f;
            isSecondaryCluster = false;
            for (int i = 0; i < hitEnemyActivationIds.Length; i++)
            {
                hitEnemyActivationIds[i] = 0;
            }

            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }

            gameObject.SetActive(false);

            if (sourcePrefab == null)
            {
                Destroy(gameObject);
                return;
            }

            if (!pools.TryGetValue(sourcePrefab, out Queue<Projectile> pool))
            {
                pool = new Queue<Projectile>();
                pools[sourcePrefab] = pool;
            }

            pool.Enqueue(this);
        }

        private Vector3 GetTargetPosition(Enemy targetEnemy)
        {
            if (targetEnemy == null) return targetLastPosition;
            ArtAdapter adapter = targetEnemy.GetComponent<ArtAdapter>();
            if (adapter != null && adapter.impactPoint != null)
            {
                return adapter.impactPoint.position;
            }
            return targetEnemy.transform.position;
        }

        private bool IsCurrentTargetValid()
        {
            return target != null
                && target.MatchesActivation(targetActivationId)
                && target.gameObject.activeInHierarchy
                && !target.IsDead;
        }
    }
}
