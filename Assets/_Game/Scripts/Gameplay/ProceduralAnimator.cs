using System;
using System.Collections;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Lightweight code-driven animation for the primitive character/tower models
    /// (no rig or Animator needed — swap for authored clips later). Provides an
    /// always-on idle bob, a walk waddle, a hit-reaction squash, an attack recoil
    /// pulse, and a death topple. Everything animates a child "model" transform so
    /// the logic root (collider, registry position) is never disturbed.
    /// </summary>
    public class ProceduralAnimator : MonoBehaviour
    {
        [SerializeField] private Transform model;

        [Header("Idle / Walk")]
        [SerializeField] private float idleBobAmp = 0.05f;
        [SerializeField] private float idleBobSpeed = 2.2f;
        [SerializeField] private float walkBobAmp = 0.14f;
        [SerializeField] private float walkBobSpeed = 9f;
        [SerializeField] private float waddleDegrees = 6f;

        private Vector3 baseLocalPos;
        private Vector3 baseScale;
        private bool moving;
        private float hitTimer;
        private float attackTimer;
        private bool dead;

        private const float HitDuration = 0.16f;
        private const float AttackDuration = 0.18f;

        private void Awake()
        {
            if (model == null)
            {
                model = transform;
            }

            baseLocalPos = model.localPosition;
            baseScale = model.localScale;
        }

        public void SetMoving(bool value) => moving = value;
        public void PlayHit() => hitTimer = HitDuration;
        public void PlayAttack() => attackTimer = AttackDuration;

        public void PlayDeath(Action onComplete)
        {
            if (dead)
            {
                onComplete?.Invoke();
                return;
            }

            dead = true;
            StartCoroutine(DeathRoutine(onComplete));
        }

        private void Update()
        {
            if (dead || model == null)
            {
                return;
            }

            float amp = moving ? walkBobAmp : idleBobAmp;
            float speed = moving ? walkBobSpeed : idleBobSpeed;
            float phase = Time.time * speed;

            float bob = Mathf.Abs(Mathf.Sin(phase)) * amp;
            model.localPosition = baseLocalPos + Vector3.up * bob;

            float tilt = moving ? Mathf.Sin(phase) * waddleDegrees : 0f;
            model.localRotation = Quaternion.Euler(0f, 0f, tilt);

            Vector3 scale = baseScale;
            if (hitTimer > 0f)
            {
                hitTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(hitTimer / HitDuration);
                // squash wide + short, then recover
                scale = Vector3.Scale(baseScale, new Vector3(1f + 0.3f * k, 1f - 0.25f * k, 1f + 0.3f * k));
            }

            if (attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(attackTimer / AttackDuration);
                scale *= 1f + 0.12f * k; // quick punch
            }

            model.localScale = scale;
        }

        private IEnumerator DeathRoutine(Action onComplete)
        {
            Vector3 startScale = model.localScale;
            Quaternion startRot = model.localRotation;
            float t = 0f;
            const float dur = 0.25f;

            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                model.localScale = Vector3.Lerp(startScale, startScale * 0.1f, k);
                model.localRotation = startRot * Quaternion.Euler(95f * k, 0f, 0f);
                model.localPosition = baseLocalPos + Vector3.down * (0.3f * k);
                yield return null;
            }

            onComplete?.Invoke();
        }
    }
}
