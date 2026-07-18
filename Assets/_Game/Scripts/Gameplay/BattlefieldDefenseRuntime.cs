using UnityEngine;

namespace Stonehold
{
    public sealed class BattlefieldDefenseRuntime : MonoBehaviour
    {
        private BattlefieldDefenseManager owner;
        private BattlefieldDefenseDefinition definition;
        private BattlefieldAnchor anchor;
        private float health;
        private int generation;
        private LineRenderer healthBar;

        public bool IsActive { get; private set; }
        public float CurrentHealth => health;
        public float MaxHealth => definition != null ? definition.health : 0f;
        public float MeleeStopRange => 1.45f;
        public BattlefieldDefenseDefinition Definition => definition;
        public int Generation => generation;

        public void Activate(BattlefieldDefenseManager manager, BattlefieldDefenseDefinition data, BattlefieldAnchor targetAnchor)
        {
            owner = manager; definition = data; anchor = targetAnchor; generation++; health = data.health; IsActive = true;
            transform.SetPositionAndRotation(anchor.transform.position, anchor.transform.rotation); gameObject.SetActive(true); EnsureHealthBar(); UpdateHealthBar();
        }

        public float TakeDamage(float amount, Enemy source, int sourceActivationId)
        {
            if (!IsActive || amount <= 0f || source == null || !source.MatchesActivation(sourceActivationId)) return 0f;
            float applied = Mathf.Max(1f, amount - (definition != null ? definition.armor : 0f));
            health -= applied; UpdateHealthBar();
            VfxManager.Instance?.PlayHit(transform.position + Vector3.up, new Color(0.65f, 0.4f, 0.15f));
            if (health <= 0f) Break();
            return applied;
        }

        public void Break()
        {
            if (!IsActive) return;
            IsActive = false; health = 0f; BattlefieldAnchor previousAnchor = anchor; BattlefieldDefenseManager previousOwner = owner;
            anchor = null; owner = null; previousAnchor?.Release(this); gameObject.SetActive(false); previousOwner?.Return(this);
        }

        private void EnsureHealthBar()
        {
            if (healthBar != null) return;
            GameObject bar = new GameObject("DefenseHealthBar"); bar.transform.SetParent(transform, false); bar.transform.localPosition = Vector3.up * 2f;
            healthBar = bar.AddComponent<LineRenderer>(); healthBar.useWorldSpace = false; healthBar.positionCount = 2; healthBar.startWidth = 0.15f; healthBar.endWidth = 0.15f;
            healthBar.material = new Material(Shader.Find("Sprites/Default")); healthBar.startColor = healthBar.endColor = new Color(0.25f, 0.9f, 0.3f);
        }

        private void UpdateHealthBar()
        {
            if (healthBar == null) return;
            float ratio = MaxHealth > 0f ? Mathf.Clamp01(health / MaxHealth) : 0f;
            healthBar.SetPosition(0, new Vector3(-0.8f, 0f, 0f)); healthBar.SetPosition(1, new Vector3(-0.8f + 1.6f * ratio, 0f, 0f));
        }
    }
}
