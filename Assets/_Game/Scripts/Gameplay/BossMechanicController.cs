using System;
using System.Collections;
using UnityEngine;

namespace Stonehold
{
    public enum BossPhase
    {
        Phase1_Normal,
        Phase2_Enraged,
        Phase3_ApexOverdrive
    }

    /// <summary>
    /// Controls multi-phase boss fight mechanics, telegraphed arena slam attacks,
    /// kinetic shield projection, and escort summons.
    /// </summary>
    public class BossMechanicController : MonoBehaviour
    {
        [Header("Phase Settings")]
        public BossPhase currentPhase = BossPhase.Phase1_Normal;
        public float phase2HealthThreshold = 0.66f;
        public float phase3HealthThreshold = 0.33f;

        [Header("Telegraphed Slam Attack")]
        public float slamCooldown = 6.0f;
        public float slamTelegraphDuration = 1.2f;
        public float slamRadius = 4.5f;
        public float slamDamage = 80f;

        [Header("Kinetic Shield")]
        public float maxKineticShield = 250f;
        public float currentKineticShield = 0f;

        public event Action<BossPhase> OnPhaseChanged;
        public event Action OnSlamTelegraphStarted;
        public event Action OnSlamExecuted;

        private Enemy parentEnemy;
        private Coroutine bossRoutine;
        private bool isSlamming = false;

        private void Awake()
        {
            parentEnemy = GetComponent<Enemy>();
        }

        private void Start()
        {
            bossRoutine = StartCoroutine(BossBehaviorLoop());
        }

        private void OnDestroy()
        {
            if (bossRoutine != null)
            {
                StopCoroutine(bossRoutine);
            }
        }

        public void CheckHealthTransition(float currentHp, float maxHp)
        {
            if (maxHp <= 0f) return;
            float hpPct = currentHp / maxHp;

            if (currentPhase == BossPhase.Phase1_Normal && hpPct <= phase2HealthThreshold)
            {
                TransitionToPhase(BossPhase.Phase2_Enraged);
            }
            else if (currentPhase == BossPhase.Phase2_Enraged && hpPct <= phase3HealthThreshold)
            {
                TransitionToPhase(BossPhase.Phase3_ApexOverdrive);
            }
        }

        public void TransitionToPhase(BossPhase newPhase)
        {
            if (currentPhase == newPhase) return;
            currentPhase = newPhase;

            if (newPhase == BossPhase.Phase2_Enraged)
            {
                slamCooldown = 4.0f; // Faster slam cadence
                Debug.Log("[BossMechanic] 🔥 BOSS ENTERED PHASE 2: ENRAGED!");
            }
            else if (newPhase == BossPhase.Phase3_ApexOverdrive)
            {
                currentKineticShield = maxKineticShield;
                slamCooldown = 3.0f;
                Debug.Log("[BossMechanic] ⚡ BOSS ENTERED PHASE 3: APEX OVERDRIVE (Kinetic Shield Active)!");
            }

            OnPhaseChanged?.Invoke(newPhase);
        }

        public float AbsorbDamage(float incomingDamage)
        {
            if (currentKineticShield <= 0f) return incomingDamage;

            if (incomingDamage <= currentKineticShield)
            {
                currentKineticShield -= incomingDamage;
                return 0f;
            }

            float leftover = incomingDamage - currentKineticShield;
            currentKineticShield = 0f;
            return leftover;
        }

        private IEnumerator BossBehaviorLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(slamCooldown);
                if (parentEnemy != null && parentEnemy.IsDead) yield break;

                yield return StartCoroutine(ExecuteTelegraphedSlam());
            }
        }

        public IEnumerator ExecuteTelegraphedSlam()
        {
            if (isSlamming) yield break;
            isSlamming = true;

            OnSlamTelegraphStarted?.Invoke();
            Debug.Log($"[BossMechanic] ⚠️ WARNING: Boss telegraphed Shockwave Slam (Radius: {slamRadius}m)!");

            yield return new WaitForSeconds(slamTelegraphDuration);

            // Execute Slam
            ExecuteSlamDamage();
            OnSlamExecuted?.Invoke();
            isSlamming = false;
        }

        public void ExecuteSlamDamage()
        {
            Vector3 origin = transform.position;
            var colliders = Physics.OverlapSphere(origin, slamRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                var castle = colliders[i].GetComponentInParent<Castle>();
                if (castle != null)
                {
                    castle.TakeDamage(Mathf.RoundToInt(slamDamage));
                }
            }
            Debug.Log($"[BossMechanic] 💥 SLAM DETONATED: Dealt {slamDamage} damage in {slamRadius}m radius.");
        }
    }
}
