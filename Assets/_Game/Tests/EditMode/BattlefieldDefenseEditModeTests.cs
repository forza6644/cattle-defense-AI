using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Stonehold.Tests
{
    public class BattlefieldDefenseEditModeTests
    {
        private const string Root = "Assets/_Game/ScriptableObjects/BattlefieldDefenseQualification";
        private TrapDefinition Caltrops => AssetDatabase.LoadAssetAtPath<TrapDefinition>(Root + "/Caltrops.asset");
        private TrapDefinition Oil => AssetDatabase.LoadAssetAtPath<TrapDefinition>(Root + "/BurningOil.asset");
        private BattlefieldDefenseDefinition Barricade => AssetDatabase.LoadAssetAtPath<BattlefieldDefenseDefinition>(Root + "/WoodenBarricade.asset");

        [Test] public void Caltrops_IdentityAndCategoryAreStable() { Assert.That(Caltrops.stableId, Is.EqualTo("trap_caltrops")); Assert.That(Card("DeployCaltrops").cardCategory, Is.EqualTo(CardCategory.Trap)); }
        [Test] public void BurningOil_IdentityAndCategoryAreStable() { Assert.That(Oil.stableId, Is.EqualTo("trap_burning_oil")); Assert.That(Card("DeployBurningOil").cardCategory, Is.EqualTo(CardCategory.Trap)); }
        [Test] public void Barricade_IdentityAndCategoryAreStable() { Assert.That(Barricade.stableId, Is.EqualTo("defense_wooden_barricade")); Assert.That(Card("DeployWoodenBarricade").cardCategory, Is.EqualTo(CardCategory.BattlefieldDefense)); }
        [Test] public void StableIds_AreUnique() { Assert.That(new[] { Caltrops.stableId, Oil.stableId, Barricade.stableId }.Distinct().Count(), Is.EqualTo(3)); }
        [Test] public void Caltrops_PrototypeValuesAreBounded() { Assert.That(Caltrops.effectRadius, Is.EqualTo(2f)); Assert.That(Caltrops.duration, Is.EqualTo(18f)); Assert.That(Caltrops.damage, Is.EqualTo(2f)); Assert.That(Caltrops.triggerInterval, Is.EqualTo(0.9f)); Assert.That(Caltrops.statusEffectValue, Is.EqualTo(0.72f)); Assert.That(Caltrops.maxActive, Is.EqualTo(2)); }
        [Test] public void Oil_PrototypeValuesAreBounded() { Assert.That(Oil.effectRadius, Is.EqualTo(2.5f)); Assert.That(Oil.ignitionDelay, Is.EqualTo(0.65f)); Assert.That(Oil.burningDuration, Is.EqualTo(5f)); Assert.That(Oil.triggerInterval, Is.EqualTo(0.75f)); Assert.That(Oil.maxActive, Is.EqualTo(1)); }
        [Test] public void Barricade_PrototypeValuesAreBounded() { Assert.That(Barricade.health, Is.EqualTo(90f)); Assert.That(Barricade.armor, Is.EqualTo(2f)); Assert.That(Barricade.maxActive, Is.EqualTo(1)); Assert.That(Barricade.damage, Is.Zero); }
        [Test] public void QualificationCards_AreOutsideResourcesCards() { foreach (string guid in AssetDatabase.FindAssets("t:CardDefinition", new[] { Root })) Assert.That(AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/'), Does.Not.Contain("/Resources/Cards/")); }
        [Test] public void ProductionCards_RemainThirtyNine() { Assert.That(AssetDatabase.FindAssets("t:CardDefinition", new[] { "Assets/_Game/Resources/Cards" }).Length, Is.EqualTo(39)); }
        [Test] public void VerticalSlice18_RemainsEighteenEntries() { CardPoolDefinition pool = AssetDatabase.LoadAssetAtPath<CardPoolDefinition>("Assets/_Game/ScriptableObjects/CardPools/VerticalSlice18.asset"); Assert.That(pool.expectedCardCount, Is.EqualTo(18)); Assert.That(pool.cards, Has.Count.EqualTo(18)); }
        [Test] public void QualificationPool_HasExactlyThreeSupportedCards() { CardPoolDefinition pool = AssetDatabase.LoadAssetAtPath<CardPoolDefinition>(Root + "/Task13FQualificationPool.asset"); Assert.That(pool.stableId, Is.EqualTo("task13f_qualification")); Assert.That(pool.cards, Has.Count.EqualTo(3)); Assert.That(pool.cards.All(x => x.card.maxStacks == 1 && x.weight > 0f), Is.True); }
        [Test] public void DefinitionsAndCards_PassValidation() { var content = new BattlefieldContentDefinition[] { Caltrops, Oil, Barricade }; var issues = GameplayDataValidation.ValidateBattlefieldContents(content); issues.AddRange(GameplayDataValidation.ValidateCards(new[] { Card("DeployCaltrops"), Card("DeployBurningOil"), Card("DeployWoodenBarricade") })); Assert.That(GameplayDataValidation.HasErrors(issues), Is.False, string.Join("\n", issues.Select(x => x.Message))); }
        [Test] public void ProductionWaves_DoNotReferenceQualificationContent() { Assert.That(AssetDatabase.FindAssets("t:WaveData", new[] { Root }), Is.Empty); }

        private static CardDefinition Card(string name) => AssetDatabase.LoadAssetAtPath<CardDefinition>(Root + "/Cards/" + name + ".asset");
    }
}
