using System.Collections.Generic;
using TraversalAI.Core;

namespace TraversalAI.Strategy.Strategies
{
    /// <summary>
    /// Depth-first traversal: aggressively pushes forward into unexplored territory.
    /// Good for aggressive/fast dungeon descent.
    /// </summary>
    public class DepthFirstTraversal : ITraversalStrategy
    {
        public string StrategyName => "Depth-First";

        public TraversalDecision Evaluate(TraversalContext context)
        {
            var decision = new TraversalDecision { Reasoning = "DFS: Prefer deepest unexplored path" };
            var neighbors = context.Graph.GetNeighbors(context.CurrentNodeId);

            foreach (var neighbor in neighbors)
            {
                float score = 0f;
                string reason = "";

                if (!context.Memory.HasVisited(neighbor.Id))
                {
                    score += 1.0f;
                    reason = "Unvisited";

                    int connections = context.Graph.GetNeighbors(neighbor.Id).Count;
                    if (connections <= 2)
                    {
                        score += 0.3f;
                        reason += " +narrow path";
                    }
                }
                else
                {
                    var unvisitedBeyond = context.Memory.GetUnvisitedNeighbors(neighbor.Id, context.Graph);
                    if (unvisitedBeyond.Count > 0)
                    {
                        score += 0.4f;
                        reason = $"Leads to {unvisitedBeyond.Count} unvisited";
                    }
                    else
                    {
                        score += 0.05f;
                        reason = "Already explored branch";
                    }
                }

                decision.RankedCandidates.Add(new ScoredNode(neighbor.Id, score, reason));
            }

            decision.RankedCandidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            return decision;
        }

        public float GetEdgeWeightBias(DungeonEdge edge, TraversalContext context)
        {
            int targetId = edge.GetOtherNode(context.CurrentNodeId);
            return context.Memory.HasVisited(targetId) ? 1.5f : 0.7f;
        }
    }
}

