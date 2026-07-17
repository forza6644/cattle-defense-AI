using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Editor
{
    public static class GameplayExpansionValidationTool
    {
        [MenuItem("Stonehold/Validation/Validate Gameplay Expansion Data")]
        public static void ValidateProjectData()
        {
            CardDefinition[] cards = LoadAssets<CardDefinition>();
            EnemyData[] enemies = LoadAssets<EnemyData>();
            HeroDefinition[] heroes = LoadAssets<HeroDefinition>();
            TrapDefinition[] traps = LoadAssets<TrapDefinition>();
            BattlefieldDefenseDefinition[] defenses = LoadAssets<BattlefieldDefenseDefinition>();
            CastleUpgradeDefinition[] castleUpgrades = LoadAssets<CastleUpgradeDefinition>();
            RerollDefinition[] rerolls = LoadAssets<RerollDefinition>();

            HashSet<string> heroIds = new HashSet<string>(
                heroes.Where(hero => hero != null && !string.IsNullOrWhiteSpace(hero.id)).Select(hero => hero.id),
                StringComparer.Ordinal);

            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateCards(cards, heroIds);
            issues.AddRange(GameplayDataValidation.ValidateEnemies(enemies));
            BattlefieldContentDefinition[] battlefieldContent = traps.Cast<BattlefieldContentDefinition>()
                .Concat(defenses)
                .ToArray();
            issues.AddRange(GameplayDataValidation.ValidateBattlefieldContents(battlefieldContent));
            issues.AddRange(GameplayDataValidation.ValidateCastleUpgrades(castleUpgrades));
            issues.AddRange(GameplayDataValidation.ValidateRerolls(rerolls));

            Debug.Log(BuildSummary(cards, enemies, traps.Length, defenses.Length, castleUpgrades.Length, rerolls.Length, issues));
            foreach (GameplayValidationIssue issue in issues)
            {
                string message = $"[GameplayData:{issue.Code}] {issue.Message}";
                switch (issue.Severity)
                {
                    case ValidationSeverity.Error:
                        Debug.LogError(message, issue.Context);
                        break;
                    case ValidationSeverity.Warning:
                        Debug.LogWarning(message, issue.Context);
                        break;
                    default:
                        Debug.Log(message, issue.Context);
                        break;
                }
            }
        }

        private static string BuildSummary(
            CardDefinition[] cards,
            EnemyData[] enemies,
            int trapCount,
            int defenseCount,
            int castleUpgradeCount,
            int rerollCount,
            List<GameplayValidationIssue> issues)
        {
            string categoryCounts = string.Join(", ", Enum.GetValues(typeof(CardCategory)).Cast<CardCategory>()
                .Select(category => $"{category}={cards.Count(card => card != null && card.cardCategory == category)}"));
            string rarityCounts = string.Join(", ", Enum.GetValues(typeof(CardRarity)).Cast<CardRarity>()
                .Select(rarity => $"{rarity}={cards.Count(card => card != null && card.rarity == rarity)}"));
            string enemyCounts = string.Join(", ", Enum.GetValues(typeof(EnemyClassification)).Cast<EnemyClassification>()
                .Select(classification => $"{classification}={enemies.Count(enemy => enemy != null && enemy.classification == classification)}"));
            int errors = issues.Count(issue => issue.Severity == ValidationSeverity.Error);
            int warnings = issues.Count(issue => issue.Severity == ValidationSeverity.Warning);

            return "[GameplayData] Read-only validation complete. "
                + $"Cards={cards.Length} ({categoryCounts}); Rarity=({rarityCounts}); "
                + $"Enemies={enemies.Length} ({enemyCounts}); Traps={trapCount}; Defenses={defenseCount}; "
                + $"CastleUpgrades={castleUpgradeCount}; Rerolls={rerollCount}; Errors={errors}; Warnings={warnings}.";
        }

        private static T[] LoadAssets<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets("t:" + typeof(T).Name)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .ToArray();
        }
    }
}
