using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class HeroVisualScalePlayModeTests
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

        [UnityTest]
        public IEnumerator AllTenHeroes_AfterAnimationUpdate_StayInsideGameplayEnvelope()
        {
            HeroDefinition[] loaded = Resources.LoadAll<HeroDefinition>("Heroes");
            if (loaded == null || loaded.Length < 10)
            {
                loaded = Resources.LoadAll<HeroDefinition>("");
            }

            GameObject slotGO = new GameObject("HeroSlot_PlayModeScale", typeof(HeroSlot));
            HeroSlot slot = slotGO.GetComponent<HeroSlot>();
            float archerHeight = 0f;

            try
            {
                foreach (string heroId in ExpectedHeroIds)
                {
                    HeroDefinition hero = loaded.First(h => h.id == heroId);
                    Assert.IsTrue(slot.SpawnHero(hero), $"Failed to spawn '{heroId}'.");
                    yield return null;
                    yield return null;

                    HeroAttack spawned = slot.CurrentHero;
                    Bounds bounds = HeroGameplayVisualNormalizer.MeasureGameplayBounds(spawned.transform);
                    if (heroId == "archer")
                    {
                        archerHeight = bounds.size.y;
                    }

                    Assert.LessOrEqual(bounds.size.y, HeroGameplayVisualNormalizer.MaxGameplayHeight + 0.15f,
                        $"Hero '{heroId}' height {bounds.size.y:0.00} exceeds the portrait gameplay envelope.");
                    Assert.LessOrEqual(bounds.size.x, 2.05f,
                        $"Hero '{heroId}' still occupies too much battlement width.");

                    ArtAdapter adapter = spawned.GetComponent<ArtAdapter>();
                    Assert.IsNotNull(adapter.muzzleTransform);
                    Assert.Greater(adapter.muzzleTransform.position.y, spawned.transform.position.y + 0.25f);
                    Assert.Less(adapter.muzzleTransform.position.y, spawned.transform.position.y + 2.4f);

                    slot.ClearHero();
                    yield return null;
                }
            }
            finally
            {
                Object.DestroyImmediate(slotGO);
            }

            Assert.Greater(archerHeight, 1.1f);
        }
    }
}
