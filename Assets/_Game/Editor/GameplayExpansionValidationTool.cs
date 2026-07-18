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
            CardPoolDefinition[] cardPools = LoadAssets<CardPoolDefinition>();
            StageData[] stages = LoadAssets<StageData>();

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
            foreach (CardPoolDefinition pool in cardPools)
            {
                issues.AddRange(GameplayDataValidation.ValidateCardPool(pool));
            }
            ValidateTask13EIsolation(enemies, issues);
            ValidateTask13FIsolation(cards, traps, defenses, issues);
            StageData expansionStage = stages.FirstOrDefault(stage => stage != null && stage.stageId == ExpansionRunValidation.StageId);
            if (expansionStage != null)
            {
                issues.AddRange(ExpansionRunValidation.Validate(expansionStage));
                ValidateTask13GIsolation(expansionStage, issues);
            }

            Debug.Log(BuildSummary(cards, enemies, traps.Length, defenses.Length, castleUpgrades.Length, rerolls.Length, cardPools.Length, issues));
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

        private static void ValidateTask13GIsolation(StageData stage, List<GameplayValidationIssue> issues)
        {
            CardDefinition[] productionCards = Resources.LoadAll<CardDefinition>("Cards");
            if (productionCards.Length != 39)
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13g.production-cards", "Production Resources/Cards must remain at 39 cards.", stage));
            CardPoolDefinition vertical = AssetDatabase.LoadAssetAtPath<CardPoolDefinition>("Assets/_Game/ScriptableObjects/CardPools/VerticalSlice18.asset");
            if (vertical == null || vertical.cards == null || vertical.cards.Count != 18)
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13g.vertical-slice", "VerticalSlice18 must remain at 18 entries.", vertical));
            string stagePath = AssetDatabase.GetAssetPath(stage);
            if (!stagePath.StartsWith(ExpansionRunQualificationBuilder.Root, StringComparison.Ordinal))
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13g.stage-isolation", "Expansion stage must remain in its qualification root.", stage));
            if (stage.cardPoolOverride != null && AssetDatabase.GetAssetPath(stage.cardPoolOverride).Contains("Resources/Cards"))
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13g.pool-isolation", "Expansion pool cannot live under Resources/Cards.", stage.cardPoolOverride));
        }

        private static string BuildSummary(
            CardDefinition[] cards,
            EnemyData[] enemies,
            int trapCount,
            int defenseCount,
            int castleUpgradeCount,
            int rerollCount,
            int cardPoolCount,
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
                + $"CastleUpgrades={castleUpgradeCount}; Rerolls={rerollCount}; CardPools={cardPoolCount}; Errors={errors}; Warnings={warnings}.";
        }

        [MenuItem("Stonehold/Validation/Build VerticalSlice18 Card Pool")]
        public static void BuildVerticalSlice18()
        {
            const string folder = "Assets/_Game/ScriptableObjects/CardPools";
            const string path = folder + "/VerticalSlice18.asset";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Game/ScriptableObjects", "CardPools");

            CardPoolDefinition pool = AssetDatabase.LoadAssetAtPath<CardPoolDefinition>(path);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<CardPoolDefinition>();
                AssetDatabase.CreateAsset(pool, path);
            }

            pool.stableId = "vertical_slice_18";
            pool.displayName = "Vertical Slice 18";
            pool.description = "Controlled four-hero qualification pool for the ten-wave vertical slice.";
            pool.startingHeroId = "archer";
            pool.supportedHeroIds = new[] { "archer", "bombardier", "frost_mage", "electric_engineer" };
            pool.expectedCardCount = 18;
            pool.recruitOptionPolicy = RecruitOptionPolicy.GuaranteeWhileAvailable;
            pool.allowedCategories = new[] { CardCategory.Modifier, CardCategory.RecruitHero, CardCategory.HeroUpgrade };
            pool.allowedRarities = new[] { CardRarity.Common, CardRarity.Rare, CardRarity.Epic };
            pool.cards = new List<CardPoolEntry>
            {
                Entry("Assets/_Game/Resources/Cards/AddBombardier.asset", CardRarity.Common, 2f),
                Entry("Assets/_Game/Resources/Cards/AddFrostMage.asset", CardRarity.Common, 2f),
                Entry("Assets/_Game/Resources/Cards/AddElectricEngineer.asset", CardRarity.Common, 2f),
                Entry("Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/TwinVolley.asset", CardRarity.Common, 1.2f),
                Entry("Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/PiercingArrows.asset", CardRarity.Rare, 0.8f),
                Entry("Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/ClusterShells.asset", CardRarity.Rare, 0.8f),
                Entry("Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/WideBlast.asset", CardRarity.Common, 1.2f),
                Entry("Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/ShardVolley.asset", CardRarity.Rare, 0.8f),
                Entry("Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/EchoingNova.asset", CardRarity.Epic, 0.35f),
                Entry("Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/ExtendedCircuit.asset", CardRarity.Rare, 0.8f),
                Entry("Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades/ForkedCurrent.asset", CardRarity.Epic, 0.35f),
                Entry("Assets/_Game/Resources/Cards/WarTraining.asset", CardRarity.Common, 1.2f),
                Entry("Assets/_Game/Resources/Cards/BattleRhythm.asset", CardRarity.Common, 1.2f),
                Entry("Assets/_Game/Resources/Cards/WatchtowerExpansion.asset", CardRarity.Common, 1.2f),
                Entry("Assets/_Game/Resources/Cards/FastCasting.asset", CardRarity.Common, 1.2f),
                Entry("Assets/_Game/Resources/Cards/EmpoweredAbilities.asset", CardRarity.Common, 1.2f),
                Entry("Assets/_Game/Resources/Cards/Frostbite.asset", CardRarity.Rare, 0.8f),
                Entry("Assets/_Game/Resources/Cards/WideBlast.asset", CardRarity.Rare, 0.8f)
            };

            EditorUtility.SetDirty(pool);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = pool;
            Debug.Log($"[CardPool] Built {pool.stableId} with {pool.cards.Count} explicit entries.", pool);
        }

        private static CardPoolEntry Entry(string path, CardRarity rarity, float weight)
        {
            CardDefinition card = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
            if (card == null) throw new InvalidOperationException("Missing card asset: " + path);
            return new CardPoolEntry { card = card, rarity = rarity, weight = weight };
        }

        private static void ValidateTask13EIsolation(EnemyData[] enemies, List<GameplayValidationIssue> issues)
        {
            EnemyData raider = enemies.FirstOrDefault(enemy => enemy != null && enemy.stableId == EnemyRosterExpansionBuilder.RaiderId);
            EnemyData shaman = enemies.FirstOrDefault(enemy => enemy != null && enemy.stableId == EnemyRosterExpansionBuilder.ShamanId);
            if (raider == null && shaman == null) return;
            if (raider == null) issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13e.raider.missing", "Task 13E requires Crossbow Raider data."));
            if (shaman == null) issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13e.shaman.missing", "Task 13E requires War Shaman data."));

            string[] expansionIds = { EnemyRosterExpansionBuilder.RaiderId, EnemyRosterExpansionBuilder.ShamanId };
            WaveData[] waves = LoadAssets<WaveData>();
            for (int i = 0; i < waves.Length; i++)
            {
                WaveData wave = waves[i];
                string wavePath = wave == null ? string.Empty : AssetDatabase.GetAssetPath(wave).Replace('\\', '/');
                if (wave == null || wave.spawns == null || wavePath == EnemyRosterExpansionBuilder.QualificationWavePath || wavePath.StartsWith(ExpansionRunQualificationBuilder.Root + "/", StringComparison.Ordinal)) continue;
                for (int j = 0; j < wave.spawns.Length; j++)
                {
                    EnemyData enemy = wave.spawns[j].enemy;
                    if (enemy != null && expansionIds.Contains(enemy.stableId))
                    {
                        issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13e.production-wave", $"'{enemy.stableId}' must remain outside production wave '{wave.name}'.", wave));
                    }
                }
            }
        }

        private static void ValidateTask13FIsolation(CardDefinition[] cards, TrapDefinition[] traps, BattlefieldDefenseDefinition[] defenses, List<GameplayValidationIssue> issues)
        {
            TrapDefinition caltrops = traps.FirstOrDefault(item => item != null && item.stableId == "trap_caltrops");
            TrapDefinition oil = traps.FirstOrDefault(item => item != null && item.stableId == "trap_burning_oil");
            BattlefieldDefenseDefinition barricade = defenses.FirstOrDefault(item => item != null && item.stableId == "defense_wooden_barricade");
            bool any = caltrops != null || oil != null || barricade != null;
            if (!any) return;
            if (traps.Count(item => item != null && AssetDatabase.GetAssetPath(item).StartsWith(BattlefieldDefenseQualificationBuilder.Root, StringComparison.Ordinal)) != 2)
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13f.trap-count", "Task 13F requires exactly two isolated trap definitions."));
            if (defenses.Count(item => item != null && AssetDatabase.GetAssetPath(item).StartsWith(BattlefieldDefenseQualificationBuilder.Root, StringComparison.Ordinal)) != 1)
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13f.defense-count", "Task 13F requires exactly one isolated battlefield defense."));
            if (caltrops == null || oil == null || barricade == null)
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13f.definition-missing", "Task 13F qualification definitions are incomplete."));

            string[] ids = { "deploy_caltrops", "deploy_burning_oil", "deploy_wooden_barricade" };
            for (int i = 0; i < ids.Length; i++)
            {
                CardDefinition card = cards.FirstOrDefault(item => item != null && item.id == ids[i]);
                if (card == null) { issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13f.card-missing", $"Missing qualification card '{ids[i]}'.")); continue; }
                string path = AssetDatabase.GetAssetPath(card).Replace('\\', '/');
                if (path.Contains("/Resources/Cards/")) issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "task13f.production-card", $"'{ids[i]}' must remain outside Resources/Cards.", card));
            }
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
