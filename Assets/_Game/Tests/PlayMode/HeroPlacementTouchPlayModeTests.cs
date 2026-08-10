using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class HeroPlacementTouchPlayModeTests
    {
        private GameObject testContainer;
        private HeroRosterManager roster;

        [SetUp]
        public void SetUp()
        {
            testContainer = new GameObject("HeroPlacementTouchTests_Container");
            roster = testContainer.AddComponent<HeroRosterManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testContainer != null)
            {
                Object.DestroyImmediate(testContainer);
            }
        }

        [UnityTest]
        public IEnumerator RecruitHeroIntoSlot_PopulatesTargetSlotAndConsumesOption()
        {
            // Create 3 HeroSlots
            HeroSlot[] slots = new HeroSlot[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject slotObj = new GameObject($"HeroSlot_{i + 1:D2}");
                slotObj.transform.SetParent(testContainer.transform);
                slotObj.transform.position = new Vector3(i * 3f, 0f, 0f);
                slots[i] = slotObj.AddComponent<HeroSlot>();
            }

            // Create test HeroDefinition
            HeroDefinition knightDef = ScriptableObject.CreateInstance<HeroDefinition>();
            knightDef.id = "hero_knight_test";
            knightDef.displayName = "Test Knight";
            knightDef.heroPrefab = new GameObject("TestKnightPrefab");
            knightDef.heroPrefab.transform.SetParent(testContainer.transform);

            // Register definitions & slots
            roster.RegisterHeroDefinition(knightDef);
            for (int i = 0; i < slots.Length; i++)
            {
                roster.RegisterSlot(slots[i]);
            }

            yield return null;

            Assert.That(roster.OwnedHeroIds.Count, Is.EqualTo(0));
            Assert.IsFalse(slots[1].IsOccupied, "Target slot 2 must start unoccupied.");

            // Recruit hero explicitly into slot 2 (middle slot)
            bool success = roster.RecruitHeroIntoSlot("hero_knight_test", slots[1]);

            Assert.IsTrue(success, "RecruitHeroIntoSlot should succeed for valid empty slot.");
            Assert.IsTrue(slots[1].IsOccupied, "Target slot 2 must now be occupied.");
            Assert.IsFalse(slots[0].IsOccupied, "Slot 1 must remain empty.");
            Assert.IsFalse(slots[2].IsOccupied, "Slot 3 must remain empty.");
            Assert.That(roster.OwnedHeroIds.Count, Is.EqualTo(1), "Owned hero count should increment to 1.");
        }

        [UnityTest]
        public IEnumerator RecruitHeroIntoSlot_FailsIfSlotOccupiedOrNull()
        {
            GameObject slotObj = new GameObject("HeroSlot_01");
            slotObj.transform.SetParent(testContainer.transform);
            HeroSlot slot = slotObj.AddComponent<HeroSlot>();

            HeroDefinition archerDef = ScriptableObject.CreateInstance<HeroDefinition>();
            archerDef.id = "hero_archer_test";
            archerDef.displayName = "Test Archer";
            archerDef.heroPrefab = new GameObject("TestArcherPrefab");
            archerDef.heroPrefab.transform.SetParent(testContainer.transform);

            roster.RegisterHeroDefinition(archerDef);
            roster.RegisterSlot(slot);

            yield return null;

            // Recruit first hero into slot
            bool firstRecruit = roster.RecruitHeroIntoSlot("hero_archer_test", slot);
            Assert.IsTrue(firstRecruit, "First recruitment into empty slot must succeed.");

            // Attempt to recruit into null slot
            bool nullSlotRecruit = roster.RecruitHeroIntoSlot("hero_archer_test", null);
            Assert.IsFalse(nullSlotRecruit, "RecruitHeroIntoSlot must return false for null slot.");

            // Attempt to recruit again into already occupied slot
            bool secondRecruit = roster.RecruitHeroIntoSlot("hero_archer_test", slot);
            Assert.IsFalse(secondRecruit, "RecruitHeroIntoSlot must return false for already occupied slot.");
        }

        [UnityTest]
        public IEnumerator HeroSelectionProxy_RespondsToPointerClickEvent()
        {
            GameObject slotObj = new GameObject("HeroSlot_TouchProxy");
            slotObj.transform.SetParent(testContainer.transform);
            HeroSlot slot = slotObj.AddComponent<HeroSlot>();

            yield return null;

            HeroSelectionProxy proxy = slotObj.GetComponentInChildren<HeroSelectionProxy>();
            Assert.IsNotNull(proxy, "HeroSlot must spawn a HeroSelectionProxy child on Start.");

            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            Assert.DoesNotThrow(() => proxy.OnPointerClick(pointerData), "OnPointerClick on HeroSelectionProxy should execute cleanly.");
        }
    }
}
