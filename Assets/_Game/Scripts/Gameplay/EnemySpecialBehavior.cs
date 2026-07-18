using UnityEngine;

namespace Stonehold
{
    /// <summary>Optional pooled behavior for expansion enemies. It owns no run-global state.</summary>
    public sealed class EnemySpecialBehavior : MonoBehaviour
    {
        private const int MaximumHealTargets = 6;

        private readonly Enemy[] healTargets = new Enemy[MaximumHealTargets];
        private Enemy enemy;
        private Castle castle;
        private int activationId;
        private float actionTimer;
        private float pulseVisualTimer;
        private bool isWindingUp;
        private bool isCasting;
        private GameObject windUpIndicator;
        private LineRenderer eliteRing;
        private LineRenderer pulseRing;
        private static Material indicatorMaterial;

        public bool IsWindingUp => isWindingUp;
        public bool IsCasting => isCasting;
        public int LastHealCount { get; private set; }
        public int BoundActivationId => activationId;

        public void PrepareForSpawn(Enemy owner)
        {
            enemy = owner;
            EnsureVisuals();
            ResetForReuse();
        }

        public void Activate(Castle targetCastle, int expectedActivationId)
        {
            castle = targetCastle;
            activationId = expectedActivationId;
            EnemyData data = enemy != null ? enemy.Data : null;
            actionTimer = data != null && data.specialRole == EnemySpecialRole.HealingElite
                ? data.healingPulse.intervalSeconds
                : 0f;
            if (eliteRing != null)
            {
                eliteRing.gameObject.SetActive(data != null && data.classification == EnemyClassification.Elite);
            }
        }

        /// <returns>True when the special behavior intentionally blocks movement.</returns>
        public bool Tick()
        {
            if (enemy == null || !enemy.MatchesActivation(activationId) || enemy.IsDead)
            {
                CancelPendingActions();
                return false;
            }

            UpdatePulseVisual();
            EnemyData data = enemy.Data;
            if (data == null) return false;

            switch (data.specialRole)
            {
                case EnemySpecialRole.RangedCastleAttacker:
                    return TickRanged(data.rangedAttack);
                case EnemySpecialRole.HealingElite:
                    return TickHealing(data.healingPulse);
                default:
                    return false;
            }
        }

        public void CancelPendingActions()
        {
            isWindingUp = false;
            isCasting = false;
            actionTimer = 0f;
            SetWindUpVisible(false, 0f, Color.clear);
        }

        public void ResetForReuse()
        {
            CancelPendingActions();
            castle = null;
            activationId = 0;
            LastHealCount = 0;
            pulseVisualTimer = 0f;
            for (int i = 0; i < healTargets.Length; i++) healTargets[i] = null;
            if (eliteRing != null) eliteRing.gameObject.SetActive(false);
            if (pulseRing != null) pulseRing.gameObject.SetActive(false);
        }

        private bool TickRanged(EnemyRangedAttackSettings settings)
        {
            if (castle == null || castle.IsGameOver)
            {
                CancelPendingActions();
                return false;
            }

            Face(castle.transform.position);
            if (isWindingUp)
            {
                actionTimer -= Time.deltaTime;
                SetWindUpVisible(true, 1f - Mathf.Clamp01(actionTimer / Mathf.Max(0.01f, settings.windUpSeconds)), new Color(1f, 0.55f, 0.08f));
                if (actionTimer <= 0f)
                {
                    FireAtCastle(settings);
                    isWindingUp = false;
                    actionTimer = settings.cooldownSeconds;
                    SetWindUpVisible(false, 0f, Color.clear);
                }
                return true;
            }

            if (actionTimer > 0f) actionTimer -= Time.deltaTime;
            float distance = Vector3.Distance(transform.position, castle.transform.position);
            float nextMovementStep = enemy.Data.moveSpeed * enemy.SlowMultiplier * Time.deltaTime;
            if (distance > settings.standOffRange && distance - nextMovementStep > settings.standOffRange) return false;
            if (distance > settings.standOffRange)
            {
                Vector3 awayFromCastle = transform.position - castle.transform.position;
                awayFromCastle.y = 0f;
                if (awayFromCastle.sqrMagnitude > 0.001f)
                {
                    transform.position = castle.transform.position + awayFromCastle.normalized * settings.standOffRange;
                }
            }

            if (actionTimer <= 0f)
            {
                isWindingUp = true;
                actionTimer = settings.windUpSeconds;
                SetWindUpVisible(true, 0f, new Color(1f, 0.55f, 0.08f));
            }
            return true;
        }

        private bool TickHealing(EnemyHealingPulseSettings settings)
        {
            if (isCasting)
            {
                actionTimer -= Time.deltaTime;
                SetWindUpVisible(true, 1f - Mathf.Clamp01(actionTimer / Mathf.Max(0.01f, settings.castSeconds)), new Color(0.25f, 1f, 0.45f));
                if (actionTimer <= 0f)
                {
                    ExecuteHealingPulse(settings);
                    isCasting = false;
                    actionTimer = settings.intervalSeconds;
                    SetWindUpVisible(false, 0f, Color.clear);
                }
                return true;
            }

            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0f)
            {
                isCasting = true;
                actionTimer = settings.castSeconds;
                SetWindUpVisible(true, 0f, new Color(0.25f, 1f, 0.45f));
                return true;
            }
            return false;
        }

        private void FireAtCastle(EnemyRangedAttackSettings settings)
        {
            if (settings.projectilePrefab == null || castle == null || !enemy.MatchesActivation(activationId)) return;
            EnemyCastleProjectile projectile = EnemyCastleProjectile.Spawn(settings.projectilePrefab, transform.position + Vector3.up * 0.9f);
            projectile?.Initialize(enemy, activationId, castle, enemy.Data.castleDamage, settings.projectileSpeed);
        }

        private void ExecuteHealingPulse(EnemyHealingPulseSettings settings)
        {
            LastHealCount = 0;
            int count = CollectHealTargets(settings);
            for (int i = 0; i < count; i++)
            {
                Enemy target = healTargets[i];
                if (!IsValidHealTarget(target, settings)) continue;
                float amount = target.MaxHealth * settings.maxHealthFraction;
                if (target == enemy) amount *= settings.selfHealMultiplier;
                if (target.RestoreHealth(amount) > 0f) LastHealCount++;
            }

            pulseVisualTimer = 0.45f;
            if (pulseRing != null)
            {
                pulseRing.gameObject.SetActive(true);
                pulseRing.startColor = new Color(0.25f, 1f, 0.45f, 0.9f);
                pulseRing.endColor = pulseRing.startColor;
                pulseRing.transform.localScale = Vector3.one * 0.1f;
            }
        }

        private int CollectHealTargets(EnemyHealingPulseSettings settings)
        {
            int count = 0;
            int cap = Mathf.Min(settings.targetCap, MaximumHealTargets);
            var all = EnemyManager.All;
            for (int i = 0; i < all.Count; i++)
            {
                Enemy candidate = all[i];
                if (!IsValidHealTarget(candidate, settings)) continue;
                float ratio = candidate.CurrentHealth / Mathf.Max(1f, candidate.MaxHealth);
                int insert = count;
                while (insert > 0)
                {
                    Enemy previous = healTargets[insert - 1];
                    float previousRatio = previous.CurrentHealth / Mathf.Max(1f, previous.MaxHealth);
                    if (previousRatio <= ratio) break;
                    if (insert < cap) healTargets[insert] = previous;
                    insert--;
                }
                if (insert < cap) healTargets[insert] = candidate;
                if (count < cap) count++;
            }
            return count;
        }

        private bool IsValidHealTarget(Enemy candidate, EnemyHealingPulseSettings settings)
        {
            if (candidate == null || candidate.IsDead || !candidate.IsTargetable || candidate.CurrentHealth >= candidate.MaxHealth) return false;
            if (settings.excludeBoss && candidate.Data != null && candidate.Data.classification == EnemyClassification.Boss) return false;
            return (candidate.transform.position - transform.position).sqrMagnitude <= settings.radius * settings.radius;
        }

        private void UpdatePulseVisual()
        {
            if (pulseVisualTimer <= 0f || pulseRing == null) return;
            pulseVisualTimer -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(pulseVisualTimer / 0.45f);
            pulseRing.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, enemy.Data.healingPulse.radius, progress);
            if (pulseVisualTimer <= 0f) pulseRing.gameObject.SetActive(false);
        }

        private void Face(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.deltaTime);
            }
        }

        private void EnsureVisuals()
        {
            if (windUpIndicator == null)
            {
                windUpIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                windUpIndicator.name = "SpecialWindUp";
                windUpIndicator.transform.SetParent(transform, false);
                windUpIndicator.transform.localPosition = Vector3.up * 1.8f;
                Destroy(windUpIndicator.GetComponent<Collider>());
                windUpIndicator.GetComponent<Renderer>().sharedMaterial = GetIndicatorMaterial();
                windUpIndicator.SetActive(false);
            }
            if (eliteRing == null) eliteRing = CreateRing("EliteIndicator", 1.05f, new Color(0.7f, 0.15f, 1f, 0.95f));
            if (pulseRing == null) pulseRing = CreateRing("HealingPulse", 1f, new Color(0.25f, 1f, 0.45f, 0.9f));
            eliteRing.gameObject.SetActive(false);
            pulseRing.gameObject.SetActive(false);
        }

        private LineRenderer CreateRing(string objectName, float radius, Color color)
        {
            GameObject ringObject = new GameObject(objectName);
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = Vector3.up * 0.05f;
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 32;
            ring.widthMultiplier = 0.08f;
            ring.sharedMaterial = GetIndicatorMaterial();
            ring.startColor = color;
            ring.endColor = color;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            return ring;
        }

        private void SetWindUpVisible(bool visible, float progress, Color color)
        {
            if (windUpIndicator == null) return;
            windUpIndicator.SetActive(visible);
            if (!visible) return;
            float scale = Mathf.Lerp(0.18f, 0.42f, progress);
            windUpIndicator.transform.localScale = Vector3.one * scale;
            windUpIndicator.GetComponent<Renderer>().sharedMaterial.color = color;
        }

        private static Material GetIndicatorMaterial()
        {
            if (indicatorMaterial != null) return indicatorMaterial;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            indicatorMaterial = new Material(shader) { name = "EnemyExpansionIndicator_Runtime" };
            return indicatorMaterial;
        }

#if UNITY_EDITOR
        public void ForceActionReadyForTests() => actionTimer = 0f;
        public void CompleteActionForTests()
        {
            if (isWindingUp || isCasting) actionTimer = 0f;
        }
#endif
    }
}
