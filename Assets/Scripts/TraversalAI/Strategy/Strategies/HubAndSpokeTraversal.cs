using System.Collections.Generic;
using System.Linq;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Strategy.Strategies
{
    /// <summary>
    /// Hub-and-spoke traversal: identifies hub rooms and explores outward branches,
    /// returning to the hub between explorations.
    /// </summary>
    public class HubAndSpokeTraversal : ITraversalStrategy
    {
        public string StrategyName => "Hub & Spoke";

        private int _currentHubId = -1;
        private bool _returningToHub;

        public TraversalDecision Evaluate(TraversalContext context)
        {
            var decision = new TraversalDecision { Reasoning = "Hub&Spoke: Explore spokes from hub" };
            var currentNode = context.Graph.GetNode(context.CurrentNodeId);
            if (currentNode == null) return decision;

            if (_currentHubId < 0 || !context.Memory.HasVisited(_currentHubId))
                _currentHubId = FindBestHub(context);

            var neighbors = context.Graph.GetNeighbors(context.CurrentNodeId);

            if (context.CurrentNodeId == _currentHubId)
            {
                _returningToHub = false;
                foreach (var neighbor in neighbors)
                {
                    float score;
                    string reason;

                    if (!context.Memory.HasVisited(neighbor.Id))
                    {
                        score = 1.0f;
                        reason = "Unexplored spoke";
                    }
                    else
                    {
                        var unvisitedBeyond = context.Memory.GetUnvisitedNeighbors(neighbor.Id, context.Graph);
                        score = unvisitedBeyond.Count > 0 ? 0.6f : 0.05f;
                        reason = unvisitedBeyond.Count > 0
                            ? $"Spoke leads to {unvisitedBeyond.Count} unvisited"
                            : "Fully explored spoke";
                    }

                    decision.RankedCandidates.Add(new ScoredNode(neighbor.Id, score, reason));
                }
            }
            else
            {
                bool hasUnvisitedAhead = neighbors.Any(n => !context.Memory.HasVisited(n.Id));

                if (hasUnvisitedAhead && !_returningToHub)
                {
                    foreach (var neighbor in neighbors)
                    {
                        float score = context.Memory.HasVisited(neighbor.Id) ? 0.1f : 0.9f;
                        decision.RankedCandidates.Add(new ScoredNode(neighbor.Id, score,
                            context.Memory.HasVisited(neighbor.Id) ? "Visited" : "Continuing spoke"));
                    }
                }
                else
                {
                    _returningToHub = true;
                    var hubNode = context.Graph.GetNode(_currentHubId);
                    foreach (var neighbor in neighbors)
                    {
                        float distToHub = hubNode != null
                            ? Vector3.Distance(neighbor.WorldPosition, hubNode.WorldPosition)
                            : float.MaxValue;
                        float score = 1f / (1f + distToHub);
                        decision.RankedCandidates.Add(new ScoredNode(neighbor.Id, score, "Returning to hub"));
                    }
                }
            }

            decision.RankedCandidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            return decision;
        }

        private int FindBestHub(TraversalContext context)
        {
            int bestId = context.CurrentNodeId;
            int bestConnections = 0;

            foreach (var node in context.Graph.Nodes)
            {
                if (node.HasTag(NodeTag.HubRoom)) return node.Id;
                int conn = context.Graph.GetNeighbors(node.Id).Count;
                if (conn > bestConnections)
                {
                    bestConnections = conn;
                    bestId = node.Id;
                }
            }
            return bestId;
        }

        public float GetEdgeWeightBias(DungeonEdge edge, TraversalContext context)
        {
            if (_returningToHub && _currentHubId >= 0)
            {
                int targetId = edge.GetOtherNode(context.CurrentNodeId);
                if (targetId == _currentHubId) return 0.5f;
            }
            return 1f;
        }
    }
}

