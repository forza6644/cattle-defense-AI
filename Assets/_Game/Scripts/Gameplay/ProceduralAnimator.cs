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
        private float flashTimer;
        private bool dead;
        private Animator unityAnimator;

        private Renderer[] renderers;
        private Color[] baseColors;
        private MaterialPropertyBlock mpb;

        private const float HitDuration = 0.18f;
        private const float AttackDuration = 0.22f;
        private const float FlashDuration = 0.14f;
        private const float BreatheCycleSpeed = 1.1f;
        private const float BreatheScale = 0.02f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            if (model == null)
            {
                model = transform;
            }

            baseLocalPos = model.localPosition;
            baseScale = model.localScale;

            unityAnimator = GetComponentInChildren<Animator>();

            renderers = model.GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material shared = renderers[i].sharedMaterial;
                baseColors[i] = shared != null && shared.HasProperty(BaseColorId) ? shared.GetColor(BaseColorId) : Color.white;
            }

            mpb = new MaterialPropertyBlock();
        }

        public void SetMoving(bool value) => moving = value;
        public void PlayHit() { hitTimer = HitDuration; flashTimer = FlashDuration; }

        public void PlayAttack()
        {
            attackTimer = AttackDuration;
            if (unityAnimator != null)
            {
                unityAnimator.Play("Attack", 0, 0f);
            }
        }

        public void PlayAbility()
        {
            attackTimer = AttackDuration;
            if (unityAnimator != null)
            {
                unityAnimator.Play("Ability", 0, 0f);
            }
        }

        public void PlayDeath(Action onComplete)
        {
            if (dead)
            {
                onComplete?.Invoke();
                return;
            }

            dead = true;
            if (unityAnimator != null)
            {
                unityAnimator.Play("Death", 0, 0f);
                StartCoroutine(WaitAndComplete(0.9f, onComplete));
            }
            else
            {
                StartCoroutine(DeathRoutine(onComplete));
            }
        }

        private IEnumerator WaitAndComplete(float duration, Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
        }

        private void Update()
        {
            if (dead || model == null)
            {
                return;
            }

            if (unityAnimator != null)
            {
                model.localPosition = baseLocalPos;
                model.localRotation = Quaternion.identity;

                var stateInfo = unityAnimator.GetCurrentAnimatorStateInfo(0);
                bool isAttacking = stateInfo.IsName("Attack") || stateInfo.IsName("Ability");
                if (!isAttacking || stateInfo.normalizedTime >= 0.95f)
                {
                    string targetState = moving ? "Walk" : "Idle";
                    if (!stateInfo.IsName(targetState))
                    {
                        unityAnimator.Play(targetState);
                    }
                }
            }
            else
            {
                float amp = moving ? walkBobAmp : idleBobAmp;
                float speed = moving ? walkBobSpeed : idleBobSpeed;
                float phase = Time.time * speed;

                float bob = Mathf.Abs(Mathf.Sin(phase)) * amp;
                model.localPosition = baseLocalPos + Vector3.up * bob;

                float tilt = moving ? Mathf.Sin(phase) * waddleDegrees : 0f;
                model.localRotation = Quaternion.Euler(0f, 0f, tilt);
            }

            // Subtle idle breathe cycle
            float breathe = 1f + Mathf.Sin(Time.time * BreatheCycleSpeed) * BreatheScale;

            Vector3 scale = baseScale * breathe;
            if (hitTimer > 0f)
            {
                hitTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(hitTimer / HitDuration);
                // squash wide + short, then recover
                scale = Vector3.Scale(baseScale, new Vector3(1f + 0.35f * k, 1f - 0.3f * k, 1f + 0.35f * k));
            }

            if (attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(attackTimer / AttackDuration);
                scale *= 1f + 0.18f * k; // strong attack punch
            }

            model.localScale = scale;

            UpdateFlash();
        }

        private void UpdateFlash()
        {
            if (renderers == null)
            {
                return;
            }

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                float f = Mathf.Clamp01(flashTimer / FlashDuration);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].GetPropertyBlock(mpb);
                    mpb.SetColor(BaseColorId, Color.Lerp(baseColors[i], Color.white, f));
                    renderers[i].SetPropertyBlock(mpb);
                }

                if (flashTimer <= 0f)
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        renderers[i].SetPropertyBlock(null);
                    }
                }
            }
        }

        private IEnumerator DeathRoutine(Action onComplete)
        {
            Vector3 startScale = model.localScale;
            Quaternion startRot = model.localRotation;
            float t = 0f;
            const float dur = 0.35f;

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
