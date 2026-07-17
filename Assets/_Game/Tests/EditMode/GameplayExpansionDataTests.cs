using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Tests
{
    public class GameplayExpansionDataTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null) Object.DestroyImmediate(createdObjects[i]);
            }
            createdObjects.Clear();
        }

        [Test]
        public void CardCategory_LegacyNumericValuesRemainStable()
        {
            Assert.That((int)CardCategory.Modifier, Is.EqualTo(0));
            Assert.That((int)CardCategory.RecruitHero, Is.EqualTo(1));
            Assert.That((int)CardCategory.HeroUpgrade, Is.EqualTo(2));
            Assert.That((int)CardCategory.Reroll, Is.EqualTo(8));
        }

        [Test]
        public void CardRarity_LegacyNumericValuesRemainStable()
        {
            Assert.That((int)CardRarity.Common, Is.EqualTo(0));
            Assert.That((int)CardRarity.Rare, Is.EqualTo(1));
            Assert.That((int)CardRarity.Epic, Is.EqualTo(2));
        }

        [Test]
        public void CardRarity_LegendaryIsAppended()
        {
            Assert.That((int)CardRarity.Legendary, Is.EqualTo(3));
        }

        [Test]
        public void ExistingCards_AllThirtyNineLoadWithoutCategoryCorruption()
        {
            CardDefinition[] cards = Resources.LoadAll<CardDefinition>("Cards");

            Assert.That(cards, Has.Length.EqualTo(39));
            Assert.That(cards.All(card => card.cardCategory == CardCategory.Modifier || card.cardCategory == CardCategory.RecruitHero), Is.True);
        }

        [Test]
        public void ExistingCards_StableIdsArePresentAndUnique()
        {
            CardDefinition[] cards = Resources.LoadAll<CardDefinition>("Cards");
            string[] ids = cards.Select(card => card.id).ToArray();

            Assert.That(ids.All(id => !string.IsNullOrWhiteSpace(id)), Is.True);
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
        }

        [Test]
        public void ExistingCards_DoNotBecomeLegendaryAutomatically()
        {
            CardDefinition[] cards = Resources.LoadAll<CardDefinition>("Cards");

            Assert.That(cards.Any(card => card.rarity == CardRarity.Legendary), Is.False);
        }

        [Test]
        public void ExistingModifierCards_ValidateWithoutErrors()
        {
            CardDefinition[] cards = Resources.LoadAll<CardDefinition>("Cards");
            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateCards(cards);

            Assert.That(GameplayDataValidation.HasErrors(issues), Is.False, FormatIssues(issues));
            Assert.That(issues.Count(issue => issue.Code == "card.legacy-modifier"), Is.GreaterThan(0));
        }

        [Test]
        public void DuplicateCardIds_AreReported()
        {
            CardDefinition first = CreateCard("duplicate");
            CardDefinition second = CreateCard("duplicate");

            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateCards(new[] { first, second });

            Assert.That(issues.Any(issue => issue.Code == "card.id.duplicate"), Is.True);
        }

        [Test]
        public void UnknownCardCategory_IsRejected()
        {
            CardDefinition card = CreateCard("unknown_category");
            card.cardCategory = (CardCategory)999;

            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateCard(card);

            Assert.That(issues.Any(issue => issue.Code == "card.category"), Is.True);
        }

        [Test]
        public void LegendaryModifier_RequiresLegendaryRarity()
        {
            CardDefinition card = CreateCard("legendary_test");
            card.cardCategory = CardCategory.LegendaryModifier;
            card.rarity = CardRarity.Epic;

            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateCard(card);

            Assert.That(issues.Any(issue => issue.Code == "card.legendary-rarity"), Is.True);
        }

        [Test]
        public void HeroBehaviorUpgrade_ValidDefinitionPasses()
        {
            HeroBehaviorUpgradeData upgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.Multishot,
                targetType = CardTargetType.HeroById,
                targetHeroId = "archer",
                count = 1,
                maxStacks = 3
            };

            Assert.That(GameplayDataValidation.HasErrors(GameplayDataValidation.ValidateHeroBehavior(upgrade)), Is.False);
        }

        [Test]
        public void HeroBehaviorUpgrade_InvalidValuesFail()
        {
            HeroBehaviorUpgradeData upgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.Piercing,
                targetType = CardTargetType.HeroById,
                targetHeroId = string.Empty,
                percentageValue = -0.1f,
                maxStacks = 0
            };

            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateHeroBehavior(upgrade);

            Assert.That(GameplayDataValidation.HasErrors(issues), Is.True);
            Assert.That(issues.Any(issue => issue.Code == "hero-upgrade.target-hero"), Is.True);
            Assert.That(issues.Any(issue => issue.Code == "hero-upgrade.values"), Is.True);
        }

        [Test]
        public void HeroUpgradeCard_RequiresExecutionData()
        {
            CardDefinition card = CreateCard("hero_upgrade_missing");
            card.cardCategory = CardCategory.HeroUpgrade;

            Assert.That(GameplayDataValidation.HasErrors(GameplayDataValidation.ValidateCard(card)), Is.True);
        }

        [Test]
        public void TrapDefinition_MissingIdIsRejected()
        {
            TrapDefinition trap = Create<TrapDefinition>();
            trap.displayName = "Caltrops";
            trap.description = "Damages enemies crossing the lane.";
            trap.prefab = CreateGameObject("Trap Prefab");

            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateBattlefieldContent(trap);

            Assert.That(issues.Any(issue => issue.Code == "battlefield.id.missing"), Is.True);
        }

        [Test]
        public void PlacementMode_RoundTripsThroughUnitySerialization()
        {
            TrapDefinition source = Create<TrapDefinition>();
            source.placementMode = PlacementMode.CastleFront;
            TrapDefinition copy = Create<TrapDefinition>();

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), copy);

            Assert.That(copy.placementMode, Is.EqualTo(PlacementMode.CastleFront));
        }

        [Test]
        public void BattlefieldDefense_MissingPrefabIsRejected()
        {
            BattlefieldDefenseDefinition defense = Create<BattlefieldDefenseDefinition>();
            defense.stableId = "wooden_barricade";
            defense.displayName = "Wooden Barricade";
            defense.description = "Temporarily blocks the castle front.";

            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateBattlefieldContent(defense);

            Assert.That(issues.Any(issue => issue.Code == "battlefield.prefab"), Is.True);
        }

        [Test]
        public void BattlefieldContent_DuplicateStableIdsAreRejected()
        {
            TrapDefinition trap = CreateValidTrap("shared_lane_item");
            BattlefieldDefenseDefinition defense = Create<BattlefieldDefenseDefinition>();
            defense.stableId = "shared_lane_item";
            defense.displayName = "Barricade";
            defense.description = "Blocks the castle front.";
            defense.prefab = CreateGameObject("Defense Prefab");

            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateBattlefieldContents(
                new BattlefieldContentDefinition[] { trap, defense });

            Assert.That(issues.Any(issue => issue.Code == "battlefield.id.duplicate"), Is.True);
        }

        [Test]
        public void CastleUpgrade_ValidContractPasses()
        {
            CastleUpgradeDefinition upgrade = Create<CastleUpgradeDefinition>();
            upgrade.stableId = "castle_max_health";
            upgrade.displayName = "Reinforced Walls";
            upgrade.description = "Increases castle maximum health.";
            upgrade.value = 5f;
            upgrade.maxStacks = 5;

            Assert.That(GameplayDataValidation.HasErrors(GameplayDataValidation.ValidateCastleUpgrade(upgrade)), Is.False);
        }

        [Test]
        public void Reroll_NegativeCostIsRejected()
        {
            RerollDefinition reroll = Create<RerollDefinition>();
            reroll.stableId = "standard_reroll";
            reroll.displayName = "Reroll";
            reroll.description = "Replaces the current draft choices.";
            reroll.baseCost = -1;

            Assert.That(GameplayDataValidation.HasErrors(GameplayDataValidation.ValidateReroll(reroll)), Is.True);
        }

        [Test]
        public void EnemyDefaults_PreserveLegacyDefenseBehavior()
        {
            EnemyData enemy = Create<EnemyData>();

            Assert.That(enemy.classification, Is.EqualTo(EnemyClassification.Normal));
            Assert.That(enemy.armor, Is.Zero);
            Assert.That(enemy.shieldCapacity, Is.Zero);
            Assert.That(enemy.dodgeChance, Is.Zero);
            Assert.That(enemy.crowdControlResistance, Is.Zero);
            Assert.That(enemy.elementalResistances.Get(DamageType.Fire), Is.Zero);
        }

        [Test]
        public void EnemyDefenseValues_OutsideRangesAreRejected()
        {
            EnemyData enemy = Create<EnemyData>();
            enemy.stableId = "invalid_enemy";
            enemy.enemyName = "Invalid Enemy";
            enemy.armor = -1f;
            enemy.shieldCapacity = -2f;
            enemy.dodgeChance = 1.1f;
            enemy.crowdControlResistance = -0.1f;
            enemy.elementalResistances = new ElementalResistanceProfile { fire = 1.1f };

            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateEnemy(enemy);

            Assert.That(issues.Any(issue => issue.Code == "enemy.defense-range"), Is.True);
            Assert.That(issues.Any(issue => issue.Code == "enemy.resistance-range"), Is.True);
        }

        [Test]
        public void ExistingEnemyAssets_LoadWithSafeDefenseValues()
        {
            EnemyData[] enemies = LoadEnemyAssets();
            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateEnemies(enemies);

            Assert.That(enemies, Has.Length.EqualTo(5));
            Assert.That(GameplayDataValidation.HasErrors(issues), Is.False, FormatIssues(issues));
            Assert.That(enemies.All(enemy => enemy.shieldCapacity == 0f && enemy.dodgeChance == 0f), Is.True);
        }

        [Test]
        public void Warlord_IsBossWithoutBalanceChanges()
        {
            EnemyData warlord = LoadEnemyAssets().Single(enemy => enemy.stableId == "warlord_boss");

            Assert.That(warlord.classification, Is.EqualTo(EnemyClassification.Boss));
            Assert.That(warlord.health, Is.EqualTo(450f));
            Assert.That(warlord.moveSpeed, Is.EqualTo(1.2f));
            Assert.That(warlord.armor, Is.EqualTo(8f));
            Assert.That(warlord.castleDamage, Is.EqualTo(10));
        }

        [Test]
        public void DamageContext_CarriesFutureResolutionData()
        {
            DamageContext context = new DamageContext(25f, DamageType.Electric, "electric_engineer", true, 0.5f);

            Assert.That(context.RawDamage, Is.EqualTo(25f));
            Assert.That(context.DamageType, Is.EqualTo(DamageType.Electric));
            Assert.That(context.SourceHeroId, Is.EqualTo("electric_engineer"));
            Assert.That(context.IsCritical, Is.True);
            Assert.That(context.ArmorPiercing, Is.EqualTo(0.5f));
        }

        [Test]
        public void SaveManager_CurrentVersionRemainsTwo()
        {
            FieldInfo field = typeof(SaveManager).GetField("CurrentSaveVersion", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            Assert.That(field.GetRawConstantValue(), Is.EqualTo(2));
        }

        [Test]
        public void RunModifierManager_BehaviorUpgrade_AppliesAndEnforcesMaxStacks()
        {
            if (RunModifierManager.Instance == null)
            {
                var go = new GameObject("RunModifierManager");
                var manager = go.AddComponent<RunModifierManager>();
                typeof(RunModifierManager).GetProperty("Instance").SetValue(null, manager);
            }
            RunModifierManager.Instance.ClearModifiers();

            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "test_upgrade";
            card.displayName = "Test Upgrade";
            card.cardCategory = CardCategory.HeroUpgrade;
            card.targetType = CardTargetType.HeroById;
            card.targetHeroId = "archer";
            card.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.ExtraProjectile,
                targetType = CardTargetType.HeroById,
                targetHeroId = "archer",
                count = 1,
                maxStacks = 2
            };

            RunModifierManager.Instance.AddCard(card);
            Assert.That(RunModifierManager.Instance.GetBehaviorStacks("archer", HeroBehaviorEffectType.ExtraProjectile), Is.EqualTo(1));
            Assert.That(RunModifierManager.Instance.GetBehaviorCount("archer", HeroBehaviorEffectType.ExtraProjectile), Is.EqualTo(1));
            Assert.That(RunModifierManager.Instance.HasBehavior("archer", HeroBehaviorEffectType.ExtraProjectile), Is.True);

            RunModifierManager.Instance.AddCard(card);
            Assert.That(RunModifierManager.Instance.GetBehaviorStacks("archer", HeroBehaviorEffectType.ExtraProjectile), Is.EqualTo(2));

            RunModifierManager.Instance.AddCard(card);
            Assert.That(RunModifierManager.Instance.GetBehaviorStacks("archer", HeroBehaviorEffectType.ExtraProjectile), Is.EqualTo(2));
            Assert.That(RunModifierManager.Instance.ActiveCards, Has.Count.EqualTo(2), "A rejected stack must not be recorded as an active card.");

            RunModifierManager.Instance.ClearModifiers();
            Assert.That(RunModifierManager.Instance.GetBehaviorStacks("archer", HeroBehaviorEffectType.ExtraProjectile), Is.EqualTo(0));
            Assert.That(RunModifierManager.Instance.HasBehavior("archer", HeroBehaviorEffectType.ExtraProjectile), Is.False);
        }

        [Test]
        public void RunModifierManager_MultipleBehaviors_Coexist()
        {
            if (RunModifierManager.Instance == null)
            {
                var go = new GameObject("RunModifierManager");
                var manager = go.AddComponent<RunModifierManager>();
                typeof(RunModifierManager).GetProperty("Instance").SetValue(null, manager);
            }
            RunModifierManager.Instance.ClearModifiers();

            CardDefinition card1 = ScriptableObject.CreateInstance<CardDefinition>();
            card1.id = "test_extra";
            card1.cardCategory = CardCategory.HeroUpgrade;
            card1.targetType = CardTargetType.HeroById;
            card1.targetHeroId = "archer";
            card1.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.ExtraProjectile,
                targetType = CardTargetType.HeroById,
                targetHeroId = "archer",
                count = 1,
                maxStacks = 2
            };

            CardDefinition card2 = ScriptableObject.CreateInstance<CardDefinition>();
            card2.id = "test_pierce";
            card2.cardCategory = CardCategory.HeroUpgrade;
            card2.targetType = CardTargetType.HeroById;
            card2.targetHeroId = "archer";
            card2.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.Piercing,
                targetType = CardTargetType.HeroById,
                targetHeroId = "archer",
                count = 1,
                maxStacks = 2
            };

            RunModifierManager.Instance.AddCard(card1);
            RunModifierManager.Instance.AddCard(card2);

            Assert.That(RunModifierManager.Instance.HasBehavior("archer", HeroBehaviorEffectType.ExtraProjectile), Is.True);
            Assert.That(RunModifierManager.Instance.HasBehavior("archer", HeroBehaviorEffectType.Piercing), Is.True);

            RunModifierManager.Instance.ClearModifiers();
        }

        [Test]
        public void Resources_NormalDraftPool_ExcludesPrototypeCards()
        {
            var cards = Resources.LoadAll<CardDefinition>("Cards");
            Assert.That(cards.Length, Is.EqualTo(39));

            foreach (var card in cards)
            {
                Assert.That(card.id, Is.Not.EqualTo("archer_twin_volley"));
                Assert.That(card.id, Is.Not.EqualTo("archer_piercing_arrows"));
                Assert.That(card.id, Is.Not.EqualTo("bombardier_cluster_shells"));
                Assert.That(card.id, Is.Not.EqualTo("bombardier_wide_blast"));
                Assert.That(card.id, Is.Not.EqualTo("frost_mage_shard_volley"));
                Assert.That(card.id, Is.Not.EqualTo("frost_mage_echoing_nova"));
                Assert.That(card.id, Is.Not.EqualTo("electric_engineer_extended_circuit"));
                Assert.That(card.id, Is.Not.EqualTo("electric_engineer_forked_current"));
            }
        }

        [Test]
        public void HeroUpgradePrototypes_ExactlyEightHaveExpectedContracts()
        {
            CardDefinition[] cards = AssetDatabase.FindAssets(
                    "t:CardDefinition",
                    new[] { "Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardDefinition>)
                .Where(card => card != null)
                .ToArray();

            string[] expectedIds =
            {
                "archer_twin_volley",
                "archer_piercing_arrows",
                "bombardier_cluster_shells",
                "bombardier_wide_blast",
                "frost_mage_shard_volley",
                "frost_mage_echoing_nova",
                "electric_engineer_extended_circuit",
                "electric_engineer_forked_current"
            };

            Assert.That(cards, Has.Length.EqualTo(8));
            Assert.That(cards.Select(card => card.id), Is.EquivalentTo(expectedIds));
            Assert.That(cards.All(card => card.cardCategory == CardCategory.HeroUpgrade), Is.True);
            Assert.That(cards.All(card => card.behaviorUpgrade != null && card.behaviorUpgrade.maxStacks > 0), Is.True);
            Assert.That(GameplayDataValidation.HasErrors(GameplayDataValidation.ValidateCards(cards)), Is.False);
        }

        [Test]
        public void HeroUpgradePrototypes_AllEightCanCoexistInControlledRunState()
        {
            if (RunModifierManager.Instance == null)
            {
                var go = new GameObject("RunModifierManager");
                var manager = go.AddComponent<RunModifierManager>();
                createdObjects.Add(go);
                typeof(RunModifierManager).GetProperty("Instance").SetValue(null, manager);
            }
            RunModifierManager.Instance.ClearModifiers();

            CardDefinition[] cards = AssetDatabase.FindAssets(
                    "t:CardDefinition",
                    new[] { "Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardDefinition>)
                .Where(card => card != null)
                .OrderBy(card => card.id)
                .ToArray();

            foreach (CardDefinition card in cards)
            {
                RunModifierManager.Instance.AddCard(card);
            }

            Assert.That(RunModifierManager.Instance.ActiveCards, Has.Count.EqualTo(8));
            foreach (CardDefinition card in cards)
            {
                Assert.That(
                    RunModifierManager.Instance.HasBehavior(card.targetHeroId, card.behaviorUpgrade.effectType),
                    Is.True,
                    card.id);
            }
        }

        private CardDefinition CreateCard(string id)
        {
            CardDefinition card = Create<CardDefinition>();
            card.id = id;
            card.displayName = "Test Card";
            card.description = "Test card description.";
            card.cardCategory = CardCategory.Modifier;
            card.modifierValue = 0.1f;
            card.weight = 1f;
            return card;
        }

        private TrapDefinition CreateValidTrap(string id)
        {
            TrapDefinition trap = Create<TrapDefinition>();
            trap.stableId = id;
            trap.displayName = "Caltrops";
            trap.description = "Damages enemies crossing the lane.";
            trap.prefab = CreateGameObject("Trap Prefab");
            return trap;
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(instance);
            return instance;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject instance = new GameObject(name);
            createdObjects.Add(instance);
            return instance;
        }

        private static EnemyData[] LoadEnemyAssets()
        {
            return AssetDatabase.FindAssets("t:EnemyData", new[] { "Assets/_Game/ScriptableObjects/Enemies" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EnemyData>)
                .Where(enemy => enemy != null)
                .ToArray();
        }

        private static string FormatIssues(IEnumerable<GameplayValidationIssue> issues)
        {
            return string.Join("\n", issues.Select(issue => $"{issue.Severity} {issue.Code}: {issue.Message}"));
        }
    }
}
