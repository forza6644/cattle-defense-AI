using UnityEngine;

namespace Stonehold
{
    public enum CrystalElement
    {
        Fire,
        Ice,
        Lightning,
        Stone,
        Shadow
    }

    [CreateAssetMenu(fileName = "StarterCrystal_", menuName = "Stonehold/Starter Crystal Definition")]
    public class StarterCrystalDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string crystalId = "crystal_lightning";
        public string displayName = "Lightning Crystal";
        [TextArea(2, 4)] public string description = "Fires rapid lightning bolts that chain between nearby enemies.";
        public CrystalElement element = CrystalElement.Lightning;

        [Header("Core Combat")]
        public float baseDamage = 15f;
        public float attacksPerSecond = 1.4f;
        public float attackRange = 14f;

        [Header("Elemental Behavior")]
        public float splashRadius = 0f;
        public int chainTargets = 3;
        public float statusMagnitude = 0f;
        public float statusDuration = 0f;
        public float damageOverTime = 0f;
        public float damageOverTimeDuration = 0f;

        [Header("Presentation References")]
        public Material crystalMaterial;
        public Mesh crystalMesh;
        public GameObject projectilePrefab;
    }
}
