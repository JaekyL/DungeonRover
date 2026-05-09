using System.Collections.Generic;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using UnityEngine;

namespace DungeonGeneration.SpatialLayout
{
    /// <summary>
    /// Binary Space Partitioning layout algorithm.
    /// Recursively splits space into rooms with structured feel (fortress, dungeon).
    /// </summary>
    public class BSPLayoutAlgorithm : ISpatialLayoutAlgorithm
    {
        public string AlgorithmName => "BSP Partitioning";
        private class BSPNode
        {
            public RectInt Bounds;
            public BSPNode Left, Right;
            public RoomInstance Room;
        }
        public void Generate(SpatialMap map, DungeonGraph graph, DungeonConfig config, SeededRandom rng)
        {
            var root = new BSPNode { Bounds = new RectInt(1, 1, map.Width - 2, map.Height - 2) };
            SplitNode(root, config, rng, 0);
            var leaves = new List<BSPNode>();
            GetLeaves(root, leaves);
            // Map graph nodes to BSP leaves
            int nodeIdx = 0;
            foreach (var leaf in leaves)
            {
                if (nodeIdx >= graph.Nodes.Count) break;
                var padding = config.roomPadding;
                var roomBounds = new RectInt(
                    leaf.Bounds.x + padding,
                    leaf.Bounds.y + padding,
                    Mathf.Max(config.minRoomSize.x, leaf.Bounds.width - padding * 2),
                    Mathf.Max(config.minRoomSize.y, leaf.Bounds.height - padding * 2)
                );
                // Randomize room size within leaf
                int w = rng.Next(config.minRoomSize.x, Mathf.Min(config.maxRoomSize.x, roomBounds.width) + 1);
                int h = rng.Next(config.minRoomSize.y, Mathf.Min(config.maxRoomSize.y, roomBounds.height) + 1);
                int x = rng.Next(roomBounds.x, roomBounds.x + roomBounds.width - w + 1);
                int y = rng.Next(roomBounds.y, roomBounds.y + roomBounds.height - h + 1);
                var finalBounds = new RectInt(x, y, w, h);
                var room = new RoomInstance
                {
                    Id = nodeIdx,
                    GraphNodeId = graph.Nodes[nodeIdx].Id,
                    Bounds = finalBounds,
                    Purpose = graph.Nodes[nodeIdx].Purpose
                };
                // Carve room into map
                map.CarveRoom(finalBounds, room.Id);
                // Track floor and wall tiles
                for (int rx = finalBounds.xMin; rx < finalBounds.xMax; rx++)
                    for (int ry = finalBounds.yMin; ry < finalBounds.yMax; ry++)
                        room.FloorTiles.Add(new Vector2Int(rx, ry));
                graph.Nodes[nodeIdx].Bounds = finalBounds;
                leaf.Room = room;
                map.Rooms.Add(room);
                nodeIdx++;
            }
            // Connect rooms via corridors based on graph edges
            ConnectRooms(map, graph, rng, config);
        }
        private void SplitNode(BSPNode node, DungeonConfig config, SeededRandom rng, int depth)
        {
            if (depth > 6) return;
            int minSize = Mathf.Max(config.minRoomSize.x, config.minRoomSize.y) + config.roomPadding * 2 + 2;
            if (node.Bounds.width < minSize * 2 && node.Bounds.height < minSize * 2) return;
            bool splitH = rng.NextBool();
            if (node.Bounds.width > node.Bounds.height * 1.5f) splitH = false;
            if (node.Bounds.height > node.Bounds.width * 1.5f) splitH = true;
            if (splitH)
            {
                if (node.Bounds.height < minSize * 2) return;
                int split = rng.Next(minSize, node.Bounds.height - minSize + 1);
                node.Left = new BSPNode { Bounds = new RectInt(node.Bounds.x, node.Bounds.y, node.Bounds.width, split) };
                node.Right = new BSPNode { Bounds = new RectInt(node.Bounds.x, node.Bounds.y + split, node.Bounds.width, node.Bounds.height - split) };
            }
            else
            {
                if (node.Bounds.width < minSize * 2) return;
                int split = rng.Next(minSize, node.Bounds.width - minSize + 1);
                node.Left = new BSPNode { Bounds = new RectInt(node.Bounds.x, node.Bounds.y, split, node.Bounds.height) };
                node.Right = new BSPNode { Bounds = new RectInt(node.Bounds.x + split, node.Bounds.y, node.Bounds.width - split, node.Bounds.height) };
            }
            SplitNode(node.Left, config, rng, depth + 1);
            SplitNode(node.Right, config, rng, depth + 1);
        }
        private void GetLeaves(BSPNode node, List<BSPNode> leaves)
        {
            if (node == null) return;
            if (node.Left == null && node.Right == null) { leaves.Add(node); return; }
            GetLeaves(node.Left, leaves);
            GetLeaves(node.Right, leaves);
        }
        private void ConnectRooms(SpatialMap map, DungeonGraph graph, SeededRandom rng, DungeonConfig config)
        {
            int corridorId = 0;
            foreach (var edge in graph.Edges)
            {
                if (edge.FromNodeId >= map.Rooms.Count || edge.ToNodeId >= map.Rooms.Count) continue;
                var roomA = map.Rooms[edge.FromNodeId];
                var roomB = map.Rooms[edge.ToNodeId];
                var centerA = new Vector2Int(roomA.Bounds.x + roomA.Bounds.width / 2, roomA.Bounds.y + roomA.Bounds.height / 2);
                var centerB = new Vector2Int(roomB.Bounds.x + roomB.Bounds.width / 2, roomB.Bounds.y + roomB.Bounds.height / 2);
                var corridor = new CorridorInstance { Id = corridorId++, FromRoomId = roomA.Id, ToRoomId = roomB.Id, Width = config.corridorWidth };
                // L-shaped corridor
                bool hFirst = rng.NextBool();
                Vector2Int current = centerA;
                Vector2Int target = hFirst ? new Vector2Int(centerB.x, centerA.y) : new Vector2Int(centerA.x, centerB.y);
                CarveLine(map, current, target, config.corridorWidth, corridor);
                CarveLine(map, target, centerB, config.corridorWidth, corridor);
                // Place door at entry points
                if (edge.Type == EdgeType.Locked || edge.Type == EdgeType.Secret)
                {
                    var doorPos = corridor.Tiles.Count > 0 ? corridor.Tiles[corridor.Tiles.Count / 2] : centerA;
                    var door = new DoorInstance
                    {
                        Position = doorPos,
                        Type = edge.Type == EdgeType.Locked ? DoorType.Locked : DoorType.Secret,
                        RequiredKey = edge.RequiredKey,
                        ConnectsRoomA = roomA.Id,
                        ConnectsRoomB = roomB.Id
                    };
                    map.Doors.Add(door);
                    map.SetTile(doorPos.x, doorPos.y, edge.Type == EdgeType.Secret ? TileType.SecretDoor : TileType.Door);
                }
                map.Corridors.Add(corridor);
            }
        }
        private void CarveLine(SpatialMap map, Vector2Int from, Vector2Int to, int width, CorridorInstance corridor)
        {
            int dx = to.x > from.x ? 1 : (to.x < from.x ? -1 : 0);
            int dy = to.y > from.y ? 1 : (to.y < from.y ? -1 : 0);
            var pos = from;
            int halfW = width / 2;
            while (pos != to)
            {
                for (int w = -halfW; w <= halfW; w++)
                {
                    var tilePos = dy != 0 ? new Vector2Int(pos.x + w, pos.y) : new Vector2Int(pos.x, pos.y + w);
                    if (map.InBounds(tilePos) && map.GetTile(tilePos).Type == TileType.Wall)
                    {
                        map.SetTile(tilePos.x, tilePos.y, TileType.Corridor);
                        map.GetTile(tilePos).CorridorId = corridor.Id;
                        corridor.Tiles.Add(tilePos);
                    }
                }
                if (pos.x != to.x) pos.x += dx;
                else if (pos.y != to.y) pos.y += dy;
                else break;
            }
        }
    }
}
