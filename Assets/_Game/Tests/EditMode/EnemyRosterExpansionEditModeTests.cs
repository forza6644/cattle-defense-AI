using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Tests
{
    public sealed class EnemyRosterExpansionEditModeTests
    {
        private const string RaiderPath = "Assets/_Game/ScriptableObjects/EnemyExpansionQualification/CrossbowRaiderData.asset";
        private const string ShamanPath = "Assets/_Game/ScriptableObjects/EnemyExpansionQualification/WarShamanData.asset";
        private const string QualificationWavePath = "Assets/_Game/ScriptableObjects/EnemyExpansionQualification/Task13E_QualificationWave.asset";
        private EnemyData raider;
        private EnemyData shaman;

        [SetUp]
        public void SetUp()
        {
            raider = AssetDatabase.LoadAssetAtPath<EnemyData>(RaiderPath);
            shaman = AssetDatabase.LoadAssetAtPath<EnemyData>(ShamanPath);
            Assert.That(raider, Is.Not.Null);
            Assert.That(shaman, Is.Not.Null);
        }

        [Test] public void CrossbowRaider_HasStableIdAndNormalClassification()
        {
            Assert.That(raider.stableId, Is.EqualTo("crossbow_raider"));
            Assert.That(raider.classification, Is.EqualTo(EnemyClassification.Normal));
        }

        [Test] public void WarShaman_HasStableIdAndEliteClassification()
        {
            Assert.That(shaman.stableId, Is.EqualTo("elite_war_shaman"));
            Assert.That(shaman.classification, Is.EqualTo(EnemyClassification.Elite));
        }

        [Test] public void EnemyStableIds_AreUniqueAcrossProject()
        {
            EnemyData[] enemies = LoadAll<EnemyData>();
            Assert.That(enemies.Select(x => x.stableId).Distinct().Count(), Is.EqualTo(enemies.Length));
        }

        [Test] public void CrossbowRaider_RangedStatsAndProjectileAreValid()
        {
            Assert.That(raider.specialRole, Is.EqualTo(EnemySpecialRole.RangedCastleAttacker));
            Assert.That(raider.health, Is.EqualTo(17f));
            Assert.That(raider.rangedAttack.standOffRange, Is.EqualTo(5.5f));
            Assert.That(raider.rangedAttack.windUpSeconds, Is.EqualTo(0.75f));
            Assert.That(raider.rangedAttack.cooldownSeconds, Is.EqualTo(2.1f));
            Assert.That(raider.rangedAttack.projectileSpeed, Is.EqualTo(10f));
            Assert.That(raider.rangedAttack.projectilePrefab, Is.Not.Null);
            Assert.That(raider.rangedAttack.projectilePrefab.GetComponent<EnemyCastleProjectile>(), Is.Not.Null);
        }

        [Test] public void WarShaman_HealingStatsAreBoundedAndExcludeBoss()
        {
            Assert.That(shaman.specialRole, Is.EqualTo(EnemySpecialRole.HealingElite));
            Assert.That(shaman.health, Is.EqualTo(75f));
            Assert.That(shaman.healingPulse.intervalSeconds, Is.EqualTo(5f));
            Assert.That(shaman.healingPulse.castSeconds, Is.EqualTo(1f));
            Assert.That(shaman.healingPulse.radius, Is.EqualTo(4f));
            Assert.That(shaman.healingPulse.maxHealthFraction, Is.EqualTo(0.12f));
            Assert.That(shaman.healingPulse.selfHealMultiplier, Is.EqualTo(0.5f));
            Assert.That(shaman.healingPulse.targetCap, Is.EqualTo(5));
            Assert.That(shaman.healingPulse.excludeBoss, Is.True);
        }

        [Test] public void ExpansionPrefabs_AreProjectOwnedAndContainEnemyComponents()
        {
            Assert.That(AssetDatabase.GetAssetPath(raider.prefab), Does.StartWith("Assets/_Game/Prefabs/EnemyExpansion/"));
            Assert.That(AssetDatabase.GetAssetPath(shaman.prefab), Does.StartWith("Assets/_Game/Prefabs/EnemyExpansion/"));
            Assert.That(raider.prefab.GetComponent<Enemy>(), Is.Not.Null);
            Assert.That(shaman.prefab.GetComponent<Enemy>(), Is.Not.Null);
            Assert.That(raider.prefab.GetComponent<EnemySpecialBehavior>(), Is.Not.Null);
            Assert.That(shaman.prefab.GetComponent<EnemySpecialBehavior>(), Is.Not.Null);
        }

        [Test] public void Validator_AcceptsBothExpansionEnemies()
        {
            var issues = GameplayDataValidation.ValidateEnemies(new[] { raider, shaman });
            Assert.That(GameplayDataValidation.HasErrors(issues), Is.False, string.Join("\n", issues.Select(x => x.Code + ": " + x.Message)));
        }

        [Test] public void Validator_RejectsInvalidRangedConfiguration()
        {
            EnemyData invalid = Object.Instantiate(raider);
            invalid.rangedAttack = new EnemyRangedAttackSettings { standOffRange = 1f };
            Assert.That(GameplayDataValidation.HasErrors(GameplayDataValidation.ValidateEnemy(invalid)), Is.True);
            Object.DestroyImmediate(invalid);
        }

        [Test] public void Validator_RejectsBossHealingAndUnboundedTargetCap()
        {
            EnemyData invalid = Object.Instantiate(shaman);
            invalid.healingPulse = new EnemyHealingPulseSettings
            {
                intervalSeconds = 5f, castSeconds = 1f, radius = 4f,
                maxHealthFraction = 0.12f, selfHealMultiplier = 0.5f,
                targetCap = 99, excludeBoss = false
            };
            var issues = GameplayDataValidation.ValidateEnemy(invalid);
            Assert.That(issues.Any(x => x.Code == "enemy.healing-stats"), Is.True);
            Assert.That(issues.Any(x => x.Code == "enemy.healing-boss-exclusion"), Is.True);
            Object.DestroyImmediate(invalid);
        }

        [Test] public void QualificationWave_ContainsOnlyTask13EEnemies()
        {
            WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(QualificationWavePath);
            Assert.That(wave, Is.Not.Null);
            Assert.That(wave.spawns, Has.Length.EqualTo(2));
            CollectionAssert.AreEquivalent(new[] { "crossbow_raider", "elite_war_shaman" }, wave.spawns.Select(x => x.enemy.stableId));
        }

        [Test] public void ProductionWaves_DoNotReferenceExpansionEnemies()
        {
            StageData isolatedStage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/ScriptableObjects/ExpansionRunQualification/StoneholdExpansionTrial.asset");
            WaveData[] waves = LoadAll<WaveData>();
            foreach (WaveData wave in waves)
            {
                string path = AssetDatabase.GetAssetPath(wave).Replace('\\', '/');
                bool isIsolatedExpansionWave = isolatedStage != null && isolatedStage.waves != null && isolatedStage.waves.Contains(wave);
                if (path == QualificationWavePath || isIsolatedExpansionWave || wave.spawns == null) continue;
                Assert.That(wave.spawns.Any(x => x.enemy == raider || x.enemy == shaman), Is.False, wave.name);
            }
        }

        [Test] public void QualifiedCardPools_RemainUnchanged()
        {
            Assert.That(Resources.LoadAll<CardDefinition>("Cards"), Has.Length.EqualTo(39));
            CardPoolDefinition verticalSlice = AssetDatabase.LoadAssetAtPath<CardPoolDefinition>("Assets/_Game/ScriptableObjects/CardPools/VerticalSlice18.asset");
            Assert.That(verticalSlice.cards, Has.Count.EqualTo(18));
            Assert.That(verticalSlice.stableId, Is.EqualTo("vertical_slice_18"));
        }

        private static T[] LoadAll<T>() where T : Object
        {
            return AssetDatabase.FindAssets("t:" + typeof(T).Name)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(x => x != null)
                .ToArray();
        }
    }
}
