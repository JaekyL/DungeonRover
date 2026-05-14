namespace TraversalAI.Goals
{
    /// <summary>
    /// Interface for all traversal goals the AI can pursue.
    /// Goals are generated dynamically based on perception and context.
    /// </summary>
    public interface ITraversalGoal
    {
        /// <summary>Unique type identifier for this goal.</summary>
        string GoalType { get; }

        /// <summary>Target node ID (-1 if no specific target).</summary>
        int TargetNodeId { get; }

        /// <summary>Base priority before utility scoring.</summary>
        float BasePriority { get; }

        /// <summary>Estimated risk of pursuing this goal [0..1].</summary>
        float EstimatedRisk { get; }

        /// <summary>Estimated reward value of completing this goal.</summary>
        float EstimatedReward { get; }

        /// <summary>Check if this goal is still valid given the current context.</summary>
        bool IsValid(GoalContext context);

        /// <summary>Check if this goal has been completed.</summary>
        bool IsComplete(GoalContext context);

        /// <summary>Human-readable description for debug UI.</summary>
        string GetDescription();
    }
}

