using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Standalone combat controller for the fortress Starter Crystal.
    /// Occupies its own dedicated position above the fortress gate.
    /// Independent of HeroSlots, HeroRosterManager ownership, and character rigs.
    /// </summary>
    public class StarterCrystal : MonoBehaviour
    {
        [SerializeField] private StarterCrystalDefinition definition;
        [SerializeField] private Vector3 launchOffset = new Vector3(0f, 0.5f, 0f);

        private float targetRefreshTimer;
        private float fireCooldown;
        private Enemy currentTarget;
        private Renderer crystalRenderer;
        private MeshFilter crystalMeshFilter;
        private readonly int[] cachedChainHitIds = new int[16];

        private Transform crystalGemTransform;
        private Material runtimeMaterial;
        private Color baseEmissionColor;
        private float hoverTime;

        public StarterCrystalDefinition Definition => definition;

        private void Awake()
        {
            crystalRenderer = GetComponent<Renderer>();
            if (crystalRenderer == null)
            {
                crystalRenderer = GetComponentInChildren<Renderer>();
            }
            crystalMeshFilter = GetComponent<MeshFilter>();
            if (crystalMeshFilter == null)
            {
                crystalMeshFilter = GetComponentInChildren<MeshFilter>();
            }
        }

        private void Start()
        {
            LoadSelectedCrystal();
        }

        public void Configure(StarterCrystalDefinition crystalDefinition)
        {
            definition = crystalDefinition;
            if (definition == null)
            {
                return;
            }

            BuildVisualHierarchy();

            fireCooldown = 0f;
            targetRefreshTimer = 0f;
        }

        private void BuildVisualHierarchy()
        {
            MeshRenderer rootRenderer = GetComponent<MeshRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            Transform visualContainer = transform.Find("CrystalVisualContainer");
            if (visualContainer != null)
            {
                if (Application.isPlaying) Destroy(visualContainer.gameObject);
                else DestroyImmediate(visualContainer.gameObject);
            }

            GameObject containerGo = new GameObject("CrystalVisualContainer");
            containerGo.transform.SetParent(transform, false);
            containerGo.transform.localPosition = Vector3.zero;

            GameObject pedestalGo = new GameObject("Pedestal");
            pedestalGo.transform.SetParent(containerGo.transform, false);
            pedestalGo.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            pedestalGo.transform.localScale = new Vector3(0.65f, 0.08f, 0.65f);

            MeshFilter pMf = pedestalGo.AddComponent<MeshFilter>();
            pMf.sharedMesh = CreatePedestalMesh();

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");

            MeshRenderer pRenderer = pedestalGo.AddComponent<MeshRenderer>();
            Material pMat = new Material(litShader);
            pMat.color = new Color(0.18f, 0.20f, 0.24f);
            pMat.SetFloat("_Smoothness", 0.3f);
            pMat.SetFloat("_Metallic", 0.6f);
            pRenderer.sharedMaterial = pMat;

            GameObject gemGo = new GameObject("CrystalGem");
            gemGo.transform.SetParent(containerGo.transform, false);
            gemGo.transform.localPosition = new Vector3(0f, 0.20f, 0f);

            MeshFilter mf = gemGo.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateFacetedGemMesh();

            MeshRenderer mr = gemGo.AddComponent<MeshRenderer>();

            Color mainColor;
            Color emissiveColor;
            Vector3 gemScale;

            switch (definition != null ? definition.element : CrystalElement.Lightning)
            {
                case CrystalElement.Fire:
                    mainColor = new Color(1.0f, 0.25f, 0.05f);
                    emissiveColor = new Color(1.0f, 0.35f, 0.05f);
                    gemScale = new Vector3(0.44f, 0.85f, 0.44f);
                    break;
                case CrystalElement.Ice:
                    mainColor = new Color(0.1f, 0.85f, 1.0f);
                    emissiveColor = new Color(0.0f, 0.75f, 1.0f);
                    gemScale = new Vector3(0.42f, 0.85f, 0.42f);
                    break;
                case CrystalElement.Lightning:
                    mainColor = new Color(1.0f, 0.85f, 0.1f);
                    emissiveColor = new Color(1.0f, 0.70f, 0.0f);
                    gemScale = new Vector3(0.38f, 0.95f, 0.38f);
                    break;
                case CrystalElement.Stone:
                    mainColor = new Color(0.85f, 0.55f, 0.22f);
                    emissiveColor = new Color(0.55f, 0.35f, 0.12f);
                    gemScale = new Vector3(0.52f, 0.75f, 0.52f);
                    break;
                case CrystalElement.Shadow:
                default:
                    mainColor = new Color(0.65f, 0.20f, 1.0f);
                    emissiveColor = new Color(0.50f, 0.10f, 0.90f);
                    gemScale = new Vector3(0.40f, 0.88f, 0.40f);
                    break;
            }

            gemGo.transform.localScale = gemScale;

            if (definition != null && definition.crystalMaterial != null)
            {
                runtimeMaterial = new Material(definition.crystalMaterial);
            }
            else
            {
                runtimeMaterial = new Material(litShader);
            }

            runtimeMaterial.color = mainColor;
            runtimeMaterial.EnableKeyword("_EMISSION");
            runtimeMaterial.SetColor("_EmissionColor", emissiveColor * 1.5f);
            baseEmissionColor = emissiveColor * 1.5f;

            mr.sharedMaterial = runtimeMaterial;
            crystalGemTransform = gemGo.transform;
            crystalRenderer = mr;
        }

        private static Mesh CreateFacetedGemMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "FacetedGemMesh";

            Vector3 top = new Vector3(0f, 0.5f, 0f);
            Vector3 bottom = new Vector3(0f, -0.5f, 0f);
            Vector3 p0 = new Vector3(0.5f, 0f, 0f);
            Vector3 p1 = new Vector3(0f, 0f, 0.5f);
            Vector3 p2 = new Vector3(-0.5f, 0f, 0f);
            Vector3 p3 = new Vector3(0f, 0f, -0.5f);

            Vector3[] vertices = new Vector3[24]
            {
                top, p0, p1,
                top, p1, p2,
                top, p2, p3,
                top, p3, p0,
                bottom, p1, p0,
                bottom, p2, p1,
                bottom, p3, p2,
                bottom, p0, p3
            };

            int[] triangles = new int[24];
            for (int i = 0; i < 24; i++)
            {
                triangles[i] = i;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreatePedestalMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "PedestalMesh";
            int sides = 8;
            Vector3[] vertices = new Vector3[sides * 2 + 2];
            float radius = 0.5f;
            float height = 0.5f;

            vertices[0] = new Vector3(0f, height * 0.5f, 0f);
            vertices[1] = new Vector3(0f, -height * 0.5f, 0f);

            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                vertices[2 + i] = new Vector3(x, height * 0.5f, z);
                vertices[2 + sides + i] = new Vector3(x, -height * 0.5f, z);
            }

            List<int> tris = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int topCurr = 2 + i;
                int topNext = 2 + next;
                int botCurr = 2 + sides + i;
                int botNext = 2 + sides + next;

                tris.Add(topCurr); tris.Add(botCurr); tris.Add(topNext);
                tris.Add(topNext); tris.Add(botCurr); tris.Add(botNext);
                tris.Add(0); tris.Add(topNext); tris.Add(topCurr);
            }

            mesh.vertices = vertices;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public void LoadSelectedCrystal()
        {
            string selectedId = SaveManager.SelectedStarterCrystalId;
            if (string.IsNullOrEmpty(selectedId))
            {
                selectedId = "crystal_lightning";
            }

            if (definition == null || definition.crystalId != selectedId)
            {
                StarterCrystalDefinition def = Resources.Load<StarterCrystalDefinition>("Crystals/" + selectedId);
                if (def == null)
                {
                    StarterCrystalDefinition[] allCrystals = Resources.LoadAll<StarterCrystalDefinition>("");
                    for (int i = 0; i < allCrystals.Length; i++)
                    {
                        if (allCrystals[i] != null && allCrystals[i].crystalId == selectedId)
                        {
                            def = allCrystals[i];
                            break;
                        }
                    }
                }

                if (def != null)
                {
                    Configure(def);
                    return;
                }
            }

            if (definition != null)
            {
                Configure(definition);
            }
            else
            {
                Debug.LogWarning($"[StarterCrystal] Definition for '{selectedId}' not found. Using current configuration.");
            }
        }

        private void Update()
        {
            if (crystalGemTransform != null)
            {
                hoverTime += Time.deltaTime;
                float hoverOffset = Mathf.Sin(hoverTime * 2.2f) * 0.05f;
                crystalGemTransform.localPosition = new Vector3(0f, 0.20f + hoverOffset, 0f);
                crystalGemTransform.Rotate(Vector3.up, 30f * Time.deltaTime, Space.World);

                if (runtimeMaterial != null)
                {
                    float pulse = 0.85f + Mathf.Sin(hoverTime * 3.5f) * 0.15f;
                    runtimeMaterial.SetColor("_EmissionColor", baseEmissionColor * pulse);
                }
            }

            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            if (definition == null)
            {
                return;
            }

            targetRefreshTimer -= Time.deltaTime;
            fireCooldown -= Time.deltaTime;

            if (targetRefreshTimer <= 0f)
            {
                float range = GetModifiedRange();
                currentTarget = EnemyManager.FindTarget(transform.position, range, TargetingMode.ClosestToGoal);
                targetRefreshTimer = 0.15f;
            }

            if (currentTarget == null || !currentTarget.IsTargetable)
            {
                return;
            }

            Vector3 direction = currentTarget.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
            }

            if (fireCooldown <= 0f)
            {
                FireAttack(currentTarget);
                float rate = GetModifiedFireRate();
                fireCooldown = rate > 0f ? 1f / rate : 1f;
            }
        }

        public float GetModifiedDamage()
        {
            float damage = definition != null ? definition.baseDamage : 0f;
            if (MetaUpgradeManager.Instance != null)
            {
                damage *= MetaUpgradeManager.Instance.GetGlobalDamageMultiplier();
            }
            return damage;
        }

        public float GetModifiedFireRate()
        {
            float fireRate = definition != null ? definition.attacksPerSecond : 1f;
            if (MetaUpgradeManager.Instance != null)
            {
                fireRate *= MetaUpgradeManager.Instance.GetGlobalFireRateMultiplier();
            }
            return fireRate;
        }

        public float GetModifiedRange()
        {
            float range = definition != null ? definition.attackRange : 14f;
            if (MetaUpgradeManager.Instance != null)
            {
                range *= MetaUpgradeManager.Instance.GetGlobalRangeMultiplier();
            }
            return range;
        }

        private void FireAttack(Enemy primaryTarget)
        {
            if (primaryTarget == null || !primaryTarget.IsTargetable)
            {
                return;
            }

            Vector3 origin = transform.position + launchOffset;
            float damage = GetModifiedDamage();

            string muzzleHeroId;
            switch (definition.element)
            {
                case CrystalElement.Fire: muzzleHeroId = "fire_mage"; break;
                case CrystalElement.Ice: muzzleHeroId = "frost_mage"; break;
                case CrystalElement.Lightning: muzzleHeroId = "electric_engineer"; break;
                case CrystalElement.Stone: muzzleHeroId = "bombardier"; break;
                case CrystalElement.Shadow: default: muzzleHeroId = "sniper"; break;
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroMuzzle(origin, muzzleHeroId);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHeroShot(muzzleHeroId);
            }

            switch (definition.element)
            {
                case CrystalElement.Fire:
                    ExecuteFireAttack(primaryTarget, origin, damage);
                    break;
                case CrystalElement.Ice:
                    ExecuteIceAttack(primaryTarget, origin, damage);
                    break;
                case CrystalElement.Lightning:
                    ExecuteLightningAttack(primaryTarget, origin, damage);
                    break;
                case CrystalElement.Stone:
                    ExecuteStoneAttack(primaryTarget, origin, damage);
                    break;
                case CrystalElement.Shadow:
                    ExecuteShadowAttack(primaryTarget, origin, damage);
                    break;
            }
        }

        private void ExecuteFireAttack(Enemy target, Vector3 origin, float damage)
        {
            float applied = target.TakeDamage(damage);
            DamageTracker.RecordDamage(definition.crystalId, applied);

            if (!target.IsDead && definition.damageOverTime > 0f)
            {
                target.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, definition.damageOverTime, definition.damageOverTimeDuration, definition.crystalId));
            }

            if (definition.splashRadius > 0f)
            {
                float radiusSqr = definition.splashRadius * definition.splashRadius;
                var enemies = EnemyManager.All;
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Enemy enemy = enemies[i];
                    if (enemy != null && enemy != target && !enemy.IsDead && (enemy.transform.position - target.transform.position).sqrMagnitude <= radiusSqr)
                    {
                        float splashDmg = enemy.TakeDamage(damage * 0.6f);
                        DamageTracker.RecordDamage(definition.crystalId, splashDmg);
                        if (!enemy.IsDead && definition.damageOverTime > 0f)
                        {
                            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, definition.damageOverTime * 0.6f, definition.damageOverTimeDuration, definition.crystalId));
                        }
                    }
                }
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayAbilityTrace(origin, target.transform.position, "fire_mage", 0.15f);
                VfxManager.Instance.PlayFireImpact(target.transform.position);
                VfxManager.Instance.PlayImpactRing(target.transform.position, new Color(1.0f, 0.35f, 0f), definition.splashRadius > 0f ? definition.splashRadius : 2.5f, 0.25f, 0.12f);
                if (!target.IsDead && definition.damageOverTime > 0f)
                {
                    VfxManager.Instance.PlayBurn(target.transform.position);
                }
            }
        }

        private void ExecuteIceAttack(Enemy target, Vector3 origin, float damage)
        {
            float applied = target.TakeDamage(damage);
            DamageTracker.RecordDamage(definition.crystalId, applied);

            if (!target.IsDead)
            {
                float slowMult = Mathf.Clamp(1f - definition.statusMagnitude, 0.1f, 0.9f);
                float duration = definition.statusDuration > 0f ? definition.statusDuration : 3f;
                target.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, slowMult, duration, definition.crystalId));
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayAbilityTrace(origin, target.transform.position, "frost_mage", 0.15f);
                VfxManager.Instance.PlayFrost(target.transform.position, 1.1f);
                VfxManager.Instance.PlayImpactRing(target.transform.position, new Color(0.2f, 0.85f, 1.0f), 1.0f, 0.30f, 0.12f);
            }
        }

        private void ExecuteLightningAttack(Enemy primaryTarget, Vector3 origin, float damage)
        {
            int maxChains = Mathf.Max(1, definition.chainTargets);
            Vector3 sourcePos = origin;
            Enemy current = primaryTarget;

            for (int i = 0; i < cachedChainHitIds.Length; i++)
            {
                cachedChainHitIds[i] = 0;
            }

            int hitCount = 0;

            for (int b = 0; b < maxChains && current != null; b++)
            {
                cachedChainHitIds[hitCount++] = current.ActivationId;
                float applied = current.TakeDamage(damage);
                DamageTracker.RecordDamage(definition.crystalId, applied);

                Vector3 targetPos = current.transform.position + Vector3.up * 0.25f;
                if (VfxManager.Instance != null)
                {
                    VfxManager.Instance.PlayAbilityTrace(sourcePos, targetPos, "electric_engineer", 0.10f);
                    VfxManager.Instance.PlayShockImpact(current.transform.position);
                }

                if (!current.IsDead)
                {
                    current.ApplyStatusEffect(new StatusEffect(StatusEffectType.Shock, 1f, 2f, definition.crystalId));
                }

                sourcePos = targetPos;
                current = FindNextChainTarget(current.transform.position, cachedChainHitIds, hitCount, 5.0f);
            }
        }

        private void ExecuteStoneAttack(Enemy target, Vector3 origin, float damage)
        {
            float applied = target.TakeDamage(damage);
            DamageTracker.RecordDamage(definition.crystalId, applied);

            if (definition.splashRadius > 0f)
            {
                float radiusSqr = definition.splashRadius * definition.splashRadius;
                var enemies = EnemyManager.All;
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Enemy enemy = enemies[i];
                    if (enemy != null && enemy != target && !enemy.IsDead && (enemy.transform.position - target.transform.position).sqrMagnitude <= radiusSqr)
                    {
                        float splashDmg = enemy.TakeDamage(damage * 0.5f);
                        DamageTracker.RecordDamage(definition.crystalId, splashDmg);
                    }
                }
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayAbilityTrace(origin, target.transform.position, "bombardier", 0.22f);
                VfxManager.Instance.PlaySniperImpact(target.transform.position);
                VfxManager.Instance.PlayImpactRing(target.transform.position, new Color(0.85f, 0.55f, 0.2f), definition.splashRadius > 0f ? definition.splashRadius : 1.5f, 0.35f, 0.15f);
            }
        }

        private void ExecuteShadowAttack(Enemy target, Vector3 origin, float damage)
        {
            float applied = target.TakeDamage(damage);
            DamageTracker.RecordDamage(definition.crystalId, applied);

            if (!target.IsDead && definition.damageOverTime > 0f)
            {
                float dotDur = definition.damageOverTimeDuration > 0f ? definition.damageOverTimeDuration : 4f;
                target.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, definition.damageOverTime, dotDur, "crystal_shadow"));
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayAbilityTrace(origin, target.transform.position, "sniper", 0.18f);
                VfxManager.Instance.PlaySniperImpact(target.transform.position);
                VfxManager.Instance.PlayImpactRing(target.transform.position, new Color(0.65f, 0.15f, 0.95f), 0.9f, 0.35f, 0.12f);
            }
        }

        private Enemy FindNextChainTarget(Vector3 sourcePos, int[] hitList, int hitCount, float bounceRange)
        {
            Enemy best = null;
            float bestDistSqr = bounceRange * bounceRange;
            var all = EnemyManager.All;
            for (int i = 0; i < all.Count; i++)
            {
                Enemy enemy = all[i];
                if (enemy == null || enemy.IsDead || !enemy.IsTargetable) continue;

                bool alreadyHit = false;
                for (int j = 0; j < hitCount; j++)
                {
                    if (hitList[j] != 0 && hitList[j] == enemy.ActivationId)
                    {
                        alreadyHit = true;
                        break;
                    }
                }
                if (alreadyHit) continue;

                float distSqr = (enemy.transform.position - sourcePos).sqrMagnitude;
                if (distSqr <= bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    best = enemy;
                }
            }
            return best;
        }
    }
}
