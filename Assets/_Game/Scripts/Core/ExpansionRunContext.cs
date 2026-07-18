using UnityEngine;

namespace Stonehold
{
    public static class ExpansionRunContext
    {
        public static StageData StageOverride { get; private set; }

        public static void SetStageOverride(StageData stage) => StageOverride = stage;
        public static void Clear() => StageOverride = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();
    }
}
