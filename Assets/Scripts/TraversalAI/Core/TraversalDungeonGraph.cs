using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Data;
using UnityEngine;

namespace TraversalAI.Core
{
    /// <summary>
    /// AI-facing dungeon graph abstraction. Bridges the generation system's DungeonGraph
    /// to the traversal AI's representation. Supports incremental discovery — nodes/edges
    /// can be revealed as the AI explores.
    /// </summary>
    [System.Serializable]
    public class TraversalDungeonGraph
    {
        [SerializeField] private List<DungeonNode> _nodes = new List<DungeonNode>();
        [SerializeField] private List<DungeonEdge> _edges = new List<DungeonEdge>();

        private Dictionary<int, DungeonNode> _nodeIndex = new Dictionary<int, DungeonNode>();
        private Dictionary<int, DungeonEdge> _edgeIndex = new Dictionary<int, DungeonEdge>();
        private Dictionary<int, List<int>> _adjacency = new Dictionary<int, List<int>>();

        /// <summary>
        /// Reference to the spatial map used for tile-level pathfinding.
        /// Null when in standalone/test mode (no generated dungeon geometry).
        /// </summary>
        private SpatialMap _spatialMap;

        public IReadOnlyList<DungeonNode> Nodes => _nodes;
        public IReadOnlyList<DungeonEdge> Edges => _edges;

        /// <summary>The SpatialMap from dungeon generation, if available.</summary>
        public SpatialMap SpatialMap => _spatialMap;

        /// <summary>
        /// Build the traversal graph from the dungeon generation data.
        /// All nodes start as unexplored.
        /// When a SpatialMap is provided, uses room center positions for accurate world placement.
        /// </summary>
        public void BuildFromGenerationData(DungeonGraph sourceGraph, SpatialMap spatialMap)
        {
            _nodes.Clear();
            _edges.Clear();
            _nodeIndex.Clear();
            _edgeIndex.Clear();
            _adjacency.Clear();

            // Store spatial map reference for tile-level pathfinding
            _spatialMap = spatialMap;

            // Build a lookup from graph node ID → room instance for accurate positioning
            Dictionary<int, RoomInstance> roomLookup = null;
            if (spatialMap != null && spatialMap.Rooms.Count > 0)
            {
                roomLookup = new Dictionary<int, RoomInstance>();
                foreach (var room in spatialMap.Rooms)
                    roomLookup[room.GraphNodeId] = room;
            }

            // Convert GraphNodes to DungeonNodes
            foreach (var srcNode in sourceGraph.Nodes)
            {
                // Use SpatialMap room bounds if available, otherwise fall back to graph node bounds
                RectInt bounds = srcNode.Bounds;
                if (roomLookup != null && roomLookup.TryGetValue(srcNode.Id, out var room))
                    bounds = room.Bounds;

                var node = new DungeonNode
                {
                    Id = srcNode.Id,
                    GridBounds = bounds,
                    WorldPosition = new Vector3(
                        bounds.center.x,
                        0f,
                        bounds.center.y
                    ),
                    Tags = NodeTag.Unexplored,
                    Label = $"{srcNode.RoomType}_{srcNode.Id}"
                };

                // Map generation data to AI tags
                switch (srcNode.RoomType)
                {
                    case RoomType.Boss:
                    case RoomType.MiniBoss:
                        node.AddTag(NodeTag.Dangerous);
                        node.AddTag(NodeTag.BossArea);
                        node.BaseDangerLevel = srcNode.RoomType == RoomType.Boss ? 0.9f : 0.6f;
                        break;
                    case RoomType.Treasure:
                        node.AddTag(NodeTag.Loot);
                        node.BaseLootValue = 0.8f;
                        break;
                    case RoomType.Hub:
                        node.AddTag(NodeTag.HubRoom);
                        node.AddTag(NodeTag.SafeZone);
                        break;
                    case RoomType.DeadEnd:
                        node.AddTag(NodeTag.DeadEnd);
                        break;
                    case RoomType.Secret:
                        node.AddTag(NodeTag.Hidden);
                        break;
                    case RoomType.Transition:
                        node.AddTag(NodeTag.Staircase);
                        break;
                    case RoomType.Start:
                        node.AddTag(NodeTag.SafeZone);
                        break;
                }

                node.BaseDangerLevel = Mathf.Max(node.BaseDangerLevel, srcNode.DifficultyTier / 10f);

                foreach (var kvp in srcNode.Metadata)
                    node.Metadata[kvp.Key] = kvp.Value;

                AddNode(node);
            }

            // Convert GraphEdges to DungeonEdges
            for (int i = 0; i < sourceGraph.Edges.Count; i++)
            {
                var srcEdge = sourceGraph.Edges[i];
                var edge = new DungeonEdge
                {
                    Id = i,
                    FromNodeId = srcEdge.FromNodeId,
                    ToNodeId = srcEdge.ToNodeId,
                    IsOneWay = srcEdge.Type == EdgeType.OneWay,
                    IsSecret = srcEdge.IsSecret,
                    IsVertical = srcEdge.Type == EdgeType.Vertical,
                    RequiresKey = !string.IsNullOrEmpty(srcEdge.RequiredKey),
                    RequiredKeyId = srcEdge.RequiredKey,
                    TraversalCost = srcEdge.Type == EdgeType.Shortcut ? 0.5f : 1f
                };

                if (srcEdge.Type == EdgeType.Locked)
                {
                    edge.IsPassable = false;
                    edge.RequiresKey = true;
                }

                AddEdge(edge);
            }
        }

        public void AddNode(DungeonNode node)
        {
            _nodes.Add(node);
            _nodeIndex[node.Id] = node;
            if (!_adjacency.ContainsKey(node.Id))
                _adjacency[node.Id] = new List<int>();
        }

        public void AddEdge(DungeonEdge edge)
        {
            _edges.Add(edge);
            _edgeIndex[edge.Id] = edge;

            if (!_adjacency.ContainsKey(edge.FromNodeId))
                _adjacency[edge.FromNodeId] = new List<int>();
            _adjacency[edge.FromNodeId].Add(edge.ToNodeId);

            if (!edge.IsOneWay)
            {
                if (!_adjacency.ContainsKey(edge.ToNodeId))
                    _adjacency[edge.ToNodeId] = new List<int>();
                _adjacency[edge.ToNodeId].Add(edge.FromNodeId);
            }

            // Cache edge IDs on nodes
            var fromNode = GetNode(edge.FromNodeId);
            if (fromNode != null) fromNode.ConnectedEdgeIds.Add(edge.Id);
            var toNode = GetNode(edge.ToNodeId);
            if (toNode != null && !edge.IsOneWay) toNode.ConnectedEdgeIds.Add(edge.Id);
        }

        public DungeonNode GetNode(int id) => _nodeIndex.TryGetValue(id, out var n) ? n : null;
        public DungeonEdge GetEdge(int id) => _edgeIndex.TryGetValue(id, out var e) ? e : null;

        public List<DungeonNode> GetNeighbors(int nodeId)
        {
            if (!_adjacency.TryGetValue(nodeId, out var neighborIds))
                return new List<DungeonNode>();
            return neighborIds.Select(id => GetNode(id)).Where(n => n != null).ToList();
        }

        public List<DungeonEdge> GetEdgesFrom(int nodeId)
        {
            return _edges.Where(e =>
                e.FromNodeId == nodeId || (!e.IsOneWay && e.ToNodeId == nodeId)
            ).ToList();
        }

        public DungeonEdge GetEdgeBetween(int fromId, int toId)
        {
            return _edges.FirstOrDefault(e =>
                (e.FromNodeId == fromId && e.ToNodeId == toId) ||
                (!e.IsOneWay && e.FromNodeId == toId && e.ToNodeId == fromId)
            );
        }

        public List<DungeonNode> GetNodesWithTag(NodeTag tag)
        {
            return _nodes.Where(n => n.HasTag(tag)).ToList();
        }

        public List<DungeonNode> GetNodesOnFloor(int floor)
        {
            return _nodes.Where(n => n.FloorIndex == floor).ToList();
        }

        /// <summary>Rebuild internal indices after deserialization.</summary>
        public void RebuildIndices()
        {
            _nodeIndex.Clear();
            _edgeIndex.Clear();
            _adjacency.Clear();

            foreach (var node in _nodes)
                _nodeIndex[node.Id] = node;

            foreach (var edge in _edges)
            {
                _edgeIndex[edge.Id] = edge;

                if (!_adjacency.ContainsKey(edge.FromNodeId))
                    _adjacency[edge.FromNodeId] = new List<int>();
                _adjacency[edge.FromNodeId].Add(edge.ToNodeId);

                if (!edge.IsOneWay)
                {
                    if (!_adjacency.ContainsKey(edge.ToNodeId))
                        _adjacency[edge.ToNodeId] = new List<int>();
                    _adjacency[edge.ToNodeId].Add(edge.FromNodeId);
                }
            }
        }
    }
}

