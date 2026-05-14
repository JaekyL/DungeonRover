using TraversalAI.Core;

namespace TraversalAI.BehaviorRules
{
    /// <summary>
    /// Context data used by behavior rule conditions.
    /// </summary>
    [System.Serializable]
    public class BehaviorContext
    {
        public float Health = 1f;
        public float Resources = 1f;
        public float InventoryFullness;
        public float CurrentDangerLevel;
        public float ExplorationProgress;
        public int NearbyUnexploredCount;
        public int NearbyEnemyCount;
        public float DistanceToSafeZone;
        public NodeTag CurrentNodeTags;
        public bool StairsAvailable;
    }
}

