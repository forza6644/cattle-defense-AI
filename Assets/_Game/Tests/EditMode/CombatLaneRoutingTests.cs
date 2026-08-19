using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class CombatLaneRoutingTests
    {
        private static readonly Vector3 LeftPortal = new Vector3(-10.5f, 0.1f, 38.6f);
        private static readonly Vector3 CenterPortal = new Vector3(0f, 0.1f, 38.6f);
        private static readonly Vector3 RightPortal = new Vector3(10.5f, 0.1f, 38.6f);
        private static readonly Vector3 Castle = new Vector3(0f, 0.1f, 0.4f);

        [Test]
        public void ClampLane_WrapsAndRejectsNegative()
        {
            Assert.That(CombatLaneRouting.ClampLane(-1), Is.EqualTo(CombatLaneRouting.Center));
            Assert.That(CombatLaneRouting.ClampLane(0), Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(CombatLaneRouting.ClampLane(2), Is.EqualTo(CombatLaneRouting.Right));
            Assert.That(CombatLaneRouting.ClampLane(3), Is.EqualTo(CombatLaneRouting.Left));
        }

        [Test]
        public void ResolveLane_ExplicitAssignments_MapToLeftCenterRight()
        {
            Assert.That(CombatLaneRouting.ResolveLane(WaveLaneAssignment.Left, 99, EnemyClassification.Normal), Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(CombatLaneRouting.ResolveLane(WaveLaneAssignment.Center, 99, EnemyClassification.Normal), Is.EqualTo(CombatLaneRouting.Center));
            Assert.That(CombatLaneRouting.ResolveLane(WaveLaneAssignment.Right, 99, EnemyClassification.Normal), Is.EqualTo(CombatLaneRouting.Right));
        }

        [Test]
        public void ResolveLane_Auto_DistributesAcrossThreeLanes()
        {
            int[] lanes = new int[9];
            for (int i = 0; i < lanes.Length; i++)
            {
                lanes[i] = CombatLaneRouting.ResolveLane(WaveLaneAssignment.Auto, i, EnemyClassification.Normal);
            }

            CollectionAssert.AreEqual(
                new[] { 0, 1, 2, 0, 1, 2, 0, 1, 2 },
                lanes);
        }

        [Test]
        public void ResolveLane_AutoBoss_UsesCenter()
        {
            Assert.That(
                CombatLaneRouting.ResolveLane(WaveLaneAssignment.Auto, 0, EnemyClassification.Boss),
                Is.EqualTo(CombatLaneRouting.Center));
            Assert.That(
                CombatLaneRouting.ResolveLane(WaveLaneAssignment.Auto, 5, EnemyClassification.Boss),
                Is.EqualTo(CombatLaneRouting.Center));
        }

        [Test]
        public void ResolveSpawnPosition_ThreePortals_UsesMatchingPortal()
        {
            Vector3[] portals = { LeftPortal, CenterPortal, RightPortal };
            Assert.That(CombatLaneRouting.ResolveSpawnPosition(0, portals, Vector3.zero, 3.5f).x, Is.EqualTo(-10.5f));
            Assert.That(CombatLaneRouting.ResolveSpawnPosition(1, portals, Vector3.zero, 3.5f).x, Is.EqualTo(0f));
            Assert.That(CombatLaneRouting.ResolveSpawnPosition(2, portals, Vector3.zero, 3.5f).x, Is.EqualTo(10.5f));
        }

        [Test]
        public void ResolveSpawnPosition_NoPortals_SynthesizesThreeColumns()
        {
            Vector3 origin = new Vector3(0f, 0.1f, 16f);
            float left = CombatLaneRouting.ResolveSpawnPosition(0, null, origin, 3.5f).x;
            float center = CombatLaneRouting.ResolveSpawnPosition(1, null, origin, 3.5f).x;
            float right = CombatLaneRouting.ResolveSpawnPosition(2, null, origin, 3.5f).x;
            Assert.That(left, Is.LessThan(center));
            Assert.That(right, Is.GreaterThan(center));
            Assert.That(center, Is.EqualTo(0f));
        }

        [Test]
        public void BuildRoute_KeepsLaneIdentity_AndConvergesProgressively()
        {
            Vector3[] left = CombatLaneRouting.BuildRoute(CombatLaneRouting.Left, LeftPortal, Castle);
            Vector3[] center = CombatLaneRouting.BuildRoute(CombatLaneRouting.Center, CenterPortal, Castle);
            Vector3[] right = CombatLaneRouting.BuildRoute(CombatLaneRouting.Right, RightPortal, Castle);

            Assert.That(left.Length, Is.EqualTo(CombatLaneRouting.RoutePointCount));
            Assert.That(CombatLaneRouting.RoutesAreDistinct(left, center, right, 6f), Is.True);
            Assert.That(CombatLaneRouting.RoutesDoNotCross(left, center, right), Is.True);
            Assert.That(CombatLaneRouting.RouteKeepsLaneIdentity(left, CombatLaneRouting.Left), Is.True);
            Assert.That(CombatLaneRouting.RouteKeepsLaneIdentity(center, CombatLaneRouting.Center), Is.True);
            Assert.That(CombatLaneRouting.RouteKeepsLaneIdentity(right, CombatLaneRouting.Right), Is.True);

            float farLeft = CombatLaneRouting.LaneXAtDepth(LeftPortal.x, Castle.x, 0.25f);
            float midLeft = CombatLaneRouting.LaneXAtDepth(LeftPortal.x, Castle.x, 0.50f);
            float nearLeft = CombatLaneRouting.LaneXAtDepth(LeftPortal.x, Castle.x, 0.75f);
            Assert.That(farLeft, Is.LessThan(-8.5f), "At 75% remaining depth the left lane must still be far left.");
            Assert.That(midLeft, Is.GreaterThan(farLeft), "Lanes must begin converging by mid-field.");
            Assert.That(midLeft, Is.LessThan(-6.0f), "Mid-field must not collapse into a highway.");
            Assert.That(nearLeft, Is.GreaterThan(midLeft), "Late field continues converging.");
            Assert.That(nearLeft, Is.LessThan(-1.5f), "At 25% remaining depth left must still be left of the gate.");
            Assert.That(center[0].x, Is.EqualTo(0f).Within(0.05f));
        }

        [Test]
        public void BuildRoute_DoesNotCollapseEarly_AndMeetsAtTheGate()
        {
            Vector3[] left = CombatLaneRouting.BuildRoute(0, LeftPortal, Castle);
            Vector3[] right = CombatLaneRouting.BuildRoute(2, RightPortal, Castle);
            float midGap = Mathf.Abs(
                CombatLaneRouting.LaneXAtDepth(RightPortal.x, Castle.x, 0.50f)
                - CombatLaneRouting.LaneXAtDepth(LeftPortal.x, Castle.x, 0.50f));
            float finalGap = Mathf.Abs(right[right.Length - 1].x - left[left.Length - 1].x);
            Assert.That(midGap, Is.GreaterThan(12f));
            Assert.That(finalGap, Is.LessThan(0.2f));
        }

        [Test]
        public void InferLaneFromPath_UsesSpawnX()
        {
            Assert.That(CombatLaneRouting.InferLaneFromPath(new[] { LeftPortal, Castle }), Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(CombatLaneRouting.InferLaneFromPath(new[] { CenterPortal, Castle }), Is.EqualTo(CombatLaneRouting.Center));
            Assert.That(CombatLaneRouting.InferLaneFromPath(new[] { RightPortal, Castle }), Is.EqualTo(CombatLaneRouting.Right));
        }

        [Test]
        public void ClampWithinLaneOffset_NeverReachesNeighborLane()
        {
            Assert.That(CombatLaneRouting.ClampWithinLaneOffset(5.2f, 0.55f), Is.EqualTo(0.55f).Within(0.001f));
            Assert.That(CombatLaneRouting.ClampWithinLaneOffset(-9f, 0.55f), Is.EqualTo(-0.55f).Within(0.001f));
        }

        [Test]
        public void WaveSpawnEntry_DefaultLaneAssignment_IsAuto()
        {
            var entry = new WaveData.SpawnEntry();
            Assert.That(entry.laneAssignment, Is.EqualTo(WaveLaneAssignment.Auto));
        }

        [Test]
        public void BuildRoute_LeftCenterRight_NeverCross()
        {
            Vector3[] left = CombatLaneRouting.BuildRoute(0, LeftPortal, Castle);
            Vector3[] center = CombatLaneRouting.BuildRoute(1, CenterPortal, Castle);
            Vector3[] right = CombatLaneRouting.BuildRoute(2, RightPortal, Castle);
            Assert.That(CombatLaneRouting.RoutesDoNotCross(left, center, right), Is.True);
        }

        [Test]
        public void ConvergeAmount_DoesNotCollapseFarField()
        {
            Assert.That(CombatLaneRouting.ConvergeAmount(0.25f), Is.LessThan(0.12f));
            Assert.That(CombatLaneRouting.ConvergeAmount(0.50f), Is.GreaterThan(0.15f));
            Assert.That(CombatLaneRouting.ConvergeAmount(0.50f), Is.LessThan(0.45f));
            Assert.That(CombatLaneRouting.ConvergeAmount(0.75f), Is.GreaterThan(0.55f));
            Assert.That(CombatLaneRouting.ConvergeAmount(1f), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Scatter_IsBoundedByDefaultAndHardCap()
        {
            Assert.That(CombatLaneRouting.ClampWithinLaneOffset(99f, CombatLaneRouting.DefaultWithinLaneHalfWidth), Is.EqualTo(0.55f).Within(0.001f));
            Assert.That(CombatLaneRouting.ClampWithinLaneOffset(99f, 4f), Is.EqualTo(CombatLaneRouting.MaxWithinLaneHalfWidth).Within(0.001f));
            Assert.That(CombatLaneRouting.MaxWithinLaneHalfWidth, Is.EqualTo(1.2f).Within(0.001f));
        }

        [Test]
        public void WaveComposition_SupportsSingleTwoAndThreeLanePressure()
        {
            Assert.That(CombatLaneRouting.ResolveLane(WaveLaneAssignment.Left, 0, EnemyClassification.Normal), Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(CombatLaneRouting.ResolveLane(WaveLaneAssignment.Left, 1, EnemyClassification.Normal), Is.EqualTo(CombatLaneRouting.Left));

            Assert.That(CombatLaneRouting.ResolveLane(WaveLaneAssignment.Left, 0, EnemyClassification.Normal), Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(CombatLaneRouting.ResolveLane(WaveLaneAssignment.Right, 1, EnemyClassification.Normal), Is.EqualTo(CombatLaneRouting.Right));

            Assert.That(CombatLaneRouting.ResolveLane(WaveLaneAssignment.Auto, 0, EnemyClassification.Normal), Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(CombatLaneRouting.ResolveLane(WaveLaneAssignment.Auto, 1, EnemyClassification.Normal), Is.EqualTo(CombatLaneRouting.Center));
            Assert.That(CombatLaneRouting.ResolveLane(WaveLaneAssignment.Auto, 2, EnemyClassification.Normal), Is.EqualTo(CombatLaneRouting.Right));
        }
    }
}
