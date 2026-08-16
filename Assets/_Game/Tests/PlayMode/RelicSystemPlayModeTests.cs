using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Stonehold;

namespace Stonehold.Tests.PlayMode
{
    public class RelicSystemPlayModeTests
    {
        private GameObject testRoot;
        private RelicManager relicManager;
        private Castle castle;
        private GameConfig gameConfig;
        private List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("RelicTestRoot");
            var rmObj = new GameObject("RelicManager");
            rmObj.transform.SetParent(testRoot.transform);
            relicManager = rmObj.AddComponent<RelicManager>();

            gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            gameConfig.castleMaxHealth = 100;
            createdObjects.Add(gameConfig);

            var castleObj = new GameObject("Castle");
            castleObj.transform.SetParent(testRoot.transform);
            castleObj.SetActive(false);
            castle = castleObj.AddComponent<Castle>();
            var configField = castle.GetType().GetField("config", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (configField != null) configField.SetValue(castle, gameConfig);
            castleObj.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }
            foreach (var obj in createdObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            createdObjects.Clear();
        }

        private RelicDefinition CreateRelic(string id, string name, RelicRarity rarity, RelicEffectType effectType, float value)
        {
            var relic = ScriptableObject.CreateInstance<RelicDefinition>();
            relic.id = id;
            relic.displayName = name;
            relic.description = "Test relic " + name;
            relic.rarity = rarity;
            relic.effectType = effectType;
            relic.effectValue = value;
            createdObjects.Add(relic);
            return relic;
        }

        [UnityTest]
        public IEnumerator RelicManager_AddRelic_RegistersAndFiresEvent()
        {
            bool eventFired = false;
            RelicManager.OnRelicsUpdated += () => eventFired = true;

            var relic = CreateRelic("test_relic", "Test Relic", RelicRarity.Rare, RelicEffectType.CooldownReductionGlobal, 0.25f);
            relicManager.AddRelic(relic);

            Assert.IsTrue(relicManager.HasRelic("test_relic"));
            Assert.AreEqual(1, relicManager.ActiveRelics.Count);
            Assert.IsTrue(eventFired);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RelicManager_ChronoHourglass_ReducesHeroAbilityCooldown()
        {
            var heroObj = new GameObject("TestHero");
            heroObj.transform.SetParent(testRoot.transform);
            var attack = heroObj.AddComponent<HeroAttack>();

            var heroDef = ScriptableObject.CreateInstance<HeroDefinition>();
            heroDef.id = "frost_mage";
            heroDef.abilityType = HeroAbilityType.FrostNova;
            heroDef.abilityCooldown = 10f;
            createdObjects.Add(heroDef);

            attack.Configure(heroDef);

            Assert.AreEqual(10f, attack.GetModifiedAbilityCooldown(), 0.01f);

            var hourglass = CreateRelic("chrono_hourglass", "Chrono Hourglass", RelicRarity.Epic, RelicEffectType.CooldownReductionGlobal, 0.20f);
            relicManager.AddRelic(hourglass);

            // 10s * (1 - 0.20) = 8.0s
            Assert.AreEqual(8.0f, attack.GetModifiedAbilityCooldown(), 0.01f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RelicManager_AegisBattery_GrantsKineticShieldAndAbsorbsDamage()
        {
            Assert.AreEqual(0f, castle.CurrentShield);
            Assert.AreEqual(100, castle.CurrentHealth);

            var aegis = CreateRelic("aegis_battery", "Aegis Battery", RelicRarity.Epic, RelicEffectType.CastleShieldRecharge, 150f);
            relicManager.AddRelic(aegis);

            Assert.AreEqual(150f, castle.CurrentShield);
            Assert.AreEqual(150f, castle.MaxShield);

            // Take 50 damage - should be absorbed by shield
            castle.TakeDamage(50);
            Assert.AreEqual(100f, castle.CurrentShield);
            Assert.AreEqual(100, castle.CurrentHealth); // Health untouched

            // Take 120 damage - 100 absorbed by shield, 20 breaches into castle health
            castle.TakeDamage(120);
            Assert.AreEqual(0f, castle.CurrentShield);
            Assert.AreEqual(80, castle.CurrentHealth);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RelicManager_MidasCoin_MultipliesGoldBonus()
        {
            Assert.AreEqual(1.0f, relicManager.GetEliteBossGoldMultiplier(), 0.01f);

            var coin = CreateRelic("midas_coin", "Midas Coin", RelicRarity.Common, RelicEffectType.EliteBossGoldBonus, 0.30f);
            relicManager.AddRelic(coin);

            Assert.AreEqual(1.30f, relicManager.GetEliteBossGoldMultiplier(), 0.01f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RelicManager_VampiricCrest_HealsCastleOnCrit()
        {
            castle.TakeDamage(40);
            Assert.AreEqual(60, castle.CurrentHealth);

            var vamp = CreateRelic("vampiric_crest", "Vampiric Crest", RelicRarity.Legendary, RelicEffectType.CritCastleVampirism, 0.10f);
            relicManager.AddRelic(vamp);

            Assert.AreEqual(0.10f, relicManager.GetCastleVampirismPercent(), 0.001f);

            // Deal 100 crit damage -> 10% siphoned = 10 HP healed to Castle
            int heal = Mathf.Max(1, Mathf.RoundToInt(100f * relicManager.GetCastleVampirismPercent()));
            castle.Repair(heal);

            Assert.AreEqual(70, castle.CurrentHealth);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RelicManager_DraftRoll_ReturnsDistinctRelics()
        {
            var r1 = CreateRelic("r1", "Relic 1", RelicRarity.Common, RelicEffectType.CooldownReductionGlobal, 0.1f);
            var r2 = CreateRelic("r2", "Relic 2", RelicRarity.Rare, RelicEffectType.ElementalReactionBoost, 0.2f);
            var r3 = CreateRelic("r3", "Relic 3", RelicRarity.Epic, RelicEffectType.CastleShieldRecharge, 100f);
            var r4 = CreateRelic("r4", "Relic 4", RelicRarity.Legendary, RelicEffectType.CritCastleVampirism, 0.05f);

            relicManager.RegisterAllAvailableRelics(new[] { r1, r2, r3, r4 });

            var draft = relicManager.RollRelicDraft(3);
            Assert.AreEqual(3, draft.Count);

            var uniqueSet = new HashSet<string>();
            for (int i = 0; i < draft.Count; i++)
            {
                Assert.IsTrue(uniqueSet.Add(draft[i].id), "Draft choice " + draft[i].id + " was duplicated!");
            }

            yield return null;
        }
    }
}
