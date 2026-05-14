using System.Collections.Generic;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Strategy.Strategies
{
    /// <summary>
    /// Straight bias traversal: prefers continuing in the same direction.
    /// Good for efficient traversal with minimal backtracking.
    /// </summary>
    public class StraightBiasTraversal : ITraversalStrategy
    {
        public string StrategyName => "Straight Bias";

        public TraversalDecision Evaluate(TraversalContext context)
        {
            var decision = new TraversalDecision { Reasoning = "Straight Bias: Continue forward" };
            var currentNode = context.Graph.GetNode(context.CurrentNodeId);
            if (currentNode == null) return decision;

            var history = context.Memory.VisitHistory;
            int previousNodeId = history.Count >= 2 ? history[history.Count - 2] : -1;

            Vector3 forwardDir = Vector3.forward;
            if (previousNodeId >= 0)
            {
                var prevNode = context.Graph.GetNode(previousNodeId);
                if (prevNode != null)
                    forwardDir = (currentNode.WorldPosition - prevNode.WorldPosition).normalized;
            }

            var neighbors = context.Graph.GetNeighbors(context.CurrentNodeId);

            foreach (var neighbor in neighbors)
            {
                Vector3 toNeighbor = (neighbor.WorldPosition - currentNode.WorldPosition).normalized;
                float alignment = Vector3.Dot(forwardDir, toNeighbor);
                float normalizedAlignment = (alignment + 1f) / 2f;

                float score;
                string reason;

                if (!context.Memory.HasVisited(neighbor.Id))
                {
                    score = 0.5f + normalizedAlignment * 0.5f;
                    reason = $"Unvisited (alignment: {alignment:F2})";
                }
                else
                {
                    score = normalizedAlignment * 0.2f;
                    reason = $"Visited (alignment: {alignment:F2})";
                }

                decision.RankedCandidates.Add(new ScoredNode(neighbor.Id, score, reason));
            }

            decision.RankedCandidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            return decision;
        }

        public float GetEdgeWeightBias(DungeonEdge edge, TraversalContext context) => 1f;
    }
}

