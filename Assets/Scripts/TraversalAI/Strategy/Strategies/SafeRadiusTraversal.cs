using System.Collections.Generic;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Strategy.Strategies
{
    /// <summary>
    /// Safe radius traversal: avoids high-danger areas. Expands gradually as safe areas are confirmed.
    /// Good for cautious/sneaky playstyles.
    /// </summary>
    public class SafeRadiusTraversal : ITraversalStrategy
    {
        public string StrategyName => "Safe Radius";

        public TraversalDecision Evaluate(TraversalContext context)
        {
            var decision = new TraversalDecision { Reasoning = "Safe Radius: Stay within safe bounds" };
            var neighbors = context.Graph.GetNeighbors(context.CurrentNodeId);

            foreach (var neighbor in neighbors)
            {
                float danger = neighbor.BaseDangerLevel;
                float safetyScore = 1f - Mathf.Clamp01(danger / Mathf.Max(0.1f, context.DangerTolerance));

                float score;
                string reason;

                if (!context.Memory.HasVisited(neighbor.Id))
                {
                    score = safetyScore * 0.8f;
                    reason = $"Unvisited (safety: {safetyScore:F2})";
                }
                else
                {
                    score = safetyScore * 0.2f;
                    reason = $"Visited (safety: {safetyScore:F2})";
                }

                if (neighbor.HasTag(NodeTag.SafeZone))
                {
                    score += 0.3f;
                    reason += " +safe zone";
                }

                if (neighbor.HasTag(NodeTag.EnemyPresence))
                {
                    score *= 0.2f;
                    reason += " -enemy!";
                }

                decision.RankedCandidates.Add(new ScoredNode(neighbor.Id, score, reason));
            }

            decision.RankedCandidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            return decision;
        }

        public float GetEdgeWeightBias(DungeonEdge edge, TraversalContext context)
        {
            return 1f + edge.DangerLevel * 3f;
        }
    }
}

