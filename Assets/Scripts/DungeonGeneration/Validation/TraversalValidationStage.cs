using System.Collections.Generic;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using UnityEngine;

namespace DungeonGeneration.Validation
{
    /// <summary>
    /// Stage 5: Validates dungeon traversal and connectivity.
    /// Ensures all rooms are reachable and critical path is valid.
    /// </summary>
    public class TraversalValidationStage : IGenerationStage
    {
        public string StageName => "Traversal Validation";
        public int Priority => 500;
        public void Execute(GenerationContext context)
        {
            var graph = context.Graph;
            if (graph.Nodes.Count == 0) return;
            // BFS connectivity check
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(0);
            visited.Add(0);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (var neighbor in graph.GetNeighbors(current))
                {
                    if (!visited.Contains(neighbor.Id))
                    {
                        visited.Add(neighbor.Id);
                        queue.Enqueue(neighbor.Id);
                    }
                }
            }
            // Check for disconnected nodes and fix
            foreach (var node in graph.Nodes)
            {
                if (!visited.Contains(node.Id))
                {
                    UnityEngine.Debug.LogWarning($"[DungeonGen] Room {node.Id} is disconnected! Adding emergency corridor.");
                    // Connect to nearest visited node
                    int nearestId = FindNearest(node.Id, visited, graph);
                    if (nearestId >= 0)
                    {
                        graph.AddEdge(node.Id, nearestId, EdgeType.Shortcut);
                        visited.Add(node.Id);
                        // Also carve corridor in spatial map if available
                        RepairSpatialConnection(node.Id, nearestId, context);
                    }
                }
            }
            // Validate critical path
            ValidateCriticalPath(graph);
            // Store traversal heatmap
            ComputeTraversalHeatmap(context);
        }
        private int FindNearest(int nodeId, HashSet<int> candidates, DungeonGraph graph)
        {
            var node = graph.GetNode(nodeId);
            if (node == null) return -1;
            int best = -1;
            float bestDist = float.MaxValue;
            var nodeCenter = new Vector2(node.Bounds.center.x, node.Bounds.center.y);
            foreach (int cid in candidates)
            {
                var candidate = graph.GetNode(cid);
                if (candidate == null) continue;
                var candidateCenter = new Vector2(candidate.Bounds.center.x, candidate.Bounds.center.y);
                float dist = Vector2.Distance(nodeCenter, candidateCenter);
                if (dist < bestDist) { bestDist = dist; best = cid; }
            }
            return best;
        }
        private void RepairSpatialConnection(int fromId, int toId, GenerationContext context)
        {
            var map = context.SpatialMap;
            if (map == null || fromId >= map.Rooms.Count || toId >= map.Rooms.Count) return;
            var roomA = map.Rooms[fromId];
            var roomB = map.Rooms[toId];
            var centerA = new Vector2Int(roomA.Bounds.x + roomA.Bounds.width / 2, roomA.Bounds.y + roomA.Bounds.height / 2);
            var centerB = new Vector2Int(roomB.Bounds.x + roomB.Bounds.width / 2, roomB.Bounds.y + roomB.Bounds.height / 2);
            // Simple L-corridor
            var pos = centerA;
            while (pos.x != centerB.x)
            {
                if (map.InBounds(pos) && map.GetTile(pos).Type == TileType.Wall)
                    map.SetTile(pos.x, pos.y, TileType.Corridor);
                pos.x += pos.x < centerB.x ? 1 : -1;
            }
            while (pos.y != centerB.y)
            {
                if (map.InBounds(pos) && map.GetTile(pos).Type == TileType.Wall)
                    map.SetTile(pos.x, pos.y, TileType.Corridor);
                pos.y += pos.y < centerB.y ? 1 : -1;
            }
        }
        private void ValidateCriticalPath(DungeonGraph graph)
        {
            var criticalNodes = graph.Nodes.FindAll(n => n.IsCriticalPath);
            for (int i = 0; i < criticalNodes.Count - 1; i++)
            {
                bool connected = graph.Edges.Exists(e =>
                    (e.FromNodeId == criticalNodes[i].Id && e.ToNodeId == criticalNodes[i + 1].Id) ||
                    (e.ToNodeId == criticalNodes[i].Id && e.FromNodeId == criticalNodes[i + 1].Id));
                if (!connected)
                {
                    UnityEngine.Debug.LogWarning($"[DungeonGen] Critical path broken between {criticalNodes[i].Id} and {criticalNodes[i + 1].Id}");
                    graph.AddEdge(criticalNodes[i].Id, criticalNodes[i + 1].Id);
                }
            }
        }
        private void ComputeTraversalHeatmap(GenerationContext context)
        {
            var heatmap = new Dictionary<int, int>();
            var graph = context.Graph;
            // BFS from start, track visit counts
            var queue = new Queue<int>();
            queue.Enqueue(0);
            heatmap[0] = 1;
            var visited = new HashSet<int> { 0 };
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (var neighbor in graph.GetNeighbors(current))
                {
                    if (!heatmap.ContainsKey(neighbor.Id))
                        heatmap[neighbor.Id] = 0;
                    heatmap[neighbor.Id]++;
                    if (!visited.Contains(neighbor.Id))
                    {
                        visited.Add(neighbor.Id);
                        queue.Enqueue(neighbor.Id);
                    }
                }
            }
            context.SetCustomData("traversal_heatmap", heatmap);
        }
    }
}
