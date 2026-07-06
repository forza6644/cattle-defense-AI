using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Data-driven catalogue of every clip the game plays. One asset under
    /// ScriptableObjects/Audio; swap the placeholder clips for authored audio later
    /// without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Stonehold/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Header("Music")]
        public AudioClip musicGameplay;
        public AudioClip musicVictory;
        public AudioClip musicDefeat;

        [Header("UI")]
        public AudioClip button;

        [Header("Combat")]
        public AudioClip cannonExplosion;
        public AudioClip frostHit;
        public AudioClip enemyDeath;
        public AudioClip gold;

        [Header("Towers / Castle")]
        public AudioClip place;
        public AudioClip upgrade;
        public AudioClip castleDamage;
    }
}
