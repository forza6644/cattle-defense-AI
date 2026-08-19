using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Authoritative three-lane battlefield math. Lane 0 = Left, 1 = Center, 2 = Right.
    /// Routes stay visually distinct for most of the march, then converge toward the
    /// single castle gate so existing range targeting becomes more valuable near the keep.
    /// </summary>
    public static class CombatLaneRouting
    {
        public const int LaneCount = 3;
        public const int Left = 0;
        public const int Center = 1;
        public const int Right = 2;

        public const float DefaultFallbackLaneSeparation = 3.5f;
        public const float DefaultWithinLaneHalfWidth = 0.55f;
        public const float MaxWithinLaneHalfWidth = 1.2f;
        public const int RoutePointCount = 7;

        /// <summary>Normalized depths (0 = portal, 1 = gate) for the cached waypoint set.</summary>
        public static readonly float[] RouteDepths =
        {
            0.00f,
            0.22f,
            0.45f,
            0.62f,
            0.78f,
            0.92f,
            1.00f
        };

        public static int ClampLane(int laneIndex)
        {
            if (laneIndex < 0)
            {
                return Center;
            }

            return laneIndex % LaneCount;
        }

        public static WaveLaneAssignment ToAssignment(int laneIndex)
        {
            switch (ClampLane(laneIndex))
            {
                case Left: return WaveLaneAssignment.Left;
                case Right: return WaveLaneAssignment.Right;
                default: return WaveLaneAssignment.Center;
            }
        }

        public static int ResolveLane(
            WaveLaneAssignment assignment,
            int spawnOrdinal,
            EnemyClassification classification)
        {
            switch (assignment)
            {
                case WaveLaneAssignment.Left:
                    return Left;
                case WaveLaneAssignment.Center:
                    return Center;
                case WaveLaneAssignment.Right:
                    return Right;
                default:
                    if (classification == EnemyClassification.Boss)
                    {
                        return Center;
                    }

                    int ordinal = spawnOrdinal < 0 ? 0 : spawnOrdinal;
                    return ordinal % LaneCount;
            }
        }

        public static Vector3 SyntheticSpawnPosition(int laneIndex, Vector3 origin, float laneSeparation)
        {
            float separation = Mathf.Max(0.5f, laneSeparation);
            float x = origin.x + (ClampLane(laneIndex) - 1) * separation;
            return new Vector3(x, origin.y, origin.z);
        }

        public static Vector3 ResolveSpawnPosition(
            int laneIndex,
            Vector3[] portalPositions,
            Vector3 fallbackOrigin,
            float fallbackSeparation)
        {
            int lane = ClampLane(laneIndex);
            if (portalPositions != null && portalPositions.Length >= LaneCount)
            {
                Vector3 portal = portalPositions[lane];
                return new Vector3(portal.x, portal.y, portal.z);
            }

            return SyntheticSpawnPosition(lane, fallbackOrigin, fallbackSeparation);
        }

        /// <summary>
        /// 0 at the portal, 1 at the gate. Stays near 0 through the far field so
        /// Left/Center/Right remain readable, then eases in toward the keep.
        /// </summary>
        public static float ConvergeAmount(float normalizedDepth)
        {
            float t = Mathf.Clamp01(normalizedDepth);
            if (t <= 0.32f)
            {
                return 0.10f * (t / 0.32f);
            }

            float u = Mathf.SmoothStep(0f, 1f, (t - 0.32f) / 0.68f);
            return Mathf.Lerp(0.10f, 1f, u);
        }

        public static float LaneXAtDepth(float spawnX, float castleX, float normalizedDepth)
        {
            return Mathf.Lerp(spawnX, castleX, ConvergeAmount(normalizedDepth));
        }

        public static float NormalizedDepth(float z, float spawnZ, float castleZ)
        {
            float span = castleZ - spawnZ;
            if (Mathf.Abs(span) < 0.001f)
            {
                return 1f;
            }

            return Mathf.Clamp01((z - spawnZ) / span);
        }

        public static Vector3[] BuildRoute(int laneIndex, Vector3 spawnPos, Vector3 castlePos)
        {
            Vector3[] points = new Vector3[RoutePointCount];
            for (int i = 0; i < RoutePointCount; i++)
            {
                float t = RouteDepths[i];
                points[i] = new Vector3(
                    LaneXAtDepth(spawnPos.x, castlePos.x, t),
                    spawnPos.y,
                    Mathf.Lerp(spawnPos.z, castlePos.z, t));
            }

            return points;
        }

        public static int InferLaneFromPath(Vector3[] points, float centerDeadzone = 2f)
        {
            if (points == null || points.Length == 0)
            {
                return Center;
            }

            float x = points[0].x;
            if (x < -centerDeadzone)
            {
                return Left;
            }

            if (x > centerDeadzone)
            {
                return Right;
            }

            return Center;
        }

        public static bool IsCombatXInLane(float x, float laneX, float maxDrift)
        {
            return Mathf.Abs(x - laneX) <= maxDrift;
        }

        public static bool RouteKeepsLaneIdentity(Vector3[] points, int laneIndex)
        {
            if (points == null || points.Length < 2)
            {
                return false;
            }

            int lane = ClampLane(laneIndex);
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / (float)(points.Length - 1);
                if (lane == Left && points[i].x > 0.35f && t < 0.92f)
                {
                    return false;
                }

                if (lane == Right && points[i].x < -0.35f && t < 0.92f)
                {
                    return false;
                }

                if (lane == Center && Mathf.Abs(points[i].x) > 1.6f)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool RoutesAreDistinct(Vector3[] left, Vector3[] center, Vector3[] right, float minEarlySeparation)
        {
            if (left == null || center == null || right == null)
            {
                return false;
            }

            int count = Mathf.Min(left.Length, Mathf.Min(center.Length, right.Length));
            if (count < 2)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? i / (float)(count - 1) : 0f;
                if (t >= 0.62f)
                {
                    continue;
                }

                float required = Mathf.Lerp(minEarlySeparation, minEarlySeparation * 0.45f, t / 0.62f);
                if (Mathf.Abs(center[i].x - left[i].x) < required)
                {
                    return false;
                }

                if (Mathf.Abs(right[i].x - center[i].x) < required)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool RoutesDoNotCross(Vector3[] left, Vector3[] center, Vector3[] right)
        {
            if (left == null || center == null || right == null)
            {
                return false;
            }

            int count = Mathf.Min(left.Length, Mathf.Min(center.Length, right.Length));
            int limit = Mathf.Max(1, count - 1);
            for (int i = 0; i < limit; i++)
            {
                if (left[i].x >= center[i].x - 0.05f)
                {
                    return false;
                }

                if (right[i].x <= center[i].x + 0.05f)
                {
                    return false;
                }
            }

            return true;
        }

        public static float ClampWithinLaneOffset(float offset, float withinLaneHalfWidth)
        {
            float limit = Mathf.Clamp(withinLaneHalfWidth, 0f, MaxWithinLaneHalfWidth);
            return Mathf.Clamp(offset, -limit, limit);
        }
    }
}
