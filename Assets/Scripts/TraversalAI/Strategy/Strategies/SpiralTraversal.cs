using System.Collections.Generic;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Strategy.Strategies
{
    /// <summary>
    /// Spiral traversal: explores outward from the starting point in expanding circles.
    /// Good for systematic area coverage.
    /// </summary>
    public class SpiralTraversal : ITraversalStrategy
    {
        public string StrategyName => "Spiral";

        public TraversalDecision Evaluate(TraversalContext context)
        {
            var decision = new TraversalDecision { Reasoning = "Spiral: Expanding outward coverage" };
            var currentNode = context.Graph.GetNode(context.CurrentNodeId);
            if (currentNode == null) return decision;

            int startNodeId = context.Memory.VisitHistory.Count > 0 ? context.Memory.VisitHistory[0] : context.CurrentNodeId;
            var startNode = context.Graph.GetNode(startNodeId) ?? currentNode;

            var neighbors = context.Graph.GetNeighbors(context.CurrentNodeId);

            foreach (var neighbor in neighbors)
            {
                float distFromCenter = Vector3.Distance(startNode.WorldPosition, neighbor.WorldPosition);
                float currentDistFromCenter = Vector3.Distance(startNode.WorldPosition, currentNode.WorldPosition);

                float score = 0f;
                string reason = "";

                if (!context.Memory.HasVisited(neighbor.Id))
                {
                    float distDiff = distFromCenter - currentDistFromCenter;
                    if (distDiff >= 0 && distDiff < 5f)
                    {
                        score = 1.0f;
                        reason = "Expanding spiral ring";
                    }
                    else if (distDiff < 0)
                    {
                        score = 0.3f;
                        reason = "Inward (filling gaps)";
                    }
                    else
                    {
                        score = 0.6f;
                        reason = "Far expansion";
                    }
                }
                else
                {
                    score = 0.1f;
                    reason = "Already visited";
                }

                decision.RankedCandidates.Add(new ScoredNode(neighbor.Id, score, reason));
            }

            decision.RankedCandidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            return decision;
        }

        public float GetEdgeWeightBias(DungeonEdge edge, TraversalContext context) => 1f;
    }
}

