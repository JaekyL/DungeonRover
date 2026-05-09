using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.History.Agents
{
    /// <summary>
    /// Flooding damages lower rooms, creates water tiles, damages structures.
    /// </summary>
    public class FloodingAgent : IHistoryAgent
    {
        public string AgentName => "Flooding";
        public int Priority => 30;
        private float _intensity;
        public void Initialize(GenerationContext context, float intensity, SeededRandom rng)
        {
            _intensity = intensity;
        }
        public void SimulateStep(int step, GenerationContext context, SeededRandom rng)
        {
            var map = context.SpatialMap;
            var log = context.HistoryLog;
            // Flood lowest rooms (by Y position)
            var sortedRooms = map.Rooms.OrderBy(r => r.Bounds.y).ToList();
            int floodCount = (int)(sortedRooms.Count * _intensity * 0.3f);
            for (int i = 0; i < floodCount && i < sortedRooms.Count; i++)
            {
                var room = sortedRooms[i];
                float floodLevel = _intensity * (1f - (float)i / floodCount);
                foreach (var tile in room.FloorTiles)
                {
                    if (rng.NextBool(floodLevel * 0.4f))
                    {
                        map.SetTile(tile.x, tile.y, TileType.Water);
                        var td = map.GetTile(tile);
                        if (td != null)
                        {
                            td.DamageLevel += floodLevel * 0.3f;
                            td.Tags.Add("water_damage");
                        }
                    }
                }
                log.RecordModification(room.Id, "flooding");
                log.AddEvent(new HistoryEvent
                {
                    Step = step, AgentType = AgentName, EventType = "flood",
                    AffectedRoomId = room.Id, Description = $"Room flooded at level {floodLevel:F2}"
                });
            }
        }
    }
}
