namespace TraversalAI.Goals
{
    /// <summary>
    /// Abstract base class for traversal goals with common functionality.
    /// </summary>
    [System.Serializable]
    public abstract class BaseTraversalGoal : ITraversalGoal
    {
        public abstract string GoalType { get; }
        public int TargetNodeId { get; set; } = -1;
        public float BasePriority { get; set; } = 0.5f;
        public float EstimatedRisk { get; set; }
        public float EstimatedReward { get; set; }

        public abstract bool IsValid(GoalContext context);
        public abstract bool IsComplete(GoalContext context);

        public virtual string GetDescription() => $"{GoalType} -> Node {TargetNodeId}";
    }
}

