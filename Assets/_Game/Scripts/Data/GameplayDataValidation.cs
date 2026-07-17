using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public enum ValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public readonly struct GameplayValidationIssue
    {
        public GameplayValidationIssue(ValidationSeverity severity, string code, string message, UnityEngine.Object context = null)
        {
            Severity = severity;
            Code = code;
            Message = message;
            Context = context;
        }

        public ValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public UnityEngine.Object Context { get; }
    }

    public static class GameplayDataValidation
    {
        public static List<GameplayValidationIssue> ValidateCards(
            IEnumerable<CardDefinition> cards,
            ISet<string> knownHeroIds = null)
        {
            List<GameplayValidationIssue> issues = new List<GameplayValidationIssue>();
            Dictionary<string, CardDefinition> ids = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);

            if (cards == null)
            {
                issues.Add(Error("cards.null", "Card collection is null."));
                return issues;
            }

            foreach (CardDefinition card in cards)
            {
                ValidateCard(card, issues, knownHeroIds);
                if (card == null || string.IsNullOrWhiteSpace(card.id))
                {
                    continue;
                }

                if (ids.TryGetValue(card.id, out CardDefinition duplicate))
                {
                    issues.Add(Error("card.id.duplicate", $"Duplicate card ID '{card.id}' on '{duplicate.name}' and '{card.name}'.", card));
                }
                else
                {
                    ids.Add(card.id, card);
                }
            }

            return issues;
        }

        public static List<GameplayValidationIssue> ValidateCard(CardDefinition card, ISet<string> knownHeroIds = null)
        {
            List<GameplayValidationIssue> issues = new List<GameplayValidationIssue>();
            ValidateCard(card, issues, knownHeroIds);
            return issues;
        }

        public static List<GameplayValidationIssue> ValidateHeroBehavior(HeroBehaviorUpgradeData data)
        {
            List<GameplayValidationIssue> issues = new List<GameplayValidationIssue>();
            if (data == null)
            {
                issues.Add(Error("hero-upgrade.missing", "Hero upgrade execution data is missing."));
                return issues;
            }

            if (!Enum.IsDefined(typeof(HeroBehaviorEffectType), data.effectType) || data.effectType == HeroBehaviorEffectType.None)
            {
                issues.Add(Error("hero-upgrade.effect", "Hero upgrade requires a known behavior effect."));
            }
            if (!Enum.IsDefined(typeof(CardTargetType), data.targetType))
            {
                issues.Add(Error("hero-upgrade.target-type", "Hero upgrade target type is invalid."));
            }
            if (data.targetType == CardTargetType.HeroById && string.IsNullOrWhiteSpace(data.targetHeroId))
            {
                issues.Add(Error("hero-upgrade.target-hero", "Hero-specific upgrade requires a target hero ID."));
            }
            if (data.integerValue < 0 || data.count < 0 || data.duration < 0f || data.maxStacks < 1
                || !IsFiniteNonNegative(data.floatValue) || !IsFiniteNonNegative(data.secondaryValue)
                || !IsFiniteInRange(data.percentageValue, 0f, 1f))
            {
                issues.Add(Error("hero-upgrade.values", "Hero upgrade values must be finite, non-negative, and max stacks must be at least one."));
            }
            if (data.integerValue == 0 && data.count == 0 && data.floatValue == 0f
                && data.secondaryValue == 0f && data.percentageValue == 0f && data.duration == 0f)
            {
                issues.Add(Error("hero-upgrade.magnitude", "Hero upgrade must define at least one behavior value."));
            }
            return issues;
        }

        public static List<GameplayValidationIssue> ValidateBattlefieldContent(BattlefieldContentDefinition definition)
        {
            List<GameplayValidationIssue> issues = new List<GameplayValidationIssue>();
            if (definition == null)
            {
                issues.Add(Error("battlefield.missing", "Battlefield content definition is missing."));
                return issues;
            }

            ValidateIdentity(definition.stableId, definition.displayName, definition.description, "battlefield", definition, issues);
            if (definition.prefab == null)
            {
                issues.Add(Error("battlefield.prefab", $"'{definition.name}' requires a prefab reference.", definition));
            }
            if (!Enum.IsDefined(typeof(PlacementMode), definition.placementMode))
            {
                issues.Add(Error("battlefield.placement", $"'{definition.name}' has an invalid placement mode.", definition));
            }
            if (!IsFiniteNonNegative(definition.duration) || definition.charges < 0
                || !IsFiniteNonNegative(definition.damage) || !IsFiniteNonNegative(definition.effectRadius)
                || !IsFiniteNonNegative(definition.triggerInterval) || !IsFiniteNonNegative(definition.statusEffectDuration)
                || !IsFinite(definition.statusEffectValue))
            {
                issues.Add(Error("battlefield.values", $"'{definition.name}' contains invalid numeric values.", definition));
            }
            return issues;
        }

        public static List<GameplayValidationIssue> ValidateBattlefieldContents(IEnumerable<BattlefieldContentDefinition> definitions)
        {
            return ValidateDefinitionCollection(
                definitions,
                ValidateBattlefieldContent,
                definition => definition.stableId,
                "battlefield.id.duplicate");
        }

        public static List<GameplayValidationIssue> ValidateCastleUpgrade(CastleUpgradeDefinition definition)
        {
            List<GameplayValidationIssue> issues = new List<GameplayValidationIssue>();
            if (definition == null)
            {
                issues.Add(Error("castle-upgrade.missing", "Castle upgrade definition is missing."));
                return issues;
            }
            ValidateIdentity(definition.stableId, definition.displayName, definition.description, "castle-upgrade", definition, issues);
            if (!IsFinite(definition.value) || definition.maxStacks < 1)
            {
                issues.Add(Error("castle-upgrade.values", $"'{definition.name}' has invalid value or max stacks.", definition));
            }
            return issues;
        }

        public static List<GameplayValidationIssue> ValidateCastleUpgrades(IEnumerable<CastleUpgradeDefinition> definitions)
        {
            return ValidateDefinitionCollection(
                definitions,
                ValidateCastleUpgrade,
                definition => definition.stableId,
                "castle-upgrade.id.duplicate");
        }

        public static List<GameplayValidationIssue> ValidateReroll(RerollDefinition definition)
        {
            List<GameplayValidationIssue> issues = new List<GameplayValidationIssue>();
            if (definition == null)
            {
                issues.Add(Error("reroll.missing", "Reroll definition is missing."));
                return issues;
            }
            ValidateIdentity(definition.stableId, definition.displayName, definition.description, "reroll", definition, issues);
            if (definition.baseCost < 0 || definition.costIncreasePerUse < 0 || definition.maxUsesPerDraft < 1)
            {
                issues.Add(Error("reroll.values", $"'{definition.name}' has invalid cost or usage values.", definition));
            }
            return issues;
        }

        public static List<GameplayValidationIssue> ValidateRerolls(IEnumerable<RerollDefinition> definitions)
        {
            return ValidateDefinitionCollection(
                definitions,
                ValidateReroll,
                definition => definition.stableId,
                "reroll.id.duplicate");
        }

        public static List<GameplayValidationIssue> ValidateEnemies(IEnumerable<EnemyData> enemies)
        {
            List<GameplayValidationIssue> issues = new List<GameplayValidationIssue>();
            Dictionary<string, EnemyData> ids = new Dictionary<string, EnemyData>(StringComparer.Ordinal);
            if (enemies == null)
            {
                issues.Add(Error("enemies.null", "Enemy collection is null."));
                return issues;
            }

            foreach (EnemyData enemy in enemies)
            {
                issues.AddRange(ValidateEnemy(enemy));
                if (enemy == null || string.IsNullOrWhiteSpace(enemy.stableId))
                {
                    continue;
                }
                if (ids.ContainsKey(enemy.stableId))
                {
                    issues.Add(Error("enemy.id.duplicate", $"Duplicate enemy ID '{enemy.stableId}'.", enemy));
                }
                else
                {
                    ids.Add(enemy.stableId, enemy);
                }
            }
            return issues;
        }

        public static List<GameplayValidationIssue> ValidateEnemy(EnemyData enemy)
        {
            List<GameplayValidationIssue> issues = new List<GameplayValidationIssue>();
            if (enemy == null)
            {
                issues.Add(Error("enemy.missing", "Enemy definition is missing."));
                return issues;
            }
            if (string.IsNullOrWhiteSpace(enemy.stableId))
            {
                issues.Add(Error("enemy.id.missing", $"'{enemy.name}' requires a stable ID.", enemy));
            }
            if (string.IsNullOrWhiteSpace(enemy.enemyName))
            {
                issues.Add(Error("enemy.name.missing", $"'{enemy.name}' requires a display name.", enemy));
            }
            if (!Enum.IsDefined(typeof(EnemyClassification), enemy.classification))
            {
                issues.Add(Error("enemy.classification", $"'{enemy.name}' has an invalid classification.", enemy));
            }
            if (!IsFiniteNonNegative(enemy.armor) || !IsFiniteNonNegative(enemy.shieldCapacity)
                || !IsFiniteInRange(enemy.dodgeChance, 0f, 1f)
                || !IsFiniteInRange(enemy.crowdControlResistance, 0f, 1f))
            {
                issues.Add(Error("enemy.defense-range", $"'{enemy.name}' has defense values outside supported ranges.", enemy));
            }
            ValidateResistance(enemy.elementalResistances.physical, "physical", enemy, issues);
            ValidateResistance(enemy.elementalResistances.fire, "fire", enemy, issues);
            ValidateResistance(enemy.elementalResistances.frost, "frost", enemy, issues);
            ValidateResistance(enemy.elementalResistances.electric, "electric", enemy, issues);
            ValidateResistance(enemy.elementalResistances.explosive, "explosive", enemy, issues);
            return issues;
        }

        public static bool HasErrors(IEnumerable<GameplayValidationIssue> issues)
        {
            if (issues == null) return false;
            foreach (GameplayValidationIssue issue in issues)
            {
                if (issue.Severity == ValidationSeverity.Error) return true;
            }
            return false;
        }

        private static void ValidateCard(CardDefinition card, List<GameplayValidationIssue> issues, ISet<string> knownHeroIds)
        {
            if (card == null)
            {
                issues.Add(Error("card.missing", "Card definition is missing."));
                return;
            }
            ValidateIdentity(card.id, card.displayName, card.description, "card", card, issues);
            if (!Enum.IsDefined(typeof(CardCategory), card.cardCategory))
            {
                issues.Add(Error("card.category", $"'{card.name}' has an unknown category value.", card));
                return;
            }
            if (!Enum.IsDefined(typeof(CardRarity), card.rarity))
            {
                issues.Add(Error("card.rarity", $"'{card.name}' has an unknown rarity value.", card));
            }
            if (!IsFinite(card.weight) || card.weight <= 0f)
            {
                issues.Add(Error("card.weight", $"'{card.name}' requires a positive finite draft weight.", card));
            }
            if (card.targetType == CardTargetType.HeroById && string.IsNullOrWhiteSpace(card.targetHeroId)
                && card.cardCategory != CardCategory.RecruitHero)
            {
                issues.Add(Error("card.target-hero", $"'{card.name}' requires a target hero ID.", card));
            }
            if (knownHeroIds != null && card.targetType == CardTargetType.HeroById
                && !string.IsNullOrWhiteSpace(card.targetHeroId) && !knownHeroIds.Contains(card.targetHeroId))
            {
                issues.Add(Error("card.target-hero.unknown", $"'{card.name}' targets unknown hero '{card.targetHeroId}'.", card));
            }

            switch (card.cardCategory)
            {
                case CardCategory.Modifier:
                    if (!IsFinite(card.modifierValue)) issues.Add(Error("card.modifier", $"'{card.name}' has an invalid modifier value.", card));
                    issues.Add(Warning("card.legacy-modifier", $"'{card.name}' uses the supported legacy Modifier category.", card));
                    break;
                case CardCategory.RecruitHero:
                    if (string.IsNullOrWhiteSpace(card.recruitHeroId)) issues.Add(Error("card.recruit", $"'{card.name}' requires a recruit hero ID.", card));
                    break;
                case CardCategory.HeroUpgrade:
                    Append(issues, ValidateHeroBehavior(card.behaviorUpgrade), card);
                    break;
                case CardCategory.GlobalUpgrade:
                    if (!IsFinite(card.modifierValue)) issues.Add(Error("card.global-upgrade", $"'{card.name}' has an invalid modifier value.", card));
                    break;
                case CardCategory.Trap:
                    if (card.trapDefinition == null) issues.Add(Error("card.trap", $"'{card.name}' requires trap execution data.", card));
                    else Append(issues, ValidateBattlefieldContent(card.trapDefinition), card);
                    break;
                case CardCategory.BattlefieldDefense:
                    if (card.battlefieldDefenseDefinition == null) issues.Add(Error("card.defense", $"'{card.name}' requires defense execution data.", card));
                    else Append(issues, ValidateBattlefieldContent(card.battlefieldDefenseDefinition), card);
                    break;
                case CardCategory.CastleUpgrade:
                    if (card.castleUpgradeDefinition == null) issues.Add(Error("card.castle-upgrade", $"'{card.name}' requires castle-upgrade execution data.", card));
                    else Append(issues, ValidateCastleUpgrade(card.castleUpgradeDefinition), card);
                    break;
                case CardCategory.LegendaryModifier:
                    if (card.rarity != CardRarity.Legendary) issues.Add(Error("card.legendary-rarity", $"'{card.name}' must use Legendary rarity.", card));
                    if (!IsFinite(card.modifierValue)) issues.Add(Error("card.legendary-value", $"'{card.name}' has an invalid modifier value.", card));
                    break;
                case CardCategory.Reroll:
                    if (card.rerollDefinition == null) issues.Add(Error("card.reroll", $"'{card.name}' requires reroll execution data.", card));
                    else Append(issues, ValidateReroll(card.rerollDefinition), card);
                    break;
            }
        }

        private static void ValidateIdentity(string id, string title, string description, string prefix, UnityEngine.Object context, List<GameplayValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(id)) issues.Add(Error(prefix + ".id.missing", $"'{context.name}' requires a stable ID.", context));
            if (string.IsNullOrWhiteSpace(title)) issues.Add(Error(prefix + ".title.missing", $"'{context.name}' requires a display title.", context));
            if (string.IsNullOrWhiteSpace(description)) issues.Add(Error(prefix + ".description.missing", $"'{context.name}' requires a description.", context));
        }

        private static void ValidateResistance(float value, string name, EnemyData context, List<GameplayValidationIssue> issues)
        {
            if (!IsFiniteInRange(value, 0f, 1f))
            {
                issues.Add(Error("enemy.resistance-range", $"'{context.name}' has invalid {name} resistance; values must remain between zero and one.", context));
            }
        }

        private static void Append(List<GameplayValidationIssue> destination, IEnumerable<GameplayValidationIssue> source, UnityEngine.Object fallbackContext)
        {
            foreach (GameplayValidationIssue issue in source)
            {
                destination.Add(new GameplayValidationIssue(issue.Severity, issue.Code, issue.Message, issue.Context != null ? issue.Context : fallbackContext));
            }
        }

        private static List<GameplayValidationIssue> ValidateDefinitionCollection<T>(
            IEnumerable<T> definitions,
            Func<T, List<GameplayValidationIssue>> validate,
            Func<T, string> getId,
            string duplicateCode) where T : UnityEngine.Object
        {
            List<GameplayValidationIssue> issues = new List<GameplayValidationIssue>();
            Dictionary<string, T> ids = new Dictionary<string, T>(StringComparer.Ordinal);
            if (definitions == null)
            {
                issues.Add(Error("definitions.null", "Definition collection is null."));
                return issues;
            }

            foreach (T definition in definitions)
            {
                issues.AddRange(validate(definition));
                if (definition == null)
                {
                    continue;
                }

                string id = getId(definition);
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }
                if (ids.ContainsKey(id))
                {
                    issues.Add(Error(duplicateCode, $"Duplicate stable ID '{id}'.", definition));
                }
                else
                {
                    ids.Add(id, definition);
                }
            }
            return issues;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool IsFiniteNonNegative(float value) => IsFinite(value) && value >= 0f;
        private static bool IsFiniteInRange(float value, float min, float max) => IsFinite(value) && value >= min && value <= max;
        private static GameplayValidationIssue Error(string code, string message, UnityEngine.Object context = null) => new GameplayValidationIssue(ValidationSeverity.Error, code, message, context);
        private static GameplayValidationIssue Warning(string code, string message, UnityEngine.Object context = null) => new GameplayValidationIssue(ValidationSeverity.Warning, code, message, context);
    }
}
