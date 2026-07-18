using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>Pooled enemy projectile protected by the shooter's activation token.</summary>
    public sealed class EnemyCastleProjectile : MonoBehaviour
    {
        private static readonly Dictionary<GameObject, Queue<EnemyCastleProjectile>> pools = new Dictionary<GameObject, Queue<EnemyCastleProjectile>>();
        private static readonly HashSet<EnemyCastleProjectile> active = new HashSet<EnemyCastleProjectile>();
        private static readonly List<EnemyCastleProjectile> cleanupBuffer = new List<EnemyCastleProjectile>(32);
        private static int createdCount;
        private static int reuseCount;
        private static int staleHitCount;
        private static int staleCancellationCount;

        private GameObject sourcePrefab;
        private Enemy source;
        private Castle target;
        private int sourceActivationId;
        private int damage;
        private float speed;
        private float lifetime;
        private bool hasHit;
        private TrailRenderer trail;

        public static int ActiveCount => active.Count;
        public static int CreatedCount => createdCount;
        public static int ReuseCount => reuseCount;
        public static int StaleHitCount => staleHitCount;
        public static int StaleCancellationCount => staleCancellationCount;
        public bool HasHit => hasHit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            pools.Clear();
            active.Clear();
            cleanupBuffer.Clear();
            createdCount = 0;
            reuseCount = 0;
            staleHitCount = 0;
            staleCancellationCount = 0;
        }

        public static EnemyCastleProjectile Spawn(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return null;
            if (!pools.TryGetValue(prefab, out Queue<EnemyCastleProjectile> pool))
            {
                pool = new Queue<EnemyCastleProjectile>();
                pools.Add(prefab, pool);
            }

            EnemyCastleProjectile projectile = pool.Count > 0 ? pool.Dequeue() : null;
            if (projectile == null)
            {
                GameObject instance = Instantiate(prefab, position, Quaternion.identity);
                projectile = instance.GetComponent<EnemyCastleProjectile>();
                if (projectile == null) projectile = instance.AddComponent<EnemyCastleProjectile>();
                projectile.sourcePrefab = prefab;
                createdCount++;
            }
            else
            {
                reuseCount++;
                projectile.transform.position = position;
                projectile.gameObject.SetActive(true);
            }
            active.Add(projectile);
            return projectile;
        }

        public static void DespawnAllActive()
        {
            cleanupBuffer.Clear();
            foreach (EnemyCastleProjectile projectile in active) cleanupBuffer.Add(projectile);
            for (int i = 0; i < cleanupBuffer.Count; i++) cleanupBuffer[i]?.ReturnToPool();
            cleanupBuffer.Clear();
        }

        private void Awake()
        {
            trail = GetComponent<TrailRenderer>();
        }

        public void Initialize(Enemy shooter, int expectedActivationId, Castle castle, int castleDamage, float projectileSpeed)
        {
            source = shooter;
            sourceActivationId = expectedActivationId;
            target = castle;
            damage = castleDamage;
            speed = projectileSpeed;
            lifetime = 8f;
            hasHit = false;
            if (trail == null) trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f || target == null || target.IsGameOver)
            {
                ReturnToPool();
                return;
            }
            if (source == null || !source.MatchesActivation(sourceActivationId))
            {
                staleCancellationCount++;
                ReturnToPool();
                return;
            }

            Vector3 destination = target.transform.position + Vector3.up * 0.8f;
            Vector3 direction = destination - transform.position;
            if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            if ((transform.position - destination).sqrMagnitude <= 0.08f * 0.08f) Impact();
        }

        private void Impact()
        {
            if (!hasHit && source != null && source.MatchesActivation(sourceActivationId) && target != null && !target.IsGameOver)
            {
                hasHit = true;
                target.TakeDamage(damage);
                VfxManager.Instance?.PlayHit(target.transform.position + Vector3.up * 0.8f, new Color(1f, 0.35f, 0.08f));
            }
            ReturnToPool();
        }

        public void ReturnToPool()
        {
            if (!active.Remove(this)) return;
            source = null;
            target = null;
            sourceActivationId = 0;
            damage = 0;
            speed = 0f;
            lifetime = 0f;
            hasHit = false;
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
            if (!pools.TryGetValue(sourcePrefab, out Queue<EnemyCastleProjectile> pool))
            {
                pool = new Queue<EnemyCastleProjectile>();
                pools[sourcePrefab] = pool;
            }
            pool.Enqueue(this);
        }
    }
}
