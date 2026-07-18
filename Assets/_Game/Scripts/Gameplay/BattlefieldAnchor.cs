using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public enum BattlefieldAnchorType { Trap = 0, Defense = 1 }

    public sealed class BattlefieldAnchor : MonoBehaviour
    {
        private static readonly List<BattlefieldAnchor> activeAnchors = new List<BattlefieldAnchor>();
        public static event Action<BattlefieldAnchor> Registered;
        public static event Action<BattlefieldAnchor> Unregistered;
        [SerializeField] private BattlefieldAnchorType anchorType;
        private Component occupant;

        public BattlefieldAnchorType AnchorType => anchorType;
        public bool IsOccupied => occupant != null;
        public Component Occupant => occupant;
        public static IReadOnlyList<BattlefieldAnchor> ActiveAnchors => activeAnchors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeAnchors.Clear();
            Registered = null;
            Unregistered = null;
        }

        public void Configure(BattlefieldAnchorType type) => anchorType = type;

        private void OnEnable()
        {
            if (!activeAnchors.Contains(this)) activeAnchors.Add(this);
            Registered?.Invoke(this);
        }

        private void OnDisable()
        {
            Release(occupant);
            activeAnchors.Remove(this);
            Unregistered?.Invoke(this);
        }

        public bool TryOccupy(Component value)
        {
            if (value == null || occupant != null) return false;
            occupant = value;
            return true;
        }

        public void Release(Component value)
        {
            if (occupant == value || value == null) occupant = null;
        }
    }
}
