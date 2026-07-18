using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public sealed class BattlefieldAnchorManager : MonoBehaviour
    {
        public static BattlefieldAnchorManager Instance { get; private set; }
        private readonly List<BattlefieldAnchor> anchors = new List<BattlefieldAnchor>();
        public IReadOnlyList<BattlefieldAnchor> Anchors => anchors;
        public int OccupiedCount { get { int count = 0; for (int i = 0; i < anchors.Count; i++) if (anchors[i] != null && anchors[i].IsOccupied) count++; return count; } }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BattlefieldAnchor.Registered += Register;
            BattlefieldAnchor.Unregistered += Unregister;
            for (int i = 0; i < BattlefieldAnchor.ActiveAnchors.Count; i++) Register(BattlefieldAnchor.ActiveAnchors[i]);
        }

        private void OnDestroy()
        {
            BattlefieldAnchor.Registered -= Register;
            BattlefieldAnchor.Unregistered -= Unregister;
            if (Instance == this) Instance = null;
        }

        private void Register(BattlefieldAnchor anchor) { if (anchor != null && !anchors.Contains(anchor)) anchors.Add(anchor); }
        private void Unregister(BattlefieldAnchor anchor) { anchors.Remove(anchor); }

        public bool TryClaim(BattlefieldAnchorType type, Component occupant, out BattlefieldAnchor selected)
        {
            for (int i = 0; i < anchors.Count; i++)
            {
                BattlefieldAnchor anchor = anchors[i];
                if (anchor != null && anchor.AnchorType == type && anchor.TryOccupy(occupant)) { selected = anchor; return true; }
            }
            selected = null;
            return false;
        }

        public bool HasAvailableAnchor(BattlefieldAnchorType type)
        {
            for (int i = 0; i < anchors.Count; i++)
            {
                BattlefieldAnchor anchor = anchors[i];
                if (anchor != null && anchor.AnchorType == type && !anchor.IsOccupied) return true;
            }
            return false;
        }

        public void ResetForRun()
        {
            for (int i = 0; i < anchors.Count; i++) if (anchors[i] != null) anchors[i].Release(null);
        }
    }
}
