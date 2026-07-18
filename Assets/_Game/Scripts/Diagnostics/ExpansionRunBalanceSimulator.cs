using System;
using System.Collections.Generic;
using System.Linq;

namespace Stonehold
{
    public sealed class ExpansionBalanceReport
    {
        public int Runs, Wins, InvalidDrafts, SoftLocks;
        public readonly int[] FailureByWave = new int[11];
        public readonly Dictionary<string, int> Offers = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly Dictionary<string, int> Picks = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly Dictionary<string, int> Recruits = new Dictionary<string, int>(StringComparer.Ordinal);
        public double WaveSum, DurationSum;
        public readonly List<int> Reached = new List<int>();
        public double WinRate => Runs == 0 ? 0d : (double)Wins / Runs;
        public double AverageWave => Runs == 0 ? 0d : WaveSum / Runs;
        public double AverageDuration => Runs == 0 ? 0d : DurationSum / Runs;
        public int MedianWave => Reached.Count == 0 ? 0 : Reached.OrderBy(x => x).ElementAt(Reached.Count / 2);
    }

    public static class ExpansionRunBalanceSimulator
    {
        public static ExpansionBalanceReport Run(StageData stage, int runs)
        {
            var report = new ExpansionBalanceReport { Runs = runs };
            foreach (CardPoolEntry entry in stage.cardPoolOverride.cards) { report.Offers[entry.card.id] = 0; report.Picks[entry.card.id] = 0; }
            for (int seed = 0; seed < runs; seed++) Simulate(stage, seed, report);
            return report;
        }

        private static void Simulate(StageData stage, int seed, ExpansionBalanceReport report)
        {
            var heroes = new HashSet<string>(StringComparer.Ordinal) { "archer" };
            var stacks = new Dictionary<string, int>(StringComparer.Ordinal);
            var rng = new Random(seed * 7919 + 17);
            int openSlots = 3, trapSlots = 2, defenseSlots = 1, reached = 0;
            float power = 37f;
            for (int waveIndex = 0; waveIndex < stage.waves.Length; waveIndex++)
            {
                DraftSelectionState state = new DraftSelectionState(heroes, AttackTypes(heroes), openSlots, stacks, trapSlots > 0, defenseSlots > 0);
                List<DraftCardChoice> choices = CardDraftSelector.Generate(stage.cardPoolOverride.cards, state, 3, stage.cardPoolOverride.recruitOptionPolicy, seed * 101 + waveIndex);
                if (choices.Count == 0) { report.InvalidDrafts++; report.SoftLocks++; break; }
                foreach (DraftCardChoice offered in choices) report.Offers[offered.Card.id]++;
                DraftCardChoice selected = Select(choices);
                report.Picks[selected.Card.id]++;
                CardDefinition card = selected.Card;
                if (card.cardCategory == CardCategory.RecruitHero)
                {
                    if (heroes.Add(card.recruitHeroId)) { openSlots--; power += 11f; report.Recruits[card.recruitHeroId] = report.Recruits.TryGetValue(card.recruitHeroId, out int value) ? value + 1 : 1; }
                }
                else if (card.cardCategory == CardCategory.HeroUpgrade)
                {
                    stacks[card.id] = stacks.TryGetValue(card.id, out int value) ? value + 1 : 1; power += 5f;
                }
                else if (card.cardCategory == CardCategory.Trap) { trapSlots--; power += 5f; }
                else if (card.cardCategory == CardCategory.BattlefieldDefense) { defenseSlots--; power += 4f; }
                else { stacks[card.id] = stacks.TryGetValue(card.id, out int value) ? value + 1 : 1; power += 3f; }

                float threat = Threat(stage.waves[waveIndex]);
                float variance = 0.88f + (float)rng.NextDouble() * 0.24f;
                if (power * variance < threat * 0.86f + (waveIndex + 1) * 0.8f)
                {
                    report.FailureByWave[waveIndex + 1]++;
                    reached = waveIndex + 1;
                    break;
                }
                reached = waveIndex + 1;
            }
            if (reached == 10) report.Wins++;
            report.WaveSum += reached;
            report.Reached.Add(reached);
            int totalEnemies = stage.waves.Sum(ExpansionRunValidation.TotalEnemies);
            report.DurationSum += 300d + totalEnemies * 0.8d + rng.NextDouble() * 60d;
        }

        private static DraftCardChoice Select(List<DraftCardChoice> choices)
        {
            DraftCardChoice found = choices.FirstOrDefault(x => x.Card.cardCategory == CardCategory.RecruitHero);
            if (found.Card != null) return found;
            found = choices.FirstOrDefault(x => x.Card.cardCategory == CardCategory.HeroUpgrade);
            if (found.Card != null) return found;
            found = choices.FirstOrDefault(x => x.Card.cardCategory == CardCategory.Trap || x.Card.cardCategory == CardCategory.BattlefieldDefense);
            if (found.Card != null) return found;
            found = choices.FirstOrDefault(x => x.Card.modifierType == CardModifierType.DamageMultiplier || x.Card.modifierType == CardModifierType.FireRateMultiplier);
            return found.Card != null ? found : choices[0];
        }

        private static IEnumerable<AttackType> AttackTypes(HashSet<string> heroes)
        {
            if (heroes.Contains("archer")) yield return AttackType.SingleTarget;
            if (heroes.Contains("bombardier")) yield return AttackType.Splash;
            if (heroes.Contains("frost_mage")) yield return AttackType.Slow;
            if (heroes.Contains("electric_engineer")) yield return AttackType.Chain;
        }

        private static float Threat(WaveData wave)
        {
            float result = 0f;
            foreach (WaveData.SpawnEntry entry in wave.spawns)
            {
                float weight = entry.enemy.stableId switch
                {
                    "runner" => 0.8f, "grunt" => 1f, "brute" => 2.5f, "armored" => 2.2f,
                    "crossbow_raider" => 1.8f, "elite_war_shaman" => 5f, "warlord_boss" => 12f, _ => 1f
                };
                result += entry.count * weight;
            }
            return result;
        }
    }
}
