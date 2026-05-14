using System.Collections.Generic;
using System.Linq;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Pathfinding
{
    /// <summary>
    /// A* pathfinder operating on the TraversalDungeonGraph.
    /// Supports danger-aware pathfinding with configurable edge weight modifiers.
    /// </summary>
    public class GraphPathfinder : IPathfinder
    {
        public PathResult FindPath(PathRequest request, TraversalDungeonGraph graph)
        {
            var startNode = graph.GetNode(request.StartNodeId);
            var endNode = graph.GetNode(request.EndNodeId);

            if (startNode == null || endNode == null)
                return PathResult.Failed("Invalid start or end node.");

            if (request.StartNodeId == request.EndNodeId)
                return new PathResult { Success = true, NodePath = new List<int> { request.StartNodeId }, TotalCost = 0f };

            // A* implementation
            var openSet = new SortedSet<AStarNode>(new AStarComparer());
            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, float>();
            var fScore = new Dictionary<int, float>();
            var closedSet = new HashSet<int>();

            gScore[request.StartNodeId] = 0f;
            fScore[request.StartNodeId] = Heuristic(startNode, endNode);
            openSet.Add(new AStarNode(request.StartNodeId, fScore[request.StartNodeId]));

            while (openSet.Count > 0)
            {
                var current = openSet.Min;
                openSet.Remove(current);

                if (current.NodeId == request.EndNodeId)
                    return ReconstructPath(cameFrom, current.NodeId, gScore[current.NodeId]);

                closedSet.Add(current.NodeId);

                var edges = graph.GetEdgesFrom(current.NodeId);
                foreach (var edge in edges)
                {
                    if (!edge.IsPassable) continue;

                    int neighborId = edge.GetOtherNode(current.NodeId);
                    if (closedSet.Contains(neighborId)) continue;

                    var neighborNode = graph.GetNode(neighborId);
                    if (neighborNode == null) continue;

                    // Calculate edge cost with modifiers
                    float edgeCost = edge.TraversalCost;

                    // Danger avoidance
                    if (request.AvoidDanger)
                    {
                        float danger = Mathf.Max(edge.DangerLevel, neighborNode.BaseDangerLevel);
                        if (danger > request.MaxAcceptableDanger)
                            edgeCost += 100f; // Heavy penalty
                        else
                            edgeCost += danger * 2f;
                    }

                    // Prefer explored paths
                    if (request.PreferExplored && neighborNode.HasTag(NodeTag.Unexplored))
                        edgeCost += 0.5f;

                    // Prefer shortcuts
                    if (request.PreferShortcuts && neighborNode.HasTag(NodeTag.Shortcut))
                        edgeCost *= 0.5f;

                    // Apply strategy-provided bias
                    if (request.EdgeWeightModifier != null)
                        edgeCost *= request.EdgeWeightModifier(edge);

                    float tentativeG = gScore[current.NodeId] + edgeCost;

                    if (!gScore.ContainsKey(neighborId) || tentativeG < gScore[neighborId])
                    {
                        cameFrom[neighborId] = current.NodeId;
                        gScore[neighborId] = tentativeG;
                        fScore[neighborId] = tentativeG + Heuristic(neighborNode, endNode);

                        var neighborAStar = new AStarNode(neighborId, fScore[neighborId]);
                        openSet.Add(neighborAStar);
                    }
                }
            }

            return PathResult.Failed("No path found.");
        }

        private float Heuristic(DungeonNode a, DungeonNode b)
        {
            return Vector3.Distance(a.WorldPosition, b.WorldPosition);
        }

        private PathResult ReconstructPath(Dictionary<int, int> cameFrom, int currentId, float totalCost)
        {
            var path = new List<int> { currentId };
            while (cameFrom.ContainsKey(currentId))
            {
                currentId = cameFrom[currentId];
                path.Insert(0, currentId);
            }

            return new PathResult
            {
                Success = true,
                NodePath = path,
                TotalCost = totalCost
            };
        }

        private struct AStarNode
        {
            public int NodeId;
            public float FScore;

            public AStarNode(int nodeId, float fScore)
            {
                NodeId = nodeId;
                FScore = fScore;
            }
        }

        private class AStarComparer : IComparer<AStarNode>
        {
            public int Compare(AStarNode a, AStarNode b)
            {
                int result = a.FScore.CompareTo(b.FScore);
                return result != 0 ? result : a.NodeId.CompareTo(b.NodeId);
            }
        }
    }
}

