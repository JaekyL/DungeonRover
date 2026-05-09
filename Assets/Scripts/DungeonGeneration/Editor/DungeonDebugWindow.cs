#if UNITY_EDITOR
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using UnityEditor;
using UnityEngine;

namespace DungeonGeneration.Editor
{
    public class DungeonDebugWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private int _selectedTab;
        private readonly string[] _tabs = { "Overview", "Rooms", "Graph", "History", "Story", "Encounters" };
        [MenuItem("Window/Dungeon Generation/Debug Window")]
        public static void ShowWindow()
        {
            GetWindow<DungeonDebugWindow>("Dungeon Debug");
        }
        private GenerationContext GetContext()
        {
            var gen = FindObjectOfType<DungeonGenerator>();
            return gen?.LastContext;
        }
        private void OnGUI()
        {
            var ctx = GetContext();
            if (ctx == null)
            {
                EditorGUILayout.HelpBox("No dungeon generated yet. Use the DungeonGenerator component to generate.", MessageType.Info);
                return;
            }
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            switch (_selectedTab)
            {
                case 0: DrawOverview(ctx); break;
                case 1: DrawRooms(ctx); break;
                case 2: DrawGraph(ctx); break;
                case 3: DrawHistory(ctx); break;
                case 4: DrawStory(ctx); break;
                case 5: DrawEncounters(ctx); break;
            }
            EditorGUILayout.EndScrollView();
        }
        private void DrawOverview(GenerationContext ctx)
        {
            EditorGUILayout.LabelField("Dungeon Overview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Seed: {ctx.Random.Seed}");
            EditorGUILayout.LabelField($"Config: {ctx.Config.dungeonName}");
            EditorGUILayout.LabelField($"Size: {ctx.Config.dungeonSize}");
            if (ctx.SpatialMap != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Spatial Data", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Rooms: {ctx.SpatialMap.Rooms.Count}");
                EditorGUILayout.LabelField($"Corridors: {ctx.SpatialMap.Corridors.Count}");
                EditorGUILayout.LabelField($"Doors: {ctx.SpatialMap.Doors.Count}");
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Story Markers: {ctx.StoryMarkers.Count}");
            EditorGUILayout.LabelField($"Decorations: {ctx.Decorations.Count}");
            EditorGUILayout.LabelField($"Encounters: {ctx.Encounters.Count}");
        }
        private void DrawRooms(GenerationContext ctx)
        {
            if (ctx.SpatialMap == null) return;
            EditorGUILayout.LabelField("Room Details", EditorStyles.boldLabel);
            foreach (var room in ctx.SpatialMap.Rooms)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Room {room.Id}: {room.Purpose}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  Bounds: {room.Bounds}");
                EditorGUILayout.LabelField($"  Floor Tiles: {room.FloorTiles.Count}");
                EditorGUILayout.LabelField($"  Faction: {room.FactionOwner ?? "none"}");
                EditorGUILayout.LabelField($"  Entry Points: {room.EntryPoints.Count}");
                EditorGUILayout.EndVertical();
            }
        }
        private void DrawGraph(GenerationContext ctx)
        {
            if (ctx.Graph == null) return;
            EditorGUILayout.LabelField($"Graph: {ctx.Graph.Nodes.Count} nodes, {ctx.Graph.Edges.Count} edges", EditorStyles.boldLabel);
            foreach (var node in ctx.Graph.Nodes)
            {
                string tags = node.NarrativeTags.Count > 0 ? string.Join(", ", node.NarrativeTags) : "none";
                EditorGUILayout.LabelField($"  [{node.Id}] {node.RoomType} | Diff={node.DifficultyTier} | CP={node.IsCriticalPath} | Tags={tags}");
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Edges", EditorStyles.boldLabel);
            foreach (var edge in ctx.Graph.Edges)
            {
                string extra = edge.Type != EdgeType.Normal ? $" ({edge.Type})" : "";
                if (!string.IsNullOrEmpty(edge.RequiredKey)) extra += $" [Key: {edge.RequiredKey}]";
                EditorGUILayout.LabelField($"  {edge.FromNodeId} -> {edge.ToNodeId}{extra}");
            }
        }
        private void DrawHistory(GenerationContext ctx)
        {
            if (ctx.HistoryLog == null) { EditorGUILayout.LabelField("No history data"); return; }
            EditorGUILayout.LabelField($"History Log: {ctx.HistoryLog.Events.Count} events", EditorStyles.boldLabel);
            var grouped = ctx.HistoryLog.Events.GroupBy(e => e.AgentType);
            foreach (var group in grouped)
            {
                EditorGUILayout.LabelField($"  {group.Key}: {group.Count()} events", EditorStyles.boldLabel);
                foreach (var evt in group.Take(10))
                {
                    EditorGUILayout.LabelField($"    Step {evt.Step}: {evt.Description}");
                }
                if (group.Count() > 10)
                    EditorGUILayout.LabelField($"    ... and {group.Count() - 10} more");
            }
        }
        private void DrawStory(GenerationContext ctx)
        {
            EditorGUILayout.LabelField($"Story Markers: {ctx.StoryMarkers.Count}", EditorStyles.boldLabel);
            var grouped = ctx.StoryMarkers.GroupBy(m => m.Type);
            foreach (var group in grouped)
            {
                EditorGUILayout.LabelField($"  {group.Key}: {group.Count()}");
            }
        }
        private void DrawEncounters(GenerationContext ctx)
        {
            EditorGUILayout.LabelField($"Encounters: {ctx.Encounters.Count}", EditorStyles.boldLabel);
            foreach (var enc in ctx.Encounters)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Room {enc.RoomId}: {enc.Type} | Diff={enc.Difficulty:F2} | Faction={enc.FactionId}");
                EditorGUILayout.LabelField($"  Spawn Points: {enc.SpawnPoints.Count}");
                foreach (var sp in enc.SpawnPoints.Take(5))
                {
                    EditorGUILayout.LabelField($"    {sp.EnemyTypeId} at {sp.Position}");
                }
                EditorGUILayout.EndVertical();
            }
        }
    }
}
#endif
