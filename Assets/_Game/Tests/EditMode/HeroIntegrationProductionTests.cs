using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Stonehold.Tests
{
    public class HeroIntegrationProductionTests
    {
        private static readonly string[] ExpectedHeroIds = new string[]
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

        [Test]
        public void AllTenHeroes_LoadedFromResourcesOrAssets_CountIsAtLeastTen()
        {
            HeroDefinition[] loaded = Resources.LoadAll<HeroDefinition>("Heroes");
            if (loaded == null || loaded.Length < 10)
            {
                loaded = Resources.LoadAll<HeroDefinition>("");
            }

            Assert.IsNotNull(loaded);
            Assert.IsTrue(loaded.Length >= 10, $"Expected at least 10 HeroDefinitions, found {loaded.Length}");

            var ids = loaded.Select(h => h.id).ToHashSet();
            foreach (string expectedId in ExpectedHeroIds)
            {
                Assert.IsTrue(ids.Contains(expectedId), $"Missing expected HeroDefinition id: {expectedId}");
            }
        }

        [Test]
        public void AllTenHeroes_HaveDedicatedAdapterPrefabs()
        {
            HeroDefinition[] loaded = Resources.LoadAll<HeroDefinition>("Heroes");
            if (loaded == null || loaded.Length < 10)
            {
                loaded = Resources.LoadAll<HeroDefinition>("");
            }

            foreach (var hero in loaded)
            {
                if (!ExpectedHeroIds.Contains(hero.id)) continue;

                Assert.IsNotNull(hero.heroPrefab, $"Hero '{hero.id}' has null heroPrefab!");
                ArtAdapter adapter = hero.heroPrefab.GetComponent<ArtAdapter>();
                Assert.IsNotNull(adapter, $"Hero '{hero.id}' prefab '{hero.heroPrefab.name}' is missing ArtAdapter component!");
                Assert.IsNotNull(adapter.muzzleTransform, $"Hero '{hero.id}' ArtAdapter has null muzzleTransform!");
                Assert.IsNotNull(adapter.abilityOrigin, $"Hero '{hero.id}' ArtAdapter has null abilityOrigin!");
                Assert.IsNotNull(adapter.impactPoint, $"Hero '{hero.id}' ArtAdapter has null impactPoint!");
            }
        }

        [Test]
        public void AllTenHeroes_HaveValidWeaponDefinitions()
        {
            HeroDefinition[] loaded = Resources.LoadAll<HeroDefinition>("Heroes");
            if (loaded == null || loaded.Length < 10)
            {
                loaded = Resources.LoadAll<HeroDefinition>("");
            }

            foreach (var hero in loaded)
            {
                if (!ExpectedHeroIds.Contains(hero.id)) continue;

                Assert.IsNotNull(hero.weapon, $"Hero '{hero.id}' has null weapon!");
                Assert.IsTrue(hero.baseDamage > 0f, $"Hero '{hero.id}' base damage must be > 0");
                Assert.IsTrue(hero.baseFireRate > 0f, $"Hero '{hero.id}' base fire rate must be > 0");
                Assert.IsTrue(hero.baseRange > 0f, $"Hero '{hero.id}' base range must be > 0");
            }
        }

        [Test]
        public void AllTenHeroes_HaveDistinctSignatureAbilities()
        {
            HeroDefinition[] loaded = Resources.LoadAll<HeroDefinition>("Heroes");
            if (loaded == null || loaded.Length < 10)
            {
                loaded = Resources.LoadAll<HeroDefinition>("");
            }

            HashSet<HeroAbilityType> abilities = new HashSet<HeroAbilityType>();
            foreach (var hero in loaded)
            {
                if (!ExpectedHeroIds.Contains(hero.id)) continue;

                Assert.AreNotEqual(HeroAbilityType.None, hero.abilityType, $"Hero '{hero.id}' must have a valid signature ability!");
                Assert.IsTrue(hero.abilityCooldown > 0f, $"Hero '{hero.id}' ability cooldown must be > 0");
                abilities.Add(hero.abilityType);
            }

            Assert.AreEqual(10, abilities.Count, "All 10 heroes must have distinct HeroAbilityType values!");
        }

        [Test]
        public void HeroRosterManager_AutoDiscoversAllTenHeroes()
        {
            GameObject go = new GameObject("HeroRosterManager_Test", typeof(HeroRosterManager));
            GameObject slotGO = new GameObject("HeroSlot_Test", typeof(HeroSlot));
            try
            {
                HeroRosterManager roster = go.GetComponent<HeroRosterManager>();
                HeroSlot slot = slotGO.GetComponent<HeroSlot>();
                roster.RegisterSlot(slot);
                roster.InitializeRunRoster();

                foreach (string heroId in ExpectedHeroIds)
                {
                    Assert.IsTrue(roster.CanRecruit(heroId), $"HeroRosterManager should be able to recruit discovered hero '{heroId}'");
                }
            }
            finally
            {
                Object.DestroyImmediate(slotGO);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HeroSlot_SpawnsEveryHeroWithValidVisualPresentation()
        {
            HeroDefinition[] loaded = Resources.LoadAll<HeroDefinition>("Heroes");
            if (loaded == null || loaded.Length < 10)
            {
                loaded = Resources.LoadAll<HeroDefinition>("");
            }

            GameObject slotGO = new GameObject("HeroSlot_Test", typeof(HeroSlot));
            HeroSlot slot = slotGO.GetComponent<HeroSlot>();

            try
            {
                foreach (string heroId in ExpectedHeroIds)
                {
                    HeroDefinition hero = loaded.FirstOrDefault(h => h.id == heroId);
                    Assert.IsNotNull(hero, $"Hero definition for '{heroId}' not found!");

                    bool spawned = slot.SpawnHero(hero);
                    Assert.IsTrue(spawned, $"HeroSlot failed to spawn hero '{heroId}'");
                    Assert.IsTrue(slot.IsOccupied, $"HeroSlot should be occupied after spawning '{heroId}'");
                    Assert.IsNotNull(slot.CurrentHero, $"HeroSlot.CurrentHero should not be null for '{heroId}'");

                    // Check presentation child
                    Transform presentation = slot.transform.Find("HeroPresentation") ?? slot.CurrentHero.transform.Find("HeroPresentation");
                    Assert.IsNotNull(presentation, $"Hero '{heroId}' must have a HeroPresentation transform child!");
                    Assert.IsTrue(presentation.childCount > 0, $"Hero '{heroId}' HeroPresentation must contain presentation pieces!");

                    slot.ClearHero();
                    Assert.IsFalse(slot.IsOccupied, $"HeroSlot should be empty after ClearHero for '{heroId}'");
                }
            }
            finally
            {
                Object.DestroyImmediate(slotGO);
            }
        }
    }
}
