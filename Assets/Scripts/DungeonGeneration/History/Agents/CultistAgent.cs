using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.History.Agents
{
    /// <summary>
    /// Cultists claim rooms and create ritual chambers, leave ritual markings.
    /// </summary>
    public class CultistAgent : IHistoryAgent
    {
        public string AgentName => "Cultists";
        public int Priority => 10;
        private float _intensity;
        private int _claimedRoomId = -1;
        public void Initialize(GenerationContext context, float intensity, SeededRandom rng)
        {
            _intensity = intensity;
            if (context.SpatialMap.Rooms.Count > 0)
                _claimedRoomId = rng.Next(context.SpatialMap.Rooms.Count);
        }
        public void SimulateStep(int step, GenerationContext context, SeededRandom rng)
        {
            if (_claimedRoomId < 0 || _claimedRoomId >= context.SpatialMap.Rooms.Count) return;
            var room = context.SpatialMap.Rooms[_claimedRoomId];
            var log = context.HistoryLog;
            if (step == 0)
            {
                log.RecordFaction(room.Id, "Cultists");
                room.FactionOwner = "Cultists";
                room.Purpose = RoomPurposeType.RitualChamber;
                log.AddEvent(new HistoryEvent
                {
                    Step = step, AgentType = AgentName, EventType = "claim_territory",
                    AffectedRoomId = room.Id, Description = "Cultists claimed room as ritual chamber"
                });
            }
            // Spread ritual markings
            if (rng.NextBool(_intensity * 0.5f) && room.FloorTiles.Count > 0)
            {
                var tile = rng.Choose(room.FloorTiles);
                var tileData = context.SpatialMap.GetTile(tile);
                if (tileData != null) tileData.Tags.Add("ritual_marking");
                log.RecordModification(room.Id, "ritual_marking");
                log.AddEvent(new HistoryEvent
                {
                    Step = step, AgentType = AgentName, EventType = "ritual_marking",
                    AffectedRoomId = room.Id, Description = "Cultists added ritual markings"
                });
            }
            // Expand to adjacent rooms
            if (step > 2 && rng.NextBool(_intensity * 0.2f))
            {
                var neighbors = context.Graph.GetNeighbors(_claimedRoomId);
                if (neighbors.Count > 0)
                {
                    var nextRoom = rng.Choose(neighbors);
                    if (nextRoom.Id < context.SpatialMap.Rooms.Count)
                    {
                        log.RecordFaction(nextRoom.Id, "Cultists");
                        _claimedRoomId = nextRoom.Id;
                    }
                }
            }
        }
    }
}
