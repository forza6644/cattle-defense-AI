using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Cached lifecycle state for a pooled particle hierarchy. It prevents stale
    /// delayed returns from reclaiming a reused effect and clears all visual state.
    /// </summary>
    public sealed class PooledParticleState : MonoBehaviour
    {
        private ParticleSystem[] particleSystems;
        private TrailRenderer[] trailRenderers;
        private Vector3 defaultLocalScale;
        private int activationId;
        private bool isInPool;

        public int ActivationId => activationId;
        public bool IsInPool => isInPool;

        public static PooledParticleState GetOrCreate(ParticleSystem root)
        {
            PooledParticleState state = root.GetComponent<PooledParticleState>();
            if (state == null)
            {
                state = root.gameObject.AddComponent<PooledParticleState>();
            }
            state.EnsureCached();
            return state;
        }

        private void Awake()
        {
            EnsureCached();
        }

        private void EnsureCached()
        {
            if (particleSystems != null)
            {
                return;
            }

            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
            defaultLocalScale = transform.localScale;
        }

        public int PrepareForPlay(bool useUnscaledTime)
        {
            EnsureCached();
            activationId++;
            isInPool = false;

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem ps = particleSystems[i];
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear(true);
                ParticleSystem.MainModule main = ps.main;
                main.useUnscaledTime = useUnscaledTime;
            }

            for (int i = 0; i < trailRenderers.Length; i++)
            {
                trailRenderers[i]?.Clear();
            }

            return activationId;
        }

        public bool TryReturn(int expectedActivationId, Transform poolParent)
        {
            if (expectedActivationId != activationId || isInPool)
            {
                return false;
            }

            return TryReturnCurrent(poolParent);
        }

        public bool TryReturnCurrent(Transform poolParent)
        {
            if (isInPool)
            {
                return false;
            }

            EnsureCached();
            isInPool = true;

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem ps = particleSystems[i];
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear(true);
                ParticleSystem.MainModule main = ps.main;
                main.useUnscaledTime = false;
            }

            for (int i = 0; i < trailRenderers.Length; i++)
            {
                trailRenderers[i]?.Clear();
            }

            transform.SetParent(poolParent, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = defaultLocalScale;
            gameObject.SetActive(false);
            return true;
        }
    }
}
