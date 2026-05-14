using System.Collections.Generic;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Strategy.Strategies
{
    /// <summary>
    /// Breadth-first traversal: explores nearby nodes before venturing deeper.
    /// Good for cautious/thorough exploration.
    /// </summary>
    public class BreadthFirstTraversal : ITraversalStrategy
    {
        public string StrategyName => "Breadth-First";

        public TraversalDecision Evaluate(TraversalContext context)
        {
            var decision = new TraversalDecision { Reasoning = "BFS: Prefer closest unexplored nodes" };
            var neighbors = context.Graph.GetNeighbors(context.CurrentNodeId);

            foreach (var neighbor in neighbors)
            {
                float score = 0f;
                string reason = "";

                if (!context.Memory.HasVisited(neighbor.Id))
                {
                    score += 0.8f;
                    reason = "Unvisited nearby";

                    int connections = context.Graph.GetNeighbors(neighbor.Id).Count;
                    score += connections * 0.1f;
                    reason += $" +{connections} connections";
                }
                else
                {
                    int visitCount = context.Memory.GetVisitCount(neighbor.Id);
                    score += Mathf.Max(0.01f, 0.2f - visitCount * 0.05f);
                    reason = $"Visited {visitCount}x";
                }

                decision.RankedCandidates.Add(new ScoredNode(neighbor.Id, score, reason));
            }

            decision.RankedCandidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            return decision;
        }

        public float GetEdgeWeightBias(DungeonEdge edge, TraversalContext context)
        {
            return 1f;
        }
    }
}

