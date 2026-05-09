using System.Collections.Generic;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using UnityEngine;

namespace DungeonGeneration.MacroGraph
{
    /// <summary>
    /// Stage 1: Generates the abstract dungeon progression graph.
    /// Creates critical path, branches, loops, dead ends, hubs, secrets, lock/key pairs.
    /// </summary>
    public class MacroGraphStage : IGenerationStage
    {
        public string StageName => "Macro Graph Generation";
        public int Priority => 100;
        public void Execute(GenerationContext context)
        {
            var config = context.Config;
            var rng = context.Random.Fork("macro");
            var graph = new DungeonGraph();
            // 1. Build critical path
            int pathLen = config.criticalPathLength;
            var criticalPath = BuildCriticalPath(graph, pathLen, rng, config);
            // 2. Add branches
            AddBranches(graph, criticalPath, rng, config);
            // 3. Add loops
            AddLoops(graph, rng, config);
            // 4. Add secret rooms
            AddSecretRooms(graph, rng, config);
            // 5. Place lock/key pairs along critical path
            PlaceLockKeyPairs(graph, criticalPath, rng, config);
            // 6. Assign difficulty tiers
            AssignDifficultyTiers(graph, criticalPath);
            context.Graph = graph;
        }
        private List<int> BuildCriticalPath(DungeonGraph graph, int length, SeededRandom rng, DungeonConfig config)
        {
            var path = new List<int>();
            // Start node
            var start = graph.AddNode(RoomType.Start);
            start.IsCriticalPath = true;
            start.Importance = 10;
            path.Add(start.Id);
            // Middle nodes
            for (int i = 1; i < length - 1; i++)
            {
                bool isHub = i == length / 2 && length > 4;
                var node = graph.AddNode(isHub ? RoomType.Hub : RoomType.Normal);
                node.IsCriticalPath = true;
                node.Importance = isHub ? 8 : 5;
                graph.AddEdge(path[path.Count - 1], node.Id);
                path.Add(node.Id);
            }
            // Boss node
            if (config.placeBoss && length > 1)
            {
                var boss = graph.AddNode(RoomType.Boss);
                boss.IsCriticalPath = true;
                boss.Importance = 10;
                graph.AddEdge(path[path.Count - 1], boss.Id);
                path.Add(boss.Id);
            }
            else
            {
                var end = graph.AddNode(RoomType.Normal);
                end.IsCriticalPath = true;
                end.Importance = 7;
                graph.AddEdge(path[path.Count - 1], end.Id);
                path.Add(end.Id);
            }
            return path;
        }
        private void AddBranches(DungeonGraph graph, List<int> criticalPath, SeededRandom rng, DungeonConfig config)
        {
            int maxBranches = config.maxRooms - graph.Nodes.Count;
            for (int i = 0; i < maxBranches; i++)
            {
                if (!rng.NextBool(config.branchProbability)) continue;
                if (graph.Nodes.Count >= config.maxRooms) break;
                int parentId = rng.Choose(criticalPath);
                int branchLen = rng.Next(1, 4);
                int prevId = parentId;
                for (int b = 0; b < branchLen && graph.Nodes.Count < config.maxRooms; b++)
                {
                    bool isEnd = b == branchLen - 1;
                    var type = isEnd ? RoomType.DeadEnd : RoomType.Normal;
                    if (isEnd && rng.NextBool(0.3f)) type = RoomType.Treasure;
                    var node = graph.AddNode(type);
                    node.Importance = 3;
                    graph.AddEdge(prevId, node.Id);
                    prevId = node.Id;
                }
            }
        }
        private void AddLoops(DungeonGraph graph, SeededRandom rng, DungeonConfig config)
        {
            var nodes = graph.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (!rng.NextBool(config.loopProbability)) continue;
                int targetId;
                int attempts = 0;
                do
                {
                    targetId = rng.Next(nodes.Count);
                    attempts++;
                } while ((targetId == i || graph.GetEdgesFor(i).Exists(e =>
                    e.FromNodeId == targetId || e.ToNodeId == targetId)) && attempts < 20);
                if (attempts < 20)
                {
                    graph.AddEdge(i, targetId, EdgeType.Shortcut);
                }
            }
        }
        private void AddSecretRooms(DungeonGraph graph, SeededRandom rng, DungeonConfig config)
        {
            int maxSecrets = Mathf.Max(1, Mathf.RoundToInt(graph.Nodes.Count * config.secretRoomProbability));
            for (int i = 0; i < maxSecrets && graph.Nodes.Count < config.maxRooms; i++)
            {
                int parentId = rng.Next(graph.Nodes.Count);
                var secret = graph.AddNode(RoomType.Secret);
                secret.IsSecret = true;
                secret.Importance = 6;
                secret.NarrativeTags.Add("hidden");
                var edge = graph.AddEdge(parentId, secret.Id, EdgeType.Secret);
                edge.IsSecret = true;
            }
        }
        private void PlaceLockKeyPairs(DungeonGraph graph, List<int> criticalPath, SeededRandom rng, DungeonConfig config)
        {
            int pairs = Mathf.Min(config.lockKeyPairs, criticalPath.Count - 2);
            for (int i = 0; i < pairs; i++)
            {
                string keyName = $"key_{(char)('A' + i)}";
                // Lock a door on the critical path (not the first or last edge)
                int lockIndex = rng.Next(1, criticalPath.Count - 1);
                var edges = graph.GetEdgesFor(criticalPath[lockIndex]);
                foreach (var edge in edges)
                {
                    int otherId = edge.FromNodeId == criticalPath[lockIndex] ? edge.ToNodeId : edge.FromNodeId;
                    if (criticalPath.Contains(otherId) && criticalPath.IndexOf(otherId) == lockIndex - 1)
                    {
                        edge.Type = EdgeType.Locked;
                        edge.RequiredKey = keyName;
                        // Place key in a branch before the lock
                        var keyNode = graph.GetNode(criticalPath[Mathf.Max(0, lockIndex - 2)]);
                        keyNode.Metadata["has_key"] = keyName;
                        keyNode.NarrativeTags.Add("key_location");
                        break;
                    }
                }
            }
        }
        private void AssignDifficultyTiers(DungeonGraph graph, List<int> criticalPath)
        {
            // BFS from start to assign difficulty based on distance
            var visited = new HashSet<int>();
            var queue = new Queue<(int id, int depth)>();
            queue.Enqueue((criticalPath[0], 0));
            visited.Add(criticalPath[0]);
            while (queue.Count > 0)
            {
                var (id, depth) = queue.Dequeue();
                var node = graph.GetNode(id);
                node.DifficultyTier = depth;
                foreach (var neighbor in graph.GetNeighbors(id))
                {
                    if (!visited.Contains(neighbor.Id))
                    {
                        visited.Add(neighbor.Id);
                        queue.Enqueue((neighbor.Id, depth + 1));
                    }
                }
            }
        }
    }
}
