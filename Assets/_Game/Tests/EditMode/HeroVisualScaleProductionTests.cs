using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    public class HeroVisualScaleProductionTests
    {
        private static readonly string[] ExpectedHeroIds =
        {
            "archer",
            "bombardier",
            "frost_mage",
            "fire_mage",
            "electric_engineer",
            "sniper",
            "plague_doctor",
            "radiant_paladin",
            "shadow_assassin",
            "storm_druid"
        };

        private static readonly string[] NewlyIntegratedHeroIds =
        {
            "radiant_paladin",
            "shadow_assassin",
            "storm_druid"
        };

        [Test]
        public void MenuShowcaseScale_OnPrefabs_IsIndependentOfGameplayEnvelope()
        {
            foreach (HeroDefinition hero in LoadRoster())
            {
                ArtAdapter adapter = hero.heroPrefab.GetComponent<ArtAdapter>();
                Assert.IsNotNull(adapter, $"Hero '{hero.id}' prefab is missing ArtAdapter.");
                Assert.AreEqual(adapter.visualScale, adapter.MenuShowcaseScale,
                    $"Hero '{hero.id}' MenuShowcaseScale must remain the authored visualScale.");
                Assert.Greater(adapter.visualScale.x, 0.5f, $"Hero '{hero.id}' menu scale was cleared.");
            }

            HeroDefinition paladin = LoadRoster().First(h => h.id == "radiant_paladin");
            ArtAdapter paladinAdapter = paladin.heroPrefab.GetComponent<ArtAdapter>();
            Assert.Greater(paladinAdapter.visualScale.x, 1.05f,
                "Radiant Paladin menu showcase scale must stay larger than gameplay envelope scale.");
        }

        [Test]
        public void GameplayCalibrationMultiplier_TreatsZeroAsIdentity()
        {
            HeroDefinition dummy = ScriptableObject.CreateInstance<HeroDefinition>();
            dummy.gameplayVisualScaleMultiplier = 0f;
            Assert.AreEqual(1f, HeroGameplayVisualNormalizer.ResolveCalibrationMultiplier(dummy));
            dummy.gameplayVisualScaleMultiplier = 0.92f;
            Assert.AreEqual(0.92f, HeroGameplayVisualNormalizer.ResolveCalibrationMultiplier(dummy));
            Object.DestroyImmediate(dummy);
        }

        [Test]
        public void AllTenHeroes_GameplayRendererBounds_StayInsidePortraitEnvelope()
        {
            Dictionary<string, HeroGameplayVisualNormalizer.VisualMetrics> metricsById =
                new Dictionary<string, HeroGameplayVisualNormalizer.VisualMetrics>();

            GameObject slotGO = new GameObject("HeroSlot_VisualScale", typeof(HeroSlot));
            HeroSlot slot = slotGO.GetComponent<HeroSlot>();
            try
            {
                foreach (HeroDefinition hero in LoadRoster())
                {
                    Assert.IsTrue(slot.SpawnHero(hero), $"Failed to spawn '{hero.id}'.");
                    HeroAttack spawned = slot.CurrentHero;
                    Assert.IsNotNull(spawned);

                    ArtAdapter adapter = spawned.GetComponent<ArtAdapter>();
                    Assert.IsNotNull(adapter);
                    Assert.IsNotNull(adapter.muzzleTransform);
                    Assert.IsNotNull(adapter.abilityOrigin);
                    Assert.IsNotNull(adapter.impactPoint);

                    HeroGameplayVisualNormalizer.VisualMetrics metrics =
                        new HeroGameplayVisualNormalizer.VisualMetrics(
                            HeroGameplayVisualNormalizer.MeasureGameplayBounds(spawned.transform),
                            adapter.visualRoot != null ? adapter.visualRoot.localScale.x : 1f,
                            true);
                    metricsById[hero.id] = metrics;
                    TestContext.WriteLine(
                        $"{hero.id}: gameplayVisualRoot={metrics.AppliedUniformScale:0.000} menuScale={adapter.visualScale.x:0.000} height={metrics.Height:0.00} width={metrics.LaneWidth:0.00} depth={metrics.Depth:0.00} calib={HeroGameplayVisualNormalizer.ResolveCalibrationMultiplier(hero):0.00}");

                    Assert.LessOrEqual(metrics.Height, HeroGameplayVisualNormalizer.MaxGameplayHeight + 0.20f,
                        $"Hero '{hero.id}' height {metrics.Height:0.00} exceeds the gameplay envelope.");
                    Assert.LessOrEqual(metrics.LaneWidth, 2.50f,
                        $"Hero '{hero.id}' lane width {metrics.LaneWidth:0.00} still dominates neighboring slots.");

                    AssertBuiltinPropsAreNotArmatureOversized(spawned.transform, hero.id);
                    AssertGameplayMarkersTrackVisual(spawned.transform, adapter);

                    ProceduralAnimator animator = spawned.GetComponent<ProceduralAnimator>();
                    Assert.IsNotNull(animator, $"Hero '{hero.id}' is missing ProceduralAnimator.");

                    slot.ClearHero();
                }
            }
            finally
            {
                Object.DestroyImmediate(slotGO);
            }

            Assert.AreEqual(ExpectedHeroIds.Length, metricsById.Count);
            float archerHeight = metricsById["archer"].Height;
            Assert.Greater(archerHeight, 1.1f, "Archer benchmark height should be a readable character, not a stub.");

            foreach (string heroId in NewlyIntegratedHeroIds)
            {
                float height = metricsById[heroId].Height;
                Assert.LessOrEqual(height, archerHeight * 1.22f,
                    $"Hero '{heroId}' height {height:0.00} still dominates Archer benchmark {archerHeight:0.00}.");
            }
        }

        [Test]
        public void WorstCaseHeroStack_SixSlotsRemainSeparated()
        {
            string[] stackIds =
            {
                "radiant_paladin",
                "storm_druid",
                "shadow_assassin",
                "archer",
                "bombardier",
                "frost_mage"
            };

            Vector3[] slotPositions =
            {
                new Vector3(-5f, 2.75f, -0.6f),
                new Vector3(-3.33f, 3.15f, -0.2f),
                new Vector3(-1.67f, 3.55f, 0.2f),
                new Vector3(1.67f, 3.55f, 0.2f),
                new Vector3(3.33f, 3.15f, -0.2f),
                new Vector3(5f, 2.75f, -0.6f)
            };

            List<GameObject> slots = new List<GameObject>();
            try
            {
                HeroDefinition[] roster = LoadRoster();
                for (int i = 0; i < stackIds.Length; i++)
                {
                    GameObject slotGO = new GameObject($"HeroSlot_Stack_{i}", typeof(HeroSlot));
                    slotGO.transform.position = slotPositions[i];
                    slots.Add(slotGO);

                    HeroDefinition hero = roster.First(h => h.id == stackIds[i]);
                    HeroSlot slot = slotGO.GetComponent<HeroSlot>();
                    Assert.IsTrue(slot.SpawnHero(hero), $"Failed to spawn stack hero '{hero.id}'.");

                    Bounds bounds = HeroGameplayVisualNormalizer.MeasureGameplayBounds(slot.CurrentHero.transform);
                    Assert.LessOrEqual(bounds.size.y, HeroGameplayVisualNormalizer.MaxGameplayHeight + 0.20f,
                        $"Stacked '{hero.id}' still covers too much of the portrait combat lane.");
                }

                for (int i = 0; i < slots.Count; i++)
                {
                    for (int j = i + 1; j < slots.Count; j++)
                    {
                        float distance = Mathf.Abs(slots[i].transform.position.x - slots[j].transform.position.x);
                        if (distance < 1.2f)
                        {
                            continue;
                        }

                        Bounds a = HeroGameplayVisualNormalizer.MeasureGameplayBounds(
                            slots[i].GetComponent<HeroSlot>().CurrentHero.transform);
                        Bounds b = HeroGameplayVisualNormalizer.MeasureGameplayBounds(
                            slots[j].GetComponent<HeroSlot>().CurrentHero.transform);
                        float overlapX = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
                        Assert.Less(overlapX, 1.25f,
                            $"{stackIds[i]} and {stackIds[j]} overlap too much on the battlement ({overlapX:0.00}m).");
                    }
                }
            }
            finally
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    Object.DestroyImmediate(slots[i]);
                }
            }
        }

        private static void AssertBuiltinPropsAreNotArmatureOversized(Transform heroRoot, string heroId)
        {
            MeshRenderer[] renderers = heroRoot.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshFilter filter = renderers[i].GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                string meshName = filter.sharedMesh.name;
                if (meshName != "Cube" && meshName != "Sphere" && meshName != "Cylinder" && meshName != "Capsule")
                {
                    continue;
                }

                float lossy = Mathf.Max(
                    Mathf.Abs(renderers[i].transform.lossyScale.x),
                    Mathf.Max(
                        Mathf.Abs(renderers[i].transform.lossyScale.y),
                        Mathf.Abs(renderers[i].transform.lossyScale.z)));
                Assert.Less(lossy, 3.5f,
                    $"Hero '{heroId}' builtin prop '{renderers[i].name}' is still armature-oversized (lossy {lossy:0.00}).");
            }
        }

        private static void AssertGameplayMarkersTrackVisual(Transform heroRoot, ArtAdapter adapter)
        {
            AssertMarkerOnCharacter(heroRoot, adapter.muzzleTransform, "Muzzle");
            AssertMarkerOnCharacter(heroRoot, adapter.abilityOrigin, "AbilityOrigin");
            AssertMarkerOnCharacter(heroRoot, adapter.impactPoint, "ImpactPoint");
        }

        private static void AssertMarkerOnCharacter(Transform heroRoot, Transform marker, string label)
        {
            Assert.IsNotNull(marker, $"{heroRoot.name} is missing {label}.");
            Vector3 local = heroRoot.InverseTransformPoint(marker.position);
            Assert.Greater(local.y, 0.12f, $"{heroRoot.name} {label} is too low after gameplay scale.");
            Assert.Less(local.y, 2.45f, $"{heroRoot.name} {label} floated off the silhouette after gameplay scale.");
            Assert.Less(Mathf.Abs(local.x), 1.6f, $"{heroRoot.name} {label} drifted too far sideways.");
        }

        private static HeroDefinition[] LoadRoster()
        {
            HeroDefinition[] loaded = Resources.LoadAll<HeroDefinition>("Heroes");
            if (loaded == null || loaded.Length < 10)
            {
                loaded = Resources.LoadAll<HeroDefinition>("");
            }

            HeroDefinition[] roster = loaded.Where(h => ExpectedHeroIds.Contains(h.id)).ToArray();
            Assert.AreEqual(ExpectedHeroIds.Length, roster.Length, "Expected all 10 production heroes.");
            return roster.OrderBy(h => System.Array.IndexOf(ExpectedHeroIds, h.id)).ToArray();
        }
    }
}
