using TraversalAI.Core;
using TraversalAI.Perception;

namespace TraversalAI.Goals
{
    /// <summary>
    /// Contextual information available to goals for validity/completion checks.
    /// Passed to goal generation and evaluation systems.
    /// </summary>
    public class GoalContext
    {
        public TraversalDungeonGraph DungeonGraph { get; set; }
        public PerceivedDungeonState PerceivedState { get; set; }
        public MemorySystem Memory { get; set; }
        public int CurrentNodeId { get; set; }
        public int CurrentFloor { get; set; }
        public float CurrentHealth { get; set; } = 1f;
        public float CurrentResources { get; set; } = 1f;
        public float InventoryFullness { get; set; }
        public float CurrentTime { get; set; }
        public float DangerTolerance { get; set; } = 0.5f;
    }
}

