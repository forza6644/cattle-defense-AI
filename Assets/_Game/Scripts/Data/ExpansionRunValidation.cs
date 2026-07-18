using System;
using System.Collections.Generic;
using System.Linq;

namespace Stonehold
{
    public static class ExpansionRunValidation
    {
        public const string StageId = "expansion_vertical_slice_01";
        public const string PoolId = "expansion_run_20";

        public static List<GameplayValidationIssue> Validate(StageData stage)
        {
            var issues = new List<GameplayValidationIssue>();
            if (stage == null)
            {
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.stage.missing", "Expansion stage is missing."));
                return issues;
            }
            if (!string.Equals(stage.stageId, StageId, StringComparison.Ordinal))
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.stage.id", "Expansion stage stable ID is invalid.", stage));
            if (stage.waves == null || stage.waves.Length != 10)
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.stage.waves", "Expansion stage requires exactly ten waves.", stage));
            else
            {
                for (int i = 0; i < stage.waves.Length; i++)
                {
                    WaveData wave = stage.waves[i];
                    if (wave == null) { issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.wave.missing", $"Wave {i + 1} is missing.", stage)); continue; }
                    if (wave.spawns == null || wave.spawns.Length == 0) issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.wave.empty", $"Wave {i + 1} is empty.", wave));
                    int shamans = 0, bosses = 0;
                    foreach (WaveData.SpawnEntry entry in wave.spawns ?? Array.Empty<WaveData.SpawnEntry>())
                    {
                        if (entry.enemy == null || entry.count <= 0 || !FinitePositive(entry.spawnInterval) || !FiniteNonNegative(entry.startDelay))
                            issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.wave.entry", $"Wave {i + 1} contains invalid spawn data.", wave));
                        if (entry.enemy != null && entry.enemy.classification == EnemyClassification.Boss) bosses += entry.count;
                        if (entry.enemy != null && entry.enemy.stableId == "elite_war_shaman") shamans += entry.count;
                    }
                    if (i < 9 && bosses != 0) issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.boss.early", $"Wave {i + 1} cannot contain a boss.", wave));
                    if (i == 9 && bosses != 1) issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.boss.final", "Wave 10 requires exactly one boss.", wave));
                    if (shamans > 2) issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.shaman.bound", $"Wave {i + 1} exceeds the two-Shaman bound.", wave));
                }
            }
            if (stage.cardPoolOverride == null || stage.cardPoolOverride.stableId != PoolId)
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.pool", "Expansion stage requires ExpansionRun20.", stage));
            else issues.AddRange(GameplayDataValidation.ValidateCardPool(stage.cardPoolOverride));
            if (!stage.useExactWaveCounts) issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.counts", "Expansion stage must use deterministic exact wave counts.", stage));
            if (stage.battlefieldFixturePrefab == null)
                issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.anchors", "Expansion stage requires a battlefield anchor fixture.", stage));
            else
            {
                BattlefieldAnchor[] anchors = stage.battlefieldFixturePrefab.GetComponentsInChildren<BattlefieldAnchor>(true);
                if (anchors.Count(x => x.AnchorType == BattlefieldAnchorType.Trap) < 2 || anchors.Count(x => x.AnchorType == BattlefieldAnchorType.Defense) < 1)
                    issues.Add(new GameplayValidationIssue(ValidationSeverity.Error, "expansion.anchors.count", "Expansion fixture requires two Trap anchors and one Defense anchor.", stage.battlefieldFixturePrefab));
            }
            return issues;
        }

        public static int TotalEnemies(WaveData wave) => wave?.spawns == null ? 0 : wave.spawns.Sum(x => x.count);
        private static bool FinitePositive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        private static bool FiniteNonNegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
