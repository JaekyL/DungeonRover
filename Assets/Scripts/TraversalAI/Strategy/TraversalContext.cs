using TraversalAI.Core;
using TraversalAI.Perception;

namespace TraversalAI.Strategy
{
    /// <summary>
    /// Context data provided to traversal strategies for decision-making.
    /// </summary>
    public class TraversalContext
    {
        public TraversalDungeonGraph Graph { get; set; }
        public PerceivedDungeonState PerceivedState { get; set; }
        public MemorySystem Memory { get; set; }
        public int CurrentNodeId { get; set; }
        public int TargetNodeId { get; set; } = -1;
        public float DangerTolerance { get; set; } = 0.5f;
        public InfluenceMap.InfluenceSampler InfluenceSampler { get; set; }
    }
}

