using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.History.Agents
{
    public class MonsterAgent : IHistoryAgent
    {
        public string AgentName => "Monsters";
        public int Priority => 20;
        private float _intensity;
        public void Initialize(GenerationContext context, float intensity, SeededRandom rng) => _intensity = intensity;
        public void SimulateStep(int step, GenerationContext context, SeededRandom rng)
        {
            if (context.SpatialMap.Rooms.Count == 0) return;
            var map = context.SpatialMap;
            var log = context.HistoryLog;
            // Monsters claim dead-end rooms as nests
            foreach (var room in map.Rooms)
            {
                var node = context.Graph.GetNode(room.GraphNodeId);
                if (node == null || node.RoomType != RoomType.DeadEnd) continue;
                if (!rng.NextBool(_intensity * 0.4f)) continue;
                room.Purpose = RoomPurposeType.Nest;
                room.FactionOwner = "Monsters";
                log.RecordFaction(room.Id, "Monsters");
                foreach (var tile in room.FloorTiles)
                {
                    if (rng.NextBool(0.2f))
                    {
                        var td = map.GetTile(tile);
                        if (td != null) td.Tags.Add("nest_debris");
                    }
                }
                log.AddEvent(new HistoryEvent
                {
                    Step = step, AgentType = AgentName, EventType = "create_nest",
                    AffectedRoomId = room.Id, Description = "Monsters created a nest"
                });
            }
        }
    }
}
