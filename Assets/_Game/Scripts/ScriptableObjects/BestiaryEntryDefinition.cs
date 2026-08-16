using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public enum EnemyCategory
    {
        Minion,
        Swarm,
        Ranged,
        Bruiser,
        Siege,
        Elite,
        Boss
    }

    [CreateAssetMenu(fileName = "NewBestiaryEntry", menuName = "Stonehold/Bestiary Entry")]
    public class BestiaryEntryDefinition : ScriptableObject
    {
        public string enemyId;
        public string displayName;
        public EnemyCategory category;
        public int threatLevel = 1; // 1 to 5 stars
        public int baseHealth = 100;
        public float baseSpeed = 2f;
        public int baseArmor = 0;
        public bool isBoss = false;

        [TextArea(3, 6)]
        public string loreDescription;

        [TextArea(2, 4)]
        public string tacticalCounterTips;

        public List<StatusEffectType> weaknesses = new List<StatusEffectType>();
        public List<StatusEffectType> resistances = new List<StatusEffectType>();
        public string recommendedHeroCounter = "Frost Mage";

        public Color themeColor = new Color(0.85f, 0.4f, 0.4f);
    }
}
