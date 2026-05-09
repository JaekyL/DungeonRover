using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.History.Agents
{
    public class InvaderAgent : IHistoryAgent
    {
        public string AgentName => "Invaders";
        public int Priority => 15;
        private float _intensity;
        public void Initialize(GenerationContext context, float intensity, SeededRandom rng) => _intensity = intensity;
        public void SimulateStep(int step, GenerationContext context, SeededRandom rng)
        {
            var map = context.SpatialMap;
            if (map.Rooms.Count == 0) return;
            // Invaders attack from the entrance inward
            foreach (var room in map.Rooms)
            {
                var node = context.Graph.GetNode(room.GraphNodeId);
                if (node == null) continue;
                // Affect rooms near critical path
                if (!node.IsCriticalPath || !rng.NextBool(_intensity * 0.3f)) continue;
                // Barricade entry points
                foreach (var entry in room.EntryPoints)
                {
                    if (rng.NextBool(0.5f))
                    {
                        var td = map.GetTile(entry);
                        if (td != null) td.Tags.Add("barricade");
                    }
                }
                // Scatter weapons and corpses
                foreach (var tile in room.FloorTiles)
                {
                    if (rng.NextBool(_intensity * 0.1f))
                    {
                        var td = map.GetTile(tile);
                        if (td != null) td.Tags.Add(rng.NextBool() ? "weapon_scatter" : "skeleton");
                    }
                }
                context.HistoryLog.RecordFaction(room.Id, "Invaders");
                context.HistoryLog.RecordModification(room.Id, "invasion_damage");
                context.HistoryLog.AddEvent(new HistoryEvent
                {
                    Step = step, AgentType = AgentName, EventType = "invasion",
                    AffectedRoomId = room.Id, Description = "Invaders attacked this area"
                });
            }
        }
    }
}
