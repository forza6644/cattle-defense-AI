using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Stonehold
{
    [Serializable]
    public class TelemetryWaveData
    {
        public int waveNumber;
        public float realStartTimeSeconds;
        public float realEndTimeSeconds;
        public float durationSeconds;
        public int enemiesSpawned;
        public int enemiesDefeated;
        public int castleHpRemaining;
        public int castleMaxHp;
        public int playerXpAtWaveEnd;
        public int playerLevelAtWaveEnd;
    }

    [Serializable]
    public class TelemetryDraftData
    {
        public int draftIndex;
        public float timestampSeconds;
        public int playerLevel;
        public List<string> offeredCardTitles = new List<string>();
        public string selectedCardTitle;
    }

    [Serializable]
    public class TelemetryDamageSourceData
    {
        public string sourceId;
        public string displayName;
        public float totalDamage;
        public float damagePercentage;
        public float dps;
        public int critCount;
    }

    [Serializable]
    public class Stage1TelemetryReportData
    {
        public string reportTitle = "STAGE 1 REAL-TIME COMBAT TELEMETRY REPORT";
        public string generatedAtTimestamp;
        public string stageId = "stage_1_castle_road";
        public string finalGameState;
        public float totalRunDurationSeconds;
        public int totalWavesCleared;
        public int totalEnemiesSpawned;
        public int totalEnemiesDefeated;
        public int finalCastleHp;
        public int finalCastleMaxHp;
        public int finalPlayerLevel;
        public int finalPlayerXp;
        public int totalDraftTriggersFired;
        public List<TelemetryWaveData> waveLogs = new List<TelemetryWaveData>();
        public List<TelemetryDraftData> draftLogs = new List<TelemetryDraftData>();
        public List<TelemetryDamageSourceData> damageBreakdown = new List<TelemetryDamageSourceData>();
    }

    public class RealCombatTelemetryLogger
    {
        private Stage1TelemetryReportData report = new Stage1TelemetryReportData();
        private float runStartTime;
        private float currentWaveStartTime;
        private int currentWaveEnemiesSpawned;
        private int currentWaveEnemiesDefeated;
        private int totalEnemiesSpawned;
        private int totalEnemiesDefeated;

        public Stage1TelemetryReportData Report => report;

        public void StartRun()
        {
            runStartTime = Time.realtimeSinceStartup;
            report.generatedAtTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void OnWaveStart(int waveNumber)
        {
            currentWaveStartTime = Time.realtimeSinceStartup - runStartTime;
            currentWaveEnemiesSpawned = 0;
            currentWaveEnemiesDefeated = 0;
        }

        public void OnEnemySpawned(int count = 1)
        {
            currentWaveEnemiesSpawned += count;
            totalEnemiesSpawned += count;
        }

        public void OnEnemyDefeated(int count = 1)
        {
            currentWaveEnemiesDefeated += count;
            totalEnemiesDefeated += count;
        }

        public void OnWaveClear(int waveNumber, Castle castle, RunProgressionManager progression)
        {
            float now = Time.realtimeSinceStartup - runStartTime;
            var waveLog = new TelemetryWaveData
            {
                waveNumber = waveNumber,
                realStartTimeSeconds = currentWaveStartTime,
                realEndTimeSeconds = now,
                durationSeconds = now - currentWaveStartTime,
                enemiesSpawned = currentWaveEnemiesSpawned,
                enemiesDefeated = currentWaveEnemiesDefeated,
                castleHpRemaining = castle != null ? castle.CurrentHealth : 0,
                castleMaxHp = castle != null ? castle.MaxHealth : 0,
                playerXpAtWaveEnd = progression != null ? progression.CurrentXp : 0,
                playerLevelAtWaveEnd = progression != null ? progression.CurrentLevel : 0
            };
            report.waveLogs.Add(waveLog);
            report.totalWavesCleared = waveNumber;
        }

        public void OnDraftTriggered(int playerLevel, string[] offeredTitles, string selectedTitle)
        {
            float now = Time.realtimeSinceStartup - runStartTime;
            var draftLog = new TelemetryDraftData
            {
                draftIndex = report.draftLogs.Count + 1,
                timestampSeconds = now,
                playerLevel = playerLevel,
                offeredCardTitles = offeredTitles != null ? new List<string>(offeredTitles) : new List<string>(),
                selectedCardTitle = selectedTitle ?? "First Choice"
            };
            report.draftLogs.Add(draftLog);
            report.totalDraftTriggersFired++;
        }

        public void CompleteRun(GameState state, Castle castle, RunProgressionManager progression, CombatTelemetryManager combatTelemetry = null)
        {
            report.finalGameState = state.ToString();
            report.totalRunDurationSeconds = Time.realtimeSinceStartup - runStartTime;
            report.totalEnemiesSpawned = totalEnemiesSpawned;
            report.totalEnemiesDefeated = totalEnemiesDefeated;
            if (castle != null)
            {
                report.finalCastleHp = castle.CurrentHealth;
                report.finalCastleMaxHp = castle.MaxHealth;
            }
            if (progression != null)
            {
                report.finalPlayerLevel = progression.CurrentLevel;
                report.finalPlayerXp = progression.CurrentXp;
            }

            var telemetry = combatTelemetry != null ? combatTelemetry : (CombatTelemetryManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<CombatTelemetryManager>());
            if (telemetry != null)
            {
                report.damageBreakdown.Clear();
                var reports = telemetry.GetAllHeroReports();
                foreach (var r in reports)
                {
                    report.damageBreakdown.Add(new TelemetryDamageSourceData
                    {
                        sourceId = r.heroId,
                        displayName = r.displayName,
                        totalDamage = r.totalDamage,
                        damagePercentage = r.damagePercentage,
                        dps = r.dps,
                        critCount = r.critCount
                    });
                }
            }
        }

        public void SaveReport(string jsonPath, string txtPath)
        {
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(jsonPath, json);

            using (StreamWriter writer = new StreamWriter(txtPath, false))
            {
                writer.WriteLine("================================================================================");
                writer.WriteLine($" {report.reportTitle}");
                writer.WriteLine("================================================================================");
                writer.WriteLine($"Timestamp:                  {report.generatedAtTimestamp}");
                writer.WriteLine($"Stage ID:                   {report.stageId}");
                writer.WriteLine($"Final Game State:           {report.finalGameState}");
                writer.WriteLine($"Total Run Duration:         {report.totalRunDurationSeconds:F2} seconds");
                writer.WriteLine($"Total Waves Cleared:        {report.totalWavesCleared} / 10");
                writer.WriteLine($"Total Enemies Spawned:      {report.totalEnemiesSpawned}");
                writer.WriteLine($"Total Enemies Defeated:     {report.totalEnemiesDefeated}");
                writer.WriteLine($"Final Castle HP:            {report.finalCastleHp} / {report.finalCastleMaxHp}");
                writer.WriteLine($"Final Player Level:         {report.finalPlayerLevel}");
                writer.WriteLine($"Final Player XP:            {report.finalPlayerXp}");
                writer.WriteLine($"Total Draft Triggers:       {report.totalDraftTriggersFired}");
                writer.WriteLine("================================================================================");
                writer.WriteLine(" DAMAGE CONTRIBUTION BREAKDOWN:");
                writer.WriteLine("--------------------------------------------------------------------------------");
                writer.WriteLine(" Source ID             | Display Name         | Total Dmg | Share % | DPS   | Crits");
                writer.WriteLine("--------------------------------------------------------------------------------");
                if (report.damageBreakdown != null && report.damageBreakdown.Count > 0)
                {
                    foreach (var d in report.damageBreakdown)
                    {
                        writer.WriteLine($" {d.sourceId,-21} | {d.displayName,-20} | {d.totalDamage,9:F1} | {d.damagePercentage,6:F1}% | {d.dps,5:F1} | {d.critCount,5}");
                    }
                }
                else
                {
                    writer.WriteLine(" (No combat damage recorded)");
                }
                writer.WriteLine("================================================================================");
                writer.WriteLine(" WAVE BREAKDOWN:");
                writer.WriteLine("--------------------------------------------------------------------------------");
                writer.WriteLine(" Wave | Start (s) | End (s) | Duration (s) | Spawned | Defeated | Castle HP | XP   | Level");
                writer.WriteLine("--------------------------------------------------------------------------------");
                foreach (var w in report.waveLogs)
                {
                    writer.WriteLine($" {w.waveNumber,4} | {w.realStartTimeSeconds,9:F1} | {w.realEndTimeSeconds,7:F1} | {w.durationSeconds,12:F1} | {w.enemiesSpawned,7} | {w.enemiesDefeated,8} | {w.castleHpRemaining,4}/{w.castleMaxHp,-4} | {w.playerXpAtWaveEnd,4} | {w.playerLevelAtWaveEnd,5}");
                }
                writer.WriteLine("================================================================================");
                writer.WriteLine(" DRAFT SELECTION LOG:");
                writer.WriteLine("--------------------------------------------------------------------------------");
                foreach (var d in report.draftLogs)
                {
                    writer.WriteLine($" Draft #{d.draftIndex} @ {d.timestampSeconds:F1}s (Level {d.playerLevel})");
                    writer.WriteLine($"   Offered Choices:  {string.Join(" | ", d.offeredCardTitles)}");
                    writer.WriteLine($"   Selected Card:    {d.selectedCardTitle}");
                    writer.WriteLine("--------------------------------------------------------------------------------");
                }
            }
        }
    }
}
