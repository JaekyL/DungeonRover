namespace TraversalAI.Strategy
{
    /// <summary>
    /// Interface for traversal strategies that influence how the AI explores.
    /// Strategies do NOT directly move the character — they only influence
    /// target selection, branch preference, and path weighting.
    /// </summary>
    public interface ITraversalStrategy
    {
        /// <summary>Display name for the strategy.</summary>
        string StrategyName { get; }

        /// <summary>
        /// Evaluate candidate nodes and return a ranked decision.
        /// </summary>
        TraversalDecision Evaluate(TraversalContext context);

        /// <summary>
        /// Apply path weight bias to an edge. Used by pathfinding to prefer certain paths.
        /// Returns a multiplier (lower = more preferred).
        /// </summary>
        float GetEdgeWeightBias(Core.DungeonEdge edge, TraversalContext context);
    }
}

