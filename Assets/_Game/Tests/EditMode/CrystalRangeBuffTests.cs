using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class CrystalRangeBuffTests
    {
        [Test]
        public void StarterCrystalDefinition_DefaultAttackRange_IsAtLeast28()
        {
            var def = ScriptableObject.CreateInstance<StarterCrystalDefinition>();
            Assert.GreaterOrEqual(def.attackRange, 28f, "Default crystal attack range should be buffed to 28m (+100%).");
        }

        [Test]
        public void Resources_StarterCrystals_AllHaveDoubledRange()
        {
            string[] crystalIds = { "crystal_fire", "crystal_ice", "crystal_lightning", "crystal_shadow", "crystal_stone" };

            foreach (string id in crystalIds)
            {
                var crystal = Resources.Load<StarterCrystalDefinition>("Crystals/" + id);
                if (crystal != null)
                {
                    Assert.GreaterOrEqual(crystal.attackRange, 24f, $"Crystal '{id}' attack range should be >= 24m (doubled range). Actual: {crystal.attackRange}");
                }
            }
        }
    }
}
