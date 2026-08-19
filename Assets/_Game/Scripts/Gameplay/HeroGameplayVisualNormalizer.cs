using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Gameplay-only visual envelope. Fits each hero's combined renderer bounds
    /// to a portrait-readable footprint using Archer as the benchmark.
    /// Does not change ArtAdapter.visualScale (Main Menu / authored showcase scale).
    /// </summary>
    public static class HeroGameplayVisualNormalizer
    {
        public const string PresentationRootName = "HeroPresentation";

        /// <summary>Archer-class body height in gameplay world meters after slot spawn.</summary>
        public const float TargetGameplayHeight = 1.86f;

        /// <summary>Hard cap so no hero can tower over the central combat lane.</summary>
        public const float MaxGameplayHeight = 2.08f;

        /// <summary>Slot spacing on the battlement is ~1.66m. Keep a readable gap.</summary>
        public const float MaxGameplayLaneWidth = 1.48f;

        /// <summary>Weapons may extend toward the field more than sideways.</summary>
        public const float MaxGameplayDepth = 1.85f;

        public const float MinUniformScale = 0.42f;
        public const float MaxUniformScale = 1.0f;
        public const float WeaponWidthSlack = 0.88f;

        private const float ImportedArmatureScale = 100f;
        private const float BoneSpaceFromMeters = 0.01f;
        private const float MeterAuthoredPositionThreshold = 0.02f;
        private const float OversizedLossyScaleThreshold = 5f;

        private static readonly string[] FxNameTokens =
        {
            "Halo", "Smoke", "Particle", "Trail", "VFX", "Glow", "Rune", "Ring",
            "Orb", "Flash", "Aura", "Indicator"
        };

        public readonly struct VisualMetrics
        {
            public readonly Bounds Bounds;
            public readonly float Height;
            public readonly float LaneWidth;
            public readonly float Depth;
            public readonly float AppliedUniformScale;
            public readonly bool HasBounds;

            public VisualMetrics(Bounds bounds, float appliedUniformScale, bool hasBounds)
            {
                Bounds = bounds;
                Height = bounds.size.y;
                LaneWidth = bounds.size.x;
                Depth = bounds.size.z;
                AppliedUniformScale = appliedUniformScale;
                HasBounds = hasBounds;
            }
        }

        public static VisualMetrics NormalizeSpawnedHero(GameObject instance, HeroDefinition hero)
        {
            if (instance == null)
            {
                return default;
            }

            CorrectMeterAuthoredPropsOnImportedArmature(instance.transform);
            SyncPresentationScaleToVisualRoot(instance);

            float calibration = ResolveCalibrationMultiplier(hero);
            float factor = ComputeEnvelopeScale(instance.transform);
            factor = Mathf.Clamp(factor * calibration, MinUniformScale, MaxUniformScale);

            ApplyGameplayScale(instance, factor);

            Bounds bounds = MeasureGameplayBounds(instance.transform);
            ArtAdapter adapter = instance.GetComponent<ArtAdapter>();
            float menuScale = adapter != null ? adapter.visualScale.x : 1f;
            string heroId = hero != null ? hero.id : instance.name;
            Debug.Log($"[HeroGameplayVisual] {heroId} factor={factor:0.000} height={bounds.size.y:0.00} width={bounds.size.x:0.00} menuScale={menuScale:0.00}");
            return new VisualMetrics(bounds, factor, bounds.size.sqrMagnitude > 0.0001f);
        }

        public static float ResolveCalibrationMultiplier(HeroDefinition hero)
        {
            if (hero == null || hero.gameplayVisualScaleMultiplier <= 0.01f)
            {
                return 1f;
            }

            return hero.gameplayVisualScaleMultiplier;
        }

        public static float ComputeEnvelopeScale(Transform heroRoot)
        {
            Bounds bounds = MeasureGameplayBounds(heroRoot);
            if (bounds.size.sqrMagnitude < 0.0001f)
            {
                return 1f;
            }

            float height = Mathf.Max(bounds.size.y, 0.01f);
            float laneWidth = Mathf.Max(bounds.size.x, 0.01f);
            float depth = Mathf.Max(bounds.size.z, 0.01f);

            float uniform = 1f;
            if (height > MaxGameplayHeight)
            {
                uniform = Mathf.Min(uniform, MaxGameplayHeight / height);
            }

            float widthScale = laneWidth > MaxGameplayLaneWidth ? MaxGameplayLaneWidth / laneWidth : 1f;
            float depthScale = depth > MaxGameplayDepth ? MaxGameplayDepth / depth : 1f;
            float planarScale = Mathf.Min(widthScale, depthScale);
            if (planarScale < uniform)
            {
                // Large weapons may exceed lane width without forcing the whole hero down to a speck.
                uniform = Mathf.Max(planarScale, uniform * WeaponWidthSlack);
            }

            return Mathf.Clamp(uniform, MinUniformScale, MaxUniformScale);
        }

        public static Bounds MeasureGameplayBounds(Transform heroRoot)
        {
            Bounds combined = new Bounds();
            bool hasBounds = false;
            Renderer[] renderers = heroRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!ShouldIncludeInGameplayEnvelope(renderer))
                {
                    continue;
                }

                Bounds rendererBounds = renderer.bounds;
                if (rendererBounds.size.sqrMagnitude < 0.000001f)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combined = rendererBounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(rendererBounds);
                }
            }

            return hasBounds ? combined : new Bounds(heroRoot.position, Vector3.zero);
        }

        public static bool ShouldIncludeInGameplayEnvelope(Renderer renderer)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
            {
                return false;
            }

            Transform current = renderer.transform;
            while (current != null)
            {
                if (NameLooksLikeNonCharacterFx(current.name))
                {
                    return false;
                }

                current = current.parent;
            }

            return true;
        }

        internal static void CorrectMeterAuthoredPropsOnImportedArmature(Transform heroRoot)
        {
            Transform armature = FindTransformRecursive(heroRoot, "CharacterArmature");
            if (armature == null)
            {
                return;
            }

            if (Mathf.Abs(armature.localScale.x - ImportedArmatureScale) > 1f)
            {
                return;
            }

            MeshRenderer[] meshRenderers = armature.GetComponentsInChildren<MeshRenderer>(true);
            List<Transform> emptyParents = new List<Transform>();
            List<Transform> oversizedPrimitives = new List<Transform>();

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer meshRenderer = meshRenderers[i];
                if (!IsUnityBuiltinPrimitive(meshRenderer))
                {
                    continue;
                }

                if (MaxComponent(meshRenderer.transform.lossyScale) < OversizedLossyScaleThreshold)
                {
                    continue;
                }

                oversizedPrimitives.Add(meshRenderer.transform);

                Transform ancestor = meshRenderer.transform.parent;
                while (ancestor != null && ancestor != armature)
                {
                    if (ancestor.GetComponent<MeshRenderer>() == null
                        && ancestor.GetComponent<SkinnedMeshRenderer>() == null
                        && ancestor.localPosition.magnitude > MeterAuthoredPositionThreshold
                        && !emptyParents.Contains(ancestor))
                    {
                        emptyParents.Add(ancestor);
                    }

                    ancestor = ancestor.parent;
                }
            }

            emptyParents.Sort(CompareTransformDepth);
            oversizedPrimitives.Sort(CompareTransformDepth);

            for (int i = 0; i < emptyParents.Count; i++)
            {
                Transform parent = emptyParents[i];
                if (parent.localPosition.magnitude > MeterAuthoredPositionThreshold)
                {
                    parent.localPosition *= BoneSpaceFromMeters;
                }
            }

            for (int i = 0; i < oversizedPrimitives.Count; i++)
            {
                Transform primitive = oversizedPrimitives[i];
                if (MaxComponent(primitive.lossyScale) < OversizedLossyScaleThreshold)
                {
                    continue;
                }

                primitive.localScale *= BoneSpaceFromMeters;
                if (primitive.localPosition.magnitude > MeterAuthoredPositionThreshold)
                {
                    primitive.localPosition *= BoneSpaceFromMeters;
                }
            }
        }

        private static void SyncPresentationScaleToVisualRoot(GameObject instance)
        {
            ArtAdapter adapter = instance.GetComponent<ArtAdapter>();
            Transform visualRoot = adapter != null ? adapter.visualRoot : null;
            Transform presentation = instance.transform.Find(PresentationRootName);
            if (visualRoot == null || presentation == null)
            {
                return;
            }

            float visualUniform = visualRoot.localScale.x;
            presentation.localScale = new Vector3(visualUniform, visualUniform, visualUniform);
        }

        private static void ApplyGameplayScale(GameObject instance, float uniformFactor)
        {
            ArtAdapter adapter = instance.GetComponent<ArtAdapter>();
            Transform visualRoot = adapter != null && adapter.visualRoot != null
                ? adapter.visualRoot
                : FindTransformRecursive(instance.transform, "VisualRoot");

            if (visualRoot == null)
            {
                visualRoot = FindTransformRecursive(instance.transform, "QuaterniusVisual");
            }

            if (visualRoot != null && !Mathf.Approximately(uniformFactor, 1f))
            {
                visualRoot.localScale *= uniformFactor;
            }

            if (adapter != null && !Mathf.Approximately(uniformFactor, 1f))
            {
                ScaleGameplayMarkerIfIndependent(adapter.muzzleTransform, visualRoot, uniformFactor);
                ScaleGameplayMarkerIfIndependent(adapter.abilityOrigin, visualRoot, uniformFactor);
                ScaleGameplayMarkerIfIndependent(adapter.impactPoint, visualRoot, uniformFactor);
            }

            Transform presentation = instance.transform.Find(PresentationRootName);
            if (presentation != null && !Mathf.Approximately(uniformFactor, 1f))
            {
                presentation.localScale *= uniformFactor;
            }

            ProceduralAnimator proceduralAnimator = instance.GetComponent<ProceduralAnimator>();
            if (proceduralAnimator != null)
            {
                proceduralAnimator.CaptureCurrentScaleAsBase();
            }
        }

        private static void ScaleGameplayMarkerIfIndependent(Transform marker, Transform visualRoot, float uniformFactor)
        {
            if (marker == null)
            {
                return;
            }

            if (visualRoot != null && marker.IsChildOf(visualRoot))
            {
                return;
            }

            marker.localPosition *= uniformFactor;
        }

        private static bool NameLooksLikeNonCharacterFx(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            for (int i = 0; i < FxNameTokens.Length; i++)
            {
                if (name.IndexOf(FxNameTokens[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsUnityBuiltinPrimitive(MeshRenderer meshRenderer)
        {
            MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                return false;
            }

            string meshName = filter.sharedMesh.name;
            return meshName == "Cube" || meshName == "Sphere" || meshName == "Cylinder"
                || meshName == "Capsule" || meshName == "Plane" || meshName == "Quad";
        }

        private static float MaxComponent(Vector3 value)
        {
            return Mathf.Max(Mathf.Abs(value.x), Mathf.Max(Mathf.Abs(value.y), Mathf.Abs(value.z)));
        }

        private static int CompareTransformDepth(Transform a, Transform b)
        {
            return GetDepth(a).CompareTo(GetDepth(b));
        }

        private static int GetDepth(Transform transform)
        {
            int depth = 0;
            Transform current = transform;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private static Transform FindTransformRecursive(Transform parent, string name)
        {
            if (parent.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindTransformRecursive(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
