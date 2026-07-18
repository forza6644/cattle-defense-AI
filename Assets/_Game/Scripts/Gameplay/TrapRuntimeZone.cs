using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public sealed class TrapRuntimeZone : MonoBehaviour
    {
        private readonly Dictionary<Enemy, int> ticksByEnemy = new Dictionary<Enemy, int>();
        private readonly Dictionary<Enemy, int> activationByEnemy = new Dictionary<Enemy, int>();
        private readonly List<Enemy> touchedEnemies = new List<Enemy>();
        private TrapRuntimeManager owner;
        private TrapDefinition definition;
        private BattlefieldAnchor anchor;
        private float lifeRemaining;
        private float tickTimer;
        private float phaseTimer;
        private bool triggered;
        private bool burning;
        private int generation;
        private const string SourcePrefix = "battlefield:";

        public TrapDefinition Definition => definition;
        public BattlefieldAnchor Anchor => anchor;
        public bool IsBurning => burning;
        public bool IsTriggered => triggered;
        public int Generation => generation;

        public void Activate(TrapRuntimeManager manager, TrapDefinition data, BattlefieldAnchor targetAnchor)
        {
            owner = manager; definition = data; anchor = targetAnchor; generation++;
            lifeRemaining = Mathf.Max(0.01f, data.duration); tickTimer = 0f; phaseTimer = 0f;
            triggered = data.runtimeType == TrapRuntimeType.Caltrops; burning = false;
            ticksByEnemy.Clear(); activationByEnemy.Clear(); touchedEnemies.Clear();
            transform.SetPositionAndRotation(targetAnchor.transform.position, targetAnchor.transform.rotation);
            transform.localScale = new Vector3(data.effectRadius * 2f, 0.08f, data.effectRadius * 2f);
            gameObject.SetActive(true);
            SetColor(data.runtimeType == TrapRuntimeType.Caltrops ? new Color(0.35f, 0.35f, 0.38f) : new Color(0.08f, 0.05f, 0.02f));
        }

        private void Update()
        {
            TickForQualification(Time.deltaTime);
        }

        public void TickForQualification(float delta)
        {
            if (definition == null) return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;
            delta = Mathf.Max(0f, delta); lifeRemaining -= delta;
            if (lifeRemaining <= 0f) { Deactivate(); return; }
            if (definition.runtimeType == TrapRuntimeType.BurningOil && !triggered)
            {
                if (HasValidEnemyInRadius()) { triggered = true; phaseTimer = definition.ignitionDelay; SetColor(new Color(1f, 0.5f, 0.08f)); }
                return;
            }
            if (definition.runtimeType == TrapRuntimeType.BurningOil && !burning)
            {
                phaseTimer -= delta;
                if (phaseTimer <= 0f) { burning = true; phaseTimer = definition.burningDuration; SetColor(new Color(1f, 0.18f, 0.02f)); }
                return;
            }
            if (burning) { phaseTimer -= delta; if (phaseTimer <= 0f) { Deactivate(); return; } }
            tickTimer -= delta;
            if (tickTimer <= 0f) { tickTimer += Mathf.Max(0.05f, definition.triggerInterval); ApplyTick(); }
        }

        private bool HasValidEnemyInRadius()
        {
            var enemies = EnemyManager.All;
            float radiusSq = definition.effectRadius * definition.effectRadius;
            for (int i = 0; i < enemies.Count; i++) if (IsValid(enemies[i]) && (enemies[i].transform.position - transform.position).sqrMagnitude <= radiusSq) return true;
            return false;
        }

        private void ApplyTick()
        {
            var enemies = EnemyManager.All;
            float radiusSq = definition.effectRadius * definition.effectRadius;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (!IsValid(enemy) || (enemy.transform.position - transform.position).sqrMagnitude > radiusSq) continue;
                if (!ticksByEnemy.TryGetValue(enemy, out int count) || !activationByEnemy.TryGetValue(enemy, out int token) || token != enemy.ActivationId) count = 0;
                if (count >= definition.maxTicksPerEnemy) continue;
                ticksByEnemy[enemy] = count + 1; activationByEnemy[enemy] = enemy.ActivationId;
                if (!touchedEnemies.Contains(enemy)) touchedEnemies.Add(enemy);
                string source = SourcePrefix + definition.stableId;
                if (definition.runtimeType == TrapRuntimeType.Caltrops)
                {
                    float resistance = enemy.Data != null ? enemy.Data.crowdControlResistance : 0f;
                    float multiplier = 1f - ((1f - Mathf.Clamp(definition.statusEffectValue, 0.05f, 1f)) * (1f - resistance));
                    enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, multiplier, definition.statusEffectDuration, source));
                    float dealt = enemy.TakeDamage(definition.damage);
                    DamageTracker.RecordDamage(source, dealt);
                }
                else
                {
                    enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, definition.damage, definition.statusEffectDuration, source));
                }
            }
        }

        private static bool IsValid(Enemy enemy) => enemy != null && enemy.IsTargetable && !enemy.IsDead;

        public void Deactivate()
        {
            if (definition != null)
            {
                string source = SourcePrefix + definition.stableId;
                for (int i = 0; i < touchedEnemies.Count; i++)
                {
                    Enemy enemy = touchedEnemies[i];
                    if (enemy != null) enemy.RemoveStatusEffectsFromSource(source);
                }
            }
            ticksByEnemy.Clear(); activationByEnemy.Clear(); touchedEnemies.Clear();
            BattlefieldAnchor previousAnchor = anchor; TrapRuntimeManager previousOwner = owner;
            definition = null; anchor = null; owner = null; triggered = false; burning = false;
            previousAnchor?.Release(this); gameObject.SetActive(false); previousOwner?.Return(this);
        }

        private void SetColor(Color color)
        {
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer == null) return;
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); renderer.SetPropertyBlock(block);
        }
    }
}
