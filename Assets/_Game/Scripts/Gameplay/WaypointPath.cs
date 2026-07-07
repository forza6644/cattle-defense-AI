using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Component placed on the Path GameObject that holds waypoint coordinates.
    /// Supports automatic child transform detection or a hardcoded fallback
    /// representing the scene's visual road layout.
    /// </summary>
    public class WaypointPath : MonoBehaviour
    {
        [Tooltip("Configure waypoint positions in world space. If empty, child Transforms are used. If still empty, a default path matching the visual road bends is generated.")]
        [SerializeField] private Vector3[] waypoints;

        public Vector3[] Points => waypoints;

        private void Awake()
        {
            InitializeWaypoints();
        }

        private void InitializeWaypoints()
        {
            // If the designer hasn't manually set up coordinates in the Inspector
            if (waypoints == null || waypoints.Length == 0)
            {
                // Check if there are child transforms under Path
                int childCount = transform.childCount;
                if (childCount > 0)
                {
                    waypoints = new Vector3[childCount];
                    for (int i = 0; i < childCount; i++)
                    {
                        waypoints[i] = transform.GetChild(i).position;
                    }
                }
                else
                {
                    // Default fallback path matching the road pieces
                    waypoints = new Vector3[]
                    {
                        new Vector3(-6f, 0.5f, 0f),      // SpawnPoint
                        new Vector3(-2f, 0.5f, 1.4f),    // RoadBend1
                        new Vector3(3f, 0.5f, -1.4f),    // RoadBend2
                        new Vector3(6f, 0.5f, 0f)        // Castle Keep
                    };
                    Debug.Log("WaypointPath: No waypoints configured in Inspector or child objects. Using default road-bend path.");
                }
            }
        }

        private void OnDrawGizmos()
        {
            Vector3[] pts = waypoints;
            if (pts == null || pts.Length == 0)
            {
                if (transform.childCount > 0)
                {
                    pts = new Vector3[transform.childCount];
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        pts[i] = transform.GetChild(i).position;
                    }
                }
                else
                {
                    pts = new Vector3[]
                    {
                        new Vector3(-6f, 0.5f, 0f),
                        new Vector3(-2f, 0.5f, 1.4f),
                        new Vector3(3f, 0.5f, -1.4f),
                        new Vector3(6f, 0.5f, 0f)
                    };
                }
            }

            if (pts == null || pts.Length < 2) return;

            Gizmos.color = Color.green;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Gizmos.DrawLine(pts[i], pts[i + 1]);
                Gizmos.DrawSphere(pts[i], 0.2f);
            }
            Gizmos.DrawSphere(pts[pts.Length - 1], 0.2f);
        }
    }
}
