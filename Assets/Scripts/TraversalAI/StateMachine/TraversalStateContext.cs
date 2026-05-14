using TraversalAI.Core;
using TraversalAI.Goals;
using TraversalAI.Perception;
using TraversalAI.Strategy;

namespace TraversalAI.StateMachine
{
    /// <summary>
    /// Context shared between traversal states, providing access to all AI subsystems.
    /// </summary>
    public class TraversalStateContext
    {
        public TraversalDungeonGraph Graph { get; set; }
        public PerceivedDungeonState PerceivedState { get; set; }
        public MemorySystem Memory { get; set; }
        public ITraversalGoal CurrentGoal { get; set; }
        public ITraversalStrategy CurrentStrategy { get; set; }
        public GoalContext GoalContext { get; set; }
        public TraversalContext StrategyContext { get; set; }
        public int CurrentNodeId { get; set; }
        public float CurrentHealth { get; set; } = 1f;
        public float DangerTolerance { get; set; } = 0.5f;

        /// <summary>Request the controller to move to a specific node.</summary>
        public System.Action<int> RequestMoveTo { get; set; }

        /// <summary>Request the controller to transition state.</summary>
        public System.Action<TraversalStateType> RequestStateChange { get; set; }
    }
}

