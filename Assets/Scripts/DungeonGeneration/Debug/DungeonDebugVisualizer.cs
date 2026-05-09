using System.Collections.Generic;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using UnityEngine;

namespace DungeonGeneration.Debug
{
    /// <summary>
    /// Editor Gizmo visualizer for the dungeon generation data.
    /// Draws rooms, corridors, graph, story markers, and encounters.
    /// </summary>
    [RequireComponent(typeof(DungeonGenerator))]
    public class DungeonDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization Layers")]
        [SerializeField] private bool _showTileMap = true;
        [SerializeField] private bool _showGraph = true;
        [SerializeField] private bool _showRoomPurpose = true;
        [SerializeField] private bool _showFactions = true;
        [SerializeField] private bool _showStoryMarkers = true;
        [SerializeField] private bool _showEncounters = true;
        [SerializeField] private bool _showDoors = true;
        [SerializeField] private bool _showHeatmap = false;
        [Header("Settings")]
        [SerializeField] private float _tileSize = 1f;
        [SerializeField] private float _graphHeightOffset = 5f;
        private DungeonGenerator _generator;
        private static readonly Dictionary<TileType, Color> TileColors = new Dictionary<TileType, Color>
        {
            { TileType.Wall, new Color(0.2f, 0.2f, 0.2f, 0.5f) },
            { TileType.Floor, new Color(0.6f, 0.6f, 0.5f, 0.3f) },
            { TileType.Corridor, new Color(0.5f, 0.5f, 0.4f, 0.3f) },
            { TileType.Door, new Color(0.8f, 0.5f, 0.2f, 0.8f) },
            { TileType.SecretDoor, new Color(0.5f, 0.2f, 0.8f, 0.8f) },
            { TileType.Water, new Color(0.2f, 0.4f, 0.9f, 0.5f) },
            { TileType.Rubble, new Color(0.5f, 0.3f, 0.2f, 0.5f) },
            { TileType.StairsUp, Color.cyan },
            { TileType.StairsDown, Color.blue },
            { TileType.Pit, Color.black },
        };
        private static readonly Dictionary<RoomType, Color> RoomTypeColors = new Dictionary<RoomType, Color>
        {
            { RoomType.Start, Color.green },
            { RoomType.Boss, Color.red },
            { RoomType.Treasure, Color.yellow },
            { RoomType.Hub, Color.cyan },
            { RoomType.Secret, new Color(0.8f, 0.2f, 1f) },
            { RoomType.DeadEnd, new Color(0.5f, 0.5f, 0.5f) },
            { RoomType.Normal, Color.white },
            { RoomType.Transition, new Color(0.7f, 0.7f, 0.3f) },
            { RoomType.MiniBoss, new Color(1f, 0.5f, 0f) },
        };
        private void OnDrawGizmos()
        {
            _generator = GetComponent<DungeonGenerator>();
            if (_generator == null || !_generator.ShowDebug) return;
            var ctx = _generator.LastContext;
            if (ctx == null) return;
            if (_showTileMap && ctx.SpatialMap != null) DrawTileMap(ctx);
            if (_showGraph && ctx.Graph != null) DrawGraph(ctx);
            if (_showRoomPurpose && ctx.SpatialMap != null) DrawRoomPurposes(ctx);
            if (_showStoryMarkers) DrawStoryMarkers(ctx);
            if (_showEncounters) DrawEncounters(ctx);
            if (_showDoors && ctx.SpatialMap != null) DrawDoors(ctx);
        }
        private void DrawTileMap(GenerationContext ctx)
        {
            var map = ctx.SpatialMap;
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(x, y);
                    if (tile.Type == TileType.Wall) continue; // Skip walls for performance
                    if (TileColors.TryGetValue(tile.Type, out var color))
                    {
                        Gizmos.color = color;
                        Gizmos.DrawCube(
                            new Vector3(x * _tileSize, 0, y * _tileSize),
                            new Vector3(_tileSize * 0.9f, 0.1f, _tileSize * 0.9f));
                    }
                }
            }
        }
        private void DrawGraph(GenerationContext ctx)
        {
            var graph = ctx.Graph;
            Vector3 offset = Vector3.up * _graphHeightOffset;
            // Draw nodes
            foreach (var node in graph.Nodes)
            {
                var center = new Vector3(node.Bounds.center.x * _tileSize, 0, node.Bounds.center.y * _tileSize) + offset;
                float radius = node.IsCriticalPath ? 1.5f : 1f;
                Gizmos.color = RoomTypeColors.TryGetValue(node.RoomType, out var c) ? c : Color.white;
                Gizmos.DrawWireSphere(center, radius);
                // Draw importance as filled sphere
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawSphere(center, radius * 0.5f);
            }
            // Draw edges
            foreach (var edge in graph.Edges)
            {
                var from = graph.GetNode(edge.FromNodeId);
                var to = graph.GetNode(edge.ToNodeId);
                if (from == null || to == null) continue;
                var fromPos = new Vector3(from.Bounds.center.x * _tileSize, 0, from.Bounds.center.y * _tileSize) + offset;
                var toPos = new Vector3(to.Bounds.center.x * _tileSize, 0, to.Bounds.center.y * _tileSize) + offset;
                switch (edge.Type)
                {
                    case EdgeType.Locked: Gizmos.color = Color.red; break;
                    case EdgeType.Secret: Gizmos.color = new Color(0.8f, 0.2f, 1f); break;
                    case EdgeType.Shortcut: Gizmos.color = Color.cyan; break;
                    default: Gizmos.color = Color.white; break;
                }
                Gizmos.DrawLine(fromPos, toPos);
            }
        }
        private void DrawRoomPurposes(GenerationContext ctx)
        {
#if UNITY_EDITOR
            foreach (var room in ctx.SpatialMap.Rooms)
            {
                var center = new Vector3(
                    (room.Bounds.x + room.Bounds.width * 0.5f) * _tileSize,
                    2f,
                    (room.Bounds.y + room.Bounds.height * 0.5f) * _tileSize);
                string label = $"R{room.Id}: {room.Purpose}";
                if (!string.IsNullOrEmpty(room.FactionOwner))
                    label += $"\n[{room.FactionOwner}]";
                UnityEditor.Handles.Label(center, label);
            }
#endif
        }
        private void DrawStoryMarkers(GenerationContext ctx)
        {
            foreach (var marker in ctx.StoryMarkers)
            {
                var pos = new Vector3(marker.Position.x * _tileSize, 0.5f, marker.Position.y * _tileSize);
                switch (marker.Type)
                {
                    case StoryMarkerType.Skeleton: Gizmos.color = Color.white; break;
                    case StoryMarkerType.BloodTrail: Gizmos.color = Color.red; break;
                    case StoryMarkerType.BurnMarks: Gizmos.color = new Color(1f, 0.5f, 0f); break;
                    case StoryMarkerType.RitualMarking: Gizmos.color = new Color(0.5f, 0f, 0.5f); break;
                    case StoryMarkerType.WaterDamage: Gizmos.color = new Color(0.3f, 0.5f, 1f); break;
                    case StoryMarkerType.FungalGrowth: Gizmos.color = new Color(0.2f, 0.7f, 0.2f); break;
                    default: Gizmos.color = Color.gray; break;
                }
                Gizmos.DrawWireCube(pos, Vector3.one * 0.3f * marker.Intensity);
            }
        }
        private void DrawEncounters(GenerationContext ctx)
        {
            foreach (var encounter in ctx.Encounters)
            {
                foreach (var spawn in encounter.SpawnPoints)
                {
                    var pos = new Vector3(spawn.Position.x * _tileSize, 1f, spawn.Position.y * _tileSize);
                    switch (encounter.Type)
                    {
                        case EncounterType.Boss: Gizmos.color = Color.red; break;
                        case EncounterType.Ambush: Gizmos.color = Color.yellow; break;
                        case EncounterType.Nest: Gizmos.color = new Color(0.5f, 0.3f, 0f); break;
                        default: Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f); break;
                    }
                    Gizmos.DrawWireSphere(pos, 0.3f);
                }
            }
        }
        private void DrawDoors(GenerationContext ctx)
        {
            foreach (var door in ctx.SpatialMap.Doors)
            {
                var pos = new Vector3(door.Position.x * _tileSize, 0.5f, door.Position.y * _tileSize);
                switch (door.Type)
                {
                    case DoorType.Locked: Gizmos.color = Color.red; break;
                    case DoorType.Secret: Gizmos.color = new Color(0.8f, 0.2f, 1f); break;
                    case DoorType.Boss: Gizmos.color = new Color(1f, 0f, 0f); break;
                    default: Gizmos.color = new Color(0.8f, 0.5f, 0.2f); break;
                }
                Gizmos.DrawCube(pos, new Vector3(0.8f, 1.5f, 0.2f));
            }
        }
    }
}
