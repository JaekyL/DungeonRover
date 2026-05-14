using System.Collections.Generic;
using UnityEngine;

namespace TraversalAI.Pathfinding
{
    /// <summary>
    /// Pathfinding request data. Decouples traversal logic from pathfinding implementation.
    /// </summary>
    [System.Serializable]
    public class PathRequest
    {
        public int StartNodeId;
        public int EndNodeId;
        public bool AvoidDanger = true;
        public bool PreferExplored;
        public bool PreferShortcuts = true;
        public float MaxAcceptableDanger = 0.7f;

        /// <summary>Optional: strategy-provided edge weight biases.</summary>
        public System.Func<Core.DungeonEdge, float> EdgeWeightModifier;
    }

    /// <summary>
    /// Result of a pathfinding request.
    /// </summary>
    [System.Serializable]
    public class PathResult
    {
        public bool Success;
        public List<int> NodePath = new List<int>();
        public float TotalCost;
        public string FailureReason;

        public int NextNodeId => NodePath.Count > 1 ? NodePath[1] : -1;

        public static PathResult Failed(string reason) => new PathResult
        {
            Success = false,
            FailureReason = reason
        };
    }

    /// <summary>
    /// Abstract pathfinding interface. Supports graph, grid, and NavMesh implementations.
    /// </summary>
    public interface IPathfinder
    {
        PathResult FindPath(PathRequest request, Core.TraversalDungeonGraph graph);
    }
}

