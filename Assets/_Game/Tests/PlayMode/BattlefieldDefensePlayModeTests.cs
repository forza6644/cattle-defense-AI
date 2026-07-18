using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class BattlefieldDefensePlayModeTests
    {
        private const string Root = "Assets/_Game/ScriptableObjects/BattlefieldDefenseQualification";
        private readonly List<Object> owned = new List<Object>();
        private BattlefieldAnchorManager anchors;
        private TrapRuntimeManager traps;
        private BattlefieldDefenseManager defenses;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            traps = TrapRuntimeManager.Instance ?? Own(new GameObject("T13F Trap Manager")).AddComponent<TrapRuntimeManager>();
            defenses = BattlefieldDefenseManager.Instance ?? Own(new GameObject("T13F Defense Manager")).AddComponent<BattlefieldDefenseManager>();
            anchors = BattlefieldAnchorManager.Instance ?? Own(new GameObject("T13F Anchor Manager")).AddComponent<BattlefieldAnchorManager>();
            traps.ResetForRun(); defenses.ResetForRun(); anchors.ResetForRun();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            traps?.ResetForRun(); defenses?.ResetForRun(); anchors?.ResetForRun();
            foreach (TrapRuntimeZone item in Object.FindObjectsByType<TrapRuntimeZone>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(item.gameObject);
            foreach (BattlefieldDefenseRuntime item in Object.FindObjectsByType<BattlefieldDefenseRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(item.gameObject);
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear(); yield return null;
        }

        [Test] public void TrapAnchor_AcceptsValidDeploymentAndRejectsDuplicate()
        {
            CreateAnchor(BattlefieldAnchorType.Trap, Vector3.zero);
            Assert.That(traps.TryDeploy(Caltrops, out TrapRuntimeZone first), Is.True);
            Assert.That(first.Anchor.IsOccupied, Is.True);
            Assert.That(traps.TryDeploy(Oil, out _), Is.False);
            Assert.That(traps.RejectedCount, Is.EqualTo(1));
        }

        [Test] public void MissingAnchor_FailsSafely()
        {
            Assert.That(traps.TryDeploy(Caltrops, out _), Is.False);
            Assert.That(defenses.TryDeploy(Barricade, out _), Is.False);
        }

        [Test] public void Caltrops_SlowsAndDealsBoundedPhysicalDamage()
        {
            CreateAnchor(BattlefieldAnchorType.Trap, Vector3.zero); Enemy enemy = CreateEnemy("runner", 30f, 0f, 0f, Vector3.zero);
            Assert.That(traps.TryDeploy(Caltrops, out TrapRuntimeZone zone), Is.True); zone.TickForQualification(0.9f);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(28f).Within(0.01f)); Assert.That(enemy.SlowMultiplier, Is.EqualTo(0.72f).Within(0.01f));
        }

        [Test] public void Caltrops_RespectsArmorAndCrowdControlResistance()
        {
            CreateAnchor(BattlefieldAnchorType.Trap, Vector3.zero); Enemy enemy = CreateEnemy("elite", 40f, 3f, 0.5f, Vector3.zero);
            traps.TryDeploy(Caltrops, out TrapRuntimeZone zone); zone.TickForQualification(0.9f);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(39f).Within(0.01f)); Assert.That(enemy.SlowMultiplier, Is.EqualTo(0.86f).Within(0.01f));
        }

        [Test] public void Caltrops_DoesNotTickDeadEnemyAndExpiresCleanly()
        {
            CreateAnchor(BattlefieldAnchorType.Trap, Vector3.zero); Enemy enemy = CreateEnemy("fragile", 1f, 0f, 0f, Vector3.zero);
            traps.TryDeploy(Caltrops, out TrapRuntimeZone zone); zone.TickForQualification(0.9f); float health = enemy.CurrentHealth; zone.TickForQualification(20f);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(health)); Assert.That(traps.ActiveTraps, Is.Empty); Assert.That(anchors.OccupiedCount, Is.Zero);
        }

        [UnityTest] public IEnumerator BurningOil_TriggersTelegraphsBurnsAndStops()
        {
            CreateAnchor(BattlefieldAnchorType.Trap, Vector3.zero); Enemy enemy = CreateEnemy("oil-target", 50f, 0f, 0f, Vector3.zero);
            traps.TryDeploy(Oil, out TrapRuntimeZone zone); Assert.That(zone.IsTriggered, Is.False); Assert.That(enemy.CurrentHealth, Is.EqualTo(50f));
            zone.TickForQualification(0.1f); Assert.That(zone.IsTriggered, Is.True); Assert.That(zone.IsBurning, Is.False);
            zone.TickForQualification(0.65f); Assert.That(zone.IsBurning, Is.True); zone.TickForQualification(0.75f);
            yield return new WaitForSeconds(1.05f); Assert.That(enemy.CurrentHealth, Is.LessThan(50f));
            zone.Deactivate(); float after = enemy.CurrentHealth; yield return new WaitForSeconds(1.1f); Assert.That(enemy.CurrentHealth, Is.EqualTo(after).Within(0.01f));
        }

        [Test] public void BurningOil_DoesNotRecursivelyCreateZones()
        {
            CreateAnchor(BattlefieldAnchorType.Trap, Vector3.zero); CreateEnemy("group", 40f, 0f, 0f, Vector3.zero);
            traps.TryDeploy(Oil, out TrapRuntimeZone zone); int created = traps.CreatedCount;
            for (int i = 0; i < 20; i++) zone.TickForQualification(0.5f);
            Assert.That(traps.CreatedCount, Is.EqualTo(created)); Assert.That(traps.ActiveTraps.Count, Is.LessThanOrEqualTo(1));
        }

        [Test] public void TrapDamage_IsAttributedAsBattlefieldNotHero()
        {
            Own(new GameObject("T13F Damage Tracker")).AddComponent<DamageTracker>(); CreateAnchor(BattlefieldAnchorType.Trap, Vector3.zero); CreateEnemy("target", 20f, 0f, 0f, Vector3.zero);
            traps.TryDeploy(Caltrops, out TrapRuntimeZone zone); zone.TickForQualification(0.9f);
            Assert.That(DamageTracker.Instance.DamageByHeroId.Keys, Does.Contain("battlefield:trap_caltrops")); Assert.That(DamageTracker.Instance.DamageByHeroId.Keys.Any(x => x == "archer"), Is.False);
        }

        [Test] public void Barricade_DeploysBlocksTakesDamageAndGivesNoReward()
        {
            CreateAnchor(BattlefieldAnchorType.Defense, Vector3.zero); Enemy attacker = CreateEnemy("grunt", 20f, 0f, 0f, Vector3.one);
            Assert.That(defenses.TryDeploy(Barricade, out BattlefieldDefenseRuntime wall), Is.True); float dealt = wall.TakeDamage(5f, attacker, attacker.ActivationId);
            Assert.That(dealt, Is.EqualTo(3f)); Assert.That(wall.CurrentHealth, Is.EqualTo(87f)); Assert.That(wall.Definition.damage, Is.Zero);
        }

        [UnityTest] public IEnumerator CrossbowRaider_AttacksBarricadeInsteadOfCastle()
        {
            CreateAnchor(BattlefieldAnchorType.Defense, Vector3.zero); defenses.TryDeploy(Barricade, out BattlefieldDefenseRuntime wall);
            Enemy raider = CreateEnemyFromAsset("Assets/_Game/ScriptableObjects/EnemyExpansionQualification/CrossbowRaiderData.asset", new Vector3(0f, 0f, 4f), true);
            EnemySpecialBehavior special = raider.GetComponent<EnemySpecialBehavior>(); special.PrepareForSpawn(raider); special.Activate(null, raider.ActivationId);
            float before = wall.CurrentHealth; float elapsed = 0f;
            while (elapsed < 1f) { special.Tick(); elapsed += Time.deltaTime; yield return null; }
            Assert.That(wall.CurrentHealth, Is.LessThan(before));
        }

        [UnityTest] public IEnumerator WarShaman_DoesNotHealBarricade()
        {
            CreateAnchor(BattlefieldAnchorType.Defense, Vector3.zero); defenses.TryDeploy(Barricade, out BattlefieldDefenseRuntime wall); Enemy attacker = CreateEnemy("attacker", 20f, 0f, 0f, Vector3.one); wall.TakeDamage(10f, attacker, attacker.ActivationId);
            Enemy shaman = CreateEnemyFromAsset("Assets/_Game/ScriptableObjects/EnemyExpansionQualification/WarShamanData.asset", Vector3.zero, true); EnemySpecialBehavior special = shaman.GetComponent<EnemySpecialBehavior>(); special.PrepareForSpawn(shaman); special.Activate(null, shaman.ActivationId);
            float before = wall.CurrentHealth; yield return new WaitForSeconds(1.1f); special.Tick(); Assert.That(wall.CurrentHealth, Is.EqualTo(before));
        }

        [Test] public void Warlord_CanDestroyBarricadeAndReleasesAnchor()
        {
            CreateAnchor(BattlefieldAnchorType.Defense, Vector3.zero); defenses.TryDeploy(Barricade, out BattlefieldDefenseRuntime wall); Enemy boss = CreateEnemy("warlord", 500f, 0f, 0f, Vector3.one, EnemyClassification.Boss, 100);
            wall.TakeDamage(100f, boss, boss.ActivationId); Assert.That(defenses.ActiveDefense, Is.Null); Assert.That(anchors.OccupiedCount, Is.Zero);
        }

        [Test] public void HeroesCannotTargetBarricade()
        {
            CreateAnchor(BattlefieldAnchorType.Defense, Vector3.zero); defenses.TryDeploy(Barricade, out _);
            Assert.That(EnemyManager.FindNearest(Vector3.zero, 100f), Is.Null);
        }

        [Test] public void Restart_ClearsTrapsDefenseAndAnchors()
        {
            CreateAnchor(BattlefieldAnchorType.Trap, Vector3.zero); CreateAnchor(BattlefieldAnchorType.Defense, Vector3.forward);
            traps.TryDeploy(Caltrops, out _); defenses.TryDeploy(Barricade, out _); traps.ResetForRun(); defenses.ResetForRun(); anchors.ResetForRun();
            Assert.That(traps.ActiveTraps, Is.Empty); Assert.That(defenses.ActiveDefense, Is.Null); Assert.That(anchors.OccupiedCount, Is.Zero); Assert.That(traps.StaleTickCount, Is.Zero); Assert.That(defenses.StaleTargetCount, Is.Zero);
        }

        [UnityTest] public IEnumerator DuplicateManagers_DoNotReplaceSingletons()
        {
            TrapRuntimeManager original = TrapRuntimeManager.Instance; GameObject duplicate = Own(new GameObject("T13F Duplicate")); duplicate.AddComponent<TrapRuntimeManager>(); yield return null;
            Assert.That(TrapRuntimeManager.Instance, Is.SameAs(original));
        }

        [Test] public void RejectedCardDeployment_DoesNotEnterActiveCards()
        {
            RunModifierManager modifiers = RunModifierManager.Instance ?? Own(new GameObject("T13F Modifiers")).AddComponent<RunModifierManager>(); modifiers.ClearModifiers();
            CardDraftManager draft = CardDraftManager.Instance ?? Own(new GameObject("T13F Draft")).AddComponent<CardDraftManager>();
            MethodInfo apply = typeof(CardDraftManager).GetMethod("ApplyCard", BindingFlags.Instance | BindingFlags.NonPublic); apply.Invoke(draft, new object[] { Load<CardDefinition>(Root + "/Cards/DeployCaltrops.asset") });
            Assert.That(modifiers.ActiveCards, Is.Empty);
        }

        [Test] public void Stress_DeployResetCyclesRemainCleanAndReusePools()
        {
            CreateAnchor(BattlefieldAnchorType.Trap, Vector3.zero); CreateAnchor(BattlefieldAnchorType.Defense, Vector3.forward);
            int trapCreatedBefore = traps.CreatedCount; int trapReusedBefore = traps.ReuseCount;
            int defenseCreatedBefore = defenses.CreatedCount; int defenseReusedBefore = defenses.ReuseCount;
            for (int i = 0; i < 300; i++) { Assert.That(traps.TryDeploy(Caltrops, out _), Is.True); traps.ResetForRun(); }
            for (int i = 0; i < 300; i++) { Assert.That(traps.TryDeploy(Oil, out _), Is.True); traps.ResetForRun(); }
            for (int i = 0; i < 200; i++) { Assert.That(defenses.TryDeploy(Barricade, out _), Is.True); defenses.ResetForRun(); }
            Assert.That(traps.ActiveTraps, Is.Empty); Assert.That(defenses.ActiveDefense, Is.Null); Assert.That(anchors.OccupiedCount, Is.Zero);
            Assert.That(traps.CreatedCount - trapCreatedBefore, Is.LessThanOrEqualTo(2)); Assert.That(defenses.CreatedCount - defenseCreatedBefore, Is.LessThanOrEqualTo(1));
            Assert.That(traps.ReuseCount - trapReusedBefore, Is.GreaterThanOrEqualTo(598)); Assert.That(defenses.ReuseCount - defenseReusedBefore, Is.GreaterThanOrEqualTo(199));
        }

        [Test] public void ActivationIdWrap_DoesNotBreakTrapSafety()
        {
            FieldInfo counter = typeof(Enemy).GetField("globalActivationCounter", BindingFlags.Static | BindingFlags.NonPublic); counter.SetValue(null, int.MaxValue);
            Enemy enemy = CreateEnemy("wrapped", 20f, 0f, 0f, Vector3.zero); Assert.That(enemy.ActivationId, Is.EqualTo(1));
            CreateAnchor(BattlefieldAnchorType.Trap, Vector3.zero); traps.TryDeploy(Caltrops, out TrapRuntimeZone zone); zone.TickForQualification(0.9f); Assert.That(enemy.CurrentHealth, Is.LessThan(20f));
        }

        [Test] public void ControlledEncounter_AllSystemsCleanAfterReset()
        {
            CreateAnchor(BattlefieldAnchorType.Trap, Vector3.zero); CreateAnchor(BattlefieldAnchorType.Trap, Vector3.right * 4f); CreateAnchor(BattlefieldAnchorType.Defense, Vector3.forward * 2f);
            for (int i = 0; i < 4; i++) Own(new GameObject("T13F Hero " + i)).AddComponent<HeroAttack>();
            CreateEnemy("runner", 30f, 0f, 0f, Vector3.zero); CreateEnemy("armored", 60f, 3f, 0.2f, Vector3.right * 4f); CreateEnemyFromAsset("Assets/_Game/ScriptableObjects/EnemyExpansionQualification/CrossbowRaiderData.asset", Vector3.one, true); CreateEnemyFromAsset("Assets/_Game/ScriptableObjects/EnemyExpansionQualification/WarShamanData.asset", Vector3.right, true); CreateEnemy("warlord", 500f, 4f, 0.6f, Vector3.forward * 2f, EnemyClassification.Boss, 12);
            traps.TryDeploy(Caltrops, out TrapRuntimeZone caltrops); traps.TryDeploy(Oil, out TrapRuntimeZone oil); defenses.TryDeploy(Barricade, out _); caltrops.TickForQualification(0.9f); oil.TickForQualification(0.1f); oil.TickForQualification(0.65f); oil.TickForQualification(0.75f);
            traps.ResetForRun(); defenses.ResetForRun(); anchors.ResetForRun(); Assert.That(traps.ActiveTraps, Is.Empty); Assert.That(defenses.ActiveDefense, Is.Null); Assert.That(anchors.OccupiedCount, Is.Zero);
        }

        private TrapDefinition Caltrops => Load<TrapDefinition>(Root + "/Caltrops.asset");
        private TrapDefinition Oil => Load<TrapDefinition>(Root + "/BurningOil.asset");
        private BattlefieldDefenseDefinition Barricade => Load<BattlefieldDefenseDefinition>(Root + "/WoodenBarricade.asset");
        private static T Load<T>(string path) where T : Object => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);

        private BattlefieldAnchor CreateAnchor(BattlefieldAnchorType type, Vector3 position)
        {
            GameObject go = Own(new GameObject("T13F Anchor")); go.SetActive(false); go.transform.position = position; BattlefieldAnchor anchor = go.AddComponent<BattlefieldAnchor>(); anchor.Configure(type); go.SetActive(true); return anchor;
        }

        private Enemy CreateEnemy(string id, float health, float armor, float resistance, Vector3 position, EnemyClassification classification = EnemyClassification.Normal, int castleDamage = 2)
        {
            EnemyData data = Own(ScriptableObject.CreateInstance<EnemyData>()); data.stableId = id; data.enemyName = id; data.health = health; data.moveSpeed = 3f; data.armor = armor; data.crowdControlResistance = resistance; data.goldReward = 1; data.xpValue = 1; data.castleDamage = castleDamage; data.classification = classification;
            return CreateEnemy(data, position, false);
        }

        private Enemy CreateEnemyFromAsset(string path, Vector3 position, bool special) => CreateEnemy(Load<EnemyData>(path), position, special);

        private Enemy CreateEnemy(EnemyData data, Vector3 position, bool special)
        {
            GameObject go = Own(new GameObject("T13F Enemy " + data.stableId)); go.SetActive(false); go.transform.position = position; if (special) go.AddComponent<EnemySpecialBehavior>(); Enemy enemy = go.AddComponent<Enemy>();
            typeof(Enemy).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, data); go.SetActive(true); return enemy;
        }

        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
    }
}
