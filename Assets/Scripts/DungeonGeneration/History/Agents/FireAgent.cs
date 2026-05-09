using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.History.Agents
{
    public class FireAgent : IHistoryAgent
    {
        public string AgentName => "Fire";
        public int Priority => 40;
        private float _intensity;
        public void Initialize(GenerationContext context, float intensity, SeededRandom rng) => _intensity = intensity;
        public void SimulateStep(int step, GenerationContext context, SeededRandom rng)
        {
            if (step != 0) return; // Fire is a single event
            var map = context.SpatialMap;
            if (map.Rooms.Count == 0) return;
            int fireRoomIdx = rng.Next(map.Rooms.Count);
            var room = map.Rooms[fireRoomIdx];
            foreach (var tile in room.FloorTiles)
            {
                if (rng.NextBool(_intensity * 0.6f))
                {
                    var td = map.GetTile(tile);
                    if (td != null)
                    {
                        td.DamageLevel += _intensity * 0.5f;
                        td.Tags.Add("burn_marks");
                    }
                }
            }
            // Collapse some walls
            foreach (var tile in room.WallTiles)
            {
                if (rng.NextBool(_intensity * 0.2f))
                    map.SetTile(tile.x, tile.y, TileType.Rubble);
            }
            context.HistoryLog.RecordModification(room.Id, "fire_damage");
            context.HistoryLog.AddEvent(new HistoryEvent
            {
                Step = step, AgentType = AgentName, EventType = "fire",
                AffectedRoomId = room.Id, Description = "Fire destroyed parts of the room"
            });
        }
    }
}
