using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.Optimization
{
    /// <summary>
    /// Stage 9: Final optimization pass.
    /// Removes redundant data, clusters decorations, prepares for rendering.
    /// </summary>
    public class OptimizationStage : IGenerationStage
    {
        public string StageName => "Final Optimization";
        public int Priority => 900;
        public void Execute(GenerationContext context)
        {
            RemoveIsolatedTiles(context);
            ClampDecorationDensity(context);
            ComputeStatistics(context);
        }
        private void RemoveIsolatedTiles(GenerationContext context)
        {
            var map = context.SpatialMap;
            for (int x = 1; x < map.Width - 1; x++)
            {
                for (int y = 1; y < map.Height - 1; y++)
                {
                    var tile = map.GetTile(x, y);
                    if (tile.Type != TileType.Floor && tile.Type != TileType.Corridor) continue;
                    // Check if surrounded by walls
                    int wallCount = 0;
                    foreach (var n in map.GetNeighbors(tile.Position))
                    {
                        if (map.GetTile(n).Type == TileType.Wall) wallCount++;
                    }
                    // Isolated floor tile surrounded by 4 walls
                    if (wallCount >= 4)
                        map.SetTile(x, y, TileType.Wall);
                }
            }
        }
        private void ClampDecorationDensity(GenerationContext context)
        {
            // Remove excess decorations per room to prevent visual clutter
            var roomDecorationCounts = new System.Collections.Generic.Dictionary<int, int>();
            var toRemove = new System.Collections.Generic.List<int>();
            for (int i = 0; i < context.Decorations.Count; i++)
            {
                var dec = context.Decorations[i];
                if (!roomDecorationCounts.ContainsKey(dec.RoomId))
                    roomDecorationCounts[dec.RoomId] = 0;
                roomDecorationCounts[dec.RoomId]++;
                // Max 20 decorations per room
                if (roomDecorationCounts[dec.RoomId] > 20)
                    toRemove.Add(i);
            }
            for (int i = toRemove.Count - 1; i >= 0; i--)
                context.Decorations.RemoveAt(toRemove[i]);
        }
        private void ComputeStatistics(GenerationContext context)
        {
            var stats = new System.Collections.Generic.Dictionary<string, object>
            {
                ["room_count"] = context.SpatialMap.Rooms.Count,
                ["corridor_count"] = context.SpatialMap.Corridors.Count,
                ["door_count"] = context.SpatialMap.Doors.Count,
                ["story_marker_count"] = context.StoryMarkers.Count,
                ["decoration_count"] = context.Decorations.Count,
                ["encounter_count"] = context.Encounters.Count,
                ["history_event_count"] = context.HistoryLog?.Events.Count ?? 0
            };
            context.SetCustomData("generation_stats", stats);
            UnityEngine.Debug.Log($"[DungeonGen] Stats: {stats["room_count"]} rooms, {stats["corridor_count"]} corridors, " +
                                  $"{stats["door_count"]} doors, {stats["encounter_count"]} encounters, " +
                                  $"{stats["decoration_count"]} decorations, {stats["story_marker_count"]} story markers");
        }
    }
}
