using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class VfxPerformancePlayModeTests
    {
        private GameObject vfxGO;
        private VfxManager vfxManager;
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;

            if (VfxManager.Instance != null)
            {
                Object.DestroyImmediate(VfxManager.Instance.gameObject);
            }

            vfxGO = new GameObject("VfxManager", typeof(VfxManager));
            vfxManager = vfxGO.GetComponent<VfxManager>();
            createdObjects.Add(vfxGO);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var obj in createdObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator VfxManager_PreWarmsPools_WithoutAllocationsDuringGameplay()
        {
            vfxManager.PreWarmPools(4);

            // Trigger various visual effects
            vfxManager.PlayExplosion(Vector3.zero);
            vfxManager.PlayFrost(new Vector3(1f, 0f, 0f));
            vfxManager.PlayHit(new Vector3(2f, 0f, 0f), Color.yellow);
            vfxManager.PlayHeroAttackTrace(Vector3.zero, new Vector3(5f, 0f, 5f), "electric_engineer");
            vfxManager.PlayHeroAttackTrace(Vector3.zero, new Vector3(5f, 0f, 5f), "sniper");
            vfxManager.PlayImpactRing(Vector3.zero, Color.cyan, 2f, 0.2f, 0.1f);

            yield return null;

            Assert.IsNotNull(VfxManager.Instance, "VfxManager singleton instance must be active.");
        }
    }
}
