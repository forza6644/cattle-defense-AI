using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class CardPoolPlayModeTests
    {
        [UnityTest]
        public IEnumerator RealDraftManager_QualifiesOverrideRecruitCapsAndRestartClearing()
        {
            SaveManager.SetSelectedStartingDefender("archer");
            AsyncOperation load = SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
            while (!load.isDone) yield return null;

            float deadline = Time.realtimeSinceStartup + 15f;
            while ((CardDraftManager.Instance == null || HeroRosterManager.Instance == null || RunModifierManager.Instance == null)
                   && Time.realtimeSinceStartup < deadline) yield return null;

            CardDraftManager draft = CardDraftManager.Instance;
            HeroRosterManager roster = HeroRosterManager.Instance;
            RunModifierManager modifiers = RunModifierManager.Instance;
            Assert.That(draft, Is.Not.Null);
            Assert.That(roster, Is.Not.Null);
            Assert.That(modifiers, Is.Not.Null);
            roster.InitializeRunRoster();
            Assert.That(roster.IsHeroOwned("archer"), Is.True);

            var created = new List<Object>();
            CardPoolDefinition pool = ScriptableObject.CreateInstance<CardPoolDefinition>();
            created.Add(pool);
            pool.stableId = "playmode_vertical_slice";
            pool.displayName = "PlayMode Vertical Slice";
            pool.description = "Injected qualification pool";
            pool.startingHeroId = "archer";
            pool.expectedCardCount = 18;
            pool.recruitOptionPolicy = RecruitOptionPolicy.GuaranteeWhileAvailable;

            CardDefinition[] production = Resources.LoadAll<CardDefinition>("Cards");
            string[] reusable = { "recruit_bombardier", "recruit_frost_mage", "recruit_electric_engineer", "war_training", "battle_rhythm", "watchtower_expansion", "fast_casting", "empowered_abilities", "frostbite", "wide_blast" };
            foreach (string id in reusable)
            {
                CardDefinition card = production.Single(x => x.id == id);
                pool.cards.Add(new CardPoolEntry { card = card, rarity = CardRarity.Common, weight = card.cardCategory == CardCategory.RecruitHero ? 2f : 1f });
            }

            AddUpgrade(pool, created, "archer_twin_volley", "archer", HeroBehaviorEffectType.ExtraProjectile, 2);
            AddUpgrade(pool, created, "archer_piercing_arrows", "archer", HeroBehaviorEffectType.Piercing, 2);
            AddUpgrade(pool, created, "bombardier_cluster_shells", "bombardier", HeroBehaviorEffectType.SplitProjectile, 2);
            AddUpgrade(pool, created, "bombardier_wide_blast", "bombardier", HeroBehaviorEffectType.ExplosionRadius, 3);
            AddUpgrade(pool, created, "frost_mage_shard_volley", "frost_mage", HeroBehaviorEffectType.ExtraProjectile, 2);
            AddUpgrade(pool, created, "frost_mage_echoing_nova", "frost_mage", HeroBehaviorEffectType.ExtraCast, 1);
            AddUpgrade(pool, created, "electric_engineer_extended_circuit", "electric_engineer", HeroBehaviorEffectType.ExtraChain, 3);
            AddUpgrade(pool, created, "electric_engineer_forked_current", "electric_engineer", HeroBehaviorEffectType.Ricochet, 2);

            draft.SetPoolOverrideForQualification(pool);
            List<DraftCardChoice> initial = draft.GenerateDraftChoices(1234);
            Assert.That(initial.Count, Is.EqualTo(3));
            Assert.That(initial.Any(x => x.Card.cardCategory == CardCategory.RecruitHero), Is.True);
            Assert.That(initial.All(x => pool.cards.Any(entry => entry.card == x.Card)), Is.True);
            Assert.That(initial.Select(x => x.Card.id).Distinct().Count(), Is.EqualTo(3));

            MethodInfo apply = typeof(CardDraftManager).GetMethod("ApplyCard", BindingFlags.Instance | BindingFlags.NonPublic);
            CardDefinition recruitBombardier = pool.cards.Single(x => x.card.id == "recruit_bombardier").card;
            apply.Invoke(draft, new object[] { recruitBombardier });
            Assert.That(roster.IsHeroOwned("bombardier"), Is.True);
            Assert.That(CardDraftSelector.IsEligible(recruitBombardier, BuildState(roster, modifiers, pool)), Is.False);

            CardDefinition cluster = pool.cards.Single(x => x.card.id == "bombardier_cluster_shells").card;
            Assert.That(CardDraftSelector.IsEligible(cluster, BuildState(roster, modifiers, pool)), Is.True);
            apply.Invoke(draft, new object[] { cluster });
            apply.Invoke(draft, new object[] { cluster });
            int activeBeforeReject = modifiers.ActiveCards.Count;
            apply.Invoke(draft, new object[] { cluster });
            Assert.That(modifiers.GetBehaviorStacks("bombardier", HeroBehaviorEffectType.SplitProjectile), Is.EqualTo(2));
            Assert.That(modifiers.ActiveCards.Count, Is.EqualTo(activeBeforeReject));
            Assert.That(CardDraftSelector.IsEligible(cluster, BuildState(roster, modifiers, pool)), Is.False);

            modifiers.ClearModifiers();
            draft.ResetForRun();
            Assert.That(modifiers.ActiveCards, Is.Empty);
            Assert.That(modifiers.GetBehaviorStacks("bombardier", HeroBehaviorEffectType.SplitProjectile), Is.Zero);

            draft.SetPoolOverrideForQualification(null);
            foreach (Object item in created) if (item != null) Object.Destroy(item);
            AsyncOperation cleanup = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            while (!cleanup.isDone) yield return null;
            yield return null;
        }

        private static DraftSelectionState BuildState(HeroRosterManager roster, RunModifierManager modifiers, CardPoolDefinition pool)
        {
            var attacks = new List<AttackType>();
            roster.CopyOwnedAttackTypesTo(attacks);
            var stacks = new Dictionary<string, int>();
            foreach (CardPoolEntry entry in pool.cards) stacks[entry.card.id] = modifiers.GetCardStackCount(entry.card.id);
            return new DraftSelectionState(roster.OwnedHeroIds, attacks, roster.EmptySlotCount, stacks);
        }

        private static void AddUpgrade(CardPoolDefinition pool, ICollection<Object> created, string id, string heroId, HeroBehaviorEffectType effect, int maxStacks)
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = id;
            card.displayName = id;
            card.description = "PlayMode qualification behavior upgrade.";
            card.cardCategory = CardCategory.HeroUpgrade;
            card.targetType = CardTargetType.HeroById;
            card.targetHeroId = heroId;
            card.weight = 1f;
            card.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = effect,
                targetType = CardTargetType.HeroById,
                targetHeroId = heroId,
                count = 1,
                maxStacks = maxStacks
            };
            created.Add(card);
            pool.cards.Add(new CardPoolEntry { card = card, rarity = CardRarity.Rare, weight = 1f });
        }
    }
}
