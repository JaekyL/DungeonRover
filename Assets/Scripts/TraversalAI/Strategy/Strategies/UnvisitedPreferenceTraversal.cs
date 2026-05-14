using System.Collections.Generic;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Strategy.Strategies
{
    /// <summary>
    /// Strongly prefers any unvisited node. Breaks ties by exploration value.
    /// Good baseline strategy.
    /// </summary>
    public class UnvisitedPreferenceTraversal : ITraversalStrategy
    {
        public string StrategyName => "Unvisited Preference";

        public TraversalDecision Evaluate(TraversalContext context)
        {
            var decision = new TraversalDecision { Reasoning = "Unvisited Preference: Go where we haven't been" };
            var neighbors = context.Graph.GetNeighbors(context.CurrentNodeId);

            foreach (var neighbor in neighbors)
            {
                float score;
                string reason;

                if (!context.Memory.HasVisited(neighbor.Id))
                {
                    var beyondUnvisited = context.Memory.GetUnvisitedNeighbors(neighbor.Id, context.Graph);
                    score = 1.0f + beyondUnvisited.Count * 0.1f;
                    reason = $"Unvisited (+{beyondUnvisited.Count} beyond)";
                }
                else
                {
                    int revisits = context.Memory.GetVisitCount(neighbor.Id);
                    score = Mathf.Max(0.01f, 0.2f / revisits);
                    reason = $"Visited {revisits}x";
                }

                decision.RankedCandidates.Add(new ScoredNode(neighbor.Id, score, reason));
            }

            decision.RankedCandidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            return decision;
        }

        public float GetEdgeWeightBias(DungeonEdge edge, TraversalContext context)
        {
            int targetId = edge.GetOtherNode(context.CurrentNodeId);
            return context.Memory.HasVisited(targetId) ? 2f : 0.5f;
        }
    }
}

