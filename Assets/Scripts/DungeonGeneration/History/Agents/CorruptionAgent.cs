using DungeonGeneration.Core;

namespace DungeonGeneration.History.Agents
{
    public class CorruptionAgent : IHistoryAgent
    {
        public string AgentName => "Corruption";
        public int Priority => 50;
        private float _intensity;
        private int _sourceRoomId = -1;
        public void Initialize(GenerationContext context, float intensity, SeededRandom rng)
        {
            _intensity = intensity;
            if (context.SpatialMap.Rooms.Count > 0)
                _sourceRoomId = rng.Next(context.SpatialMap.Rooms.Count);
        }
        public void SimulateStep(int step, GenerationContext context, SeededRandom rng)
        {
            if (_sourceRoomId < 0 || _sourceRoomId >= context.SpatialMap.Rooms.Count) return;
            var map = context.SpatialMap;
            var room = map.Rooms[_sourceRoomId];
            // Corruption spreads biome changes
            foreach (var tile in room.FloorTiles)
            {
                if (rng.NextBool(_intensity * 0.3f))
                {
                    var td = map.GetTile(tile);
                    if (td != null)
                    {
                        td.BiomeTag = "corrupted";
                        td.Tags.Add("fungal_growth");
                    }
                }
            }
            context.HistoryLog.RecordModification(room.Id, "corruption");
            // Expand corruption
            if (rng.NextBool(_intensity * 0.3f))
            {
                var neighbors = context.Graph.GetNeighbors(_sourceRoomId);
                if (neighbors.Count > 0)
                    _sourceRoomId = rng.Choose(neighbors).Id;
            }
        }
    }
}
