using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.RoomPurpose
{
    /// <summary>
    /// Stage 4: Assigns semantic identities to rooms based on graph metadata,
    /// history simulation results, and weighted random selection.
    /// </summary>
    public class RoomPurposeStage : IGenerationStage
    {
        public string StageName => "Room Purpose Assignment";
        public int Priority => 400;
        private static readonly RoomPurposeType[] DefaultPurposes =
        {
            RoomPurposeType.Barracks, RoomPurposeType.Shrine, RoomPurposeType.Library,
            RoomPurposeType.Prison, RoomPurposeType.Treasury, RoomPurposeType.Armory,
            RoomPurposeType.Kitchen, RoomPurposeType.StorageRoom, RoomPurposeType.Workshop,
            RoomPurposeType.Crypt, RoomPurposeType.Garden
        };
        public void Execute(GenerationContext context)
        {
            var rng = context.Random.Fork("purpose");
            var map = context.SpatialMap;
            foreach (var room in map.Rooms)
            {
                // Skip rooms already assigned by history agents
                if (room.Purpose != RoomPurposeType.None) continue;
                var node = context.Graph.GetNode(room.GraphNodeId);
                if (node == null) continue;
                room.Purpose = DeterminePurpose(node, room, context, rng);
                node.Purpose = room.Purpose;
            }
        }
        private RoomPurposeType DeterminePurpose(GraphNode node, RoomInstance room,
            GenerationContext context, SeededRandom rng)
        {
            // Special room types get fixed purposes
            switch (node.RoomType)
            {
                case RoomType.Start: return RoomPurposeType.Entrance;
                case RoomType.Boss: return RoomPurposeType.ThroneRoom;
                case RoomType.Treasure: return RoomPurposeType.Treasury;
            }
            // If config has purpose definitions, use weighted selection
            var definitions = context.Config.roomPurposeDefinitions;
            if (definitions != null && definitions.Count > 0)
            {
                var valid = definitions
                    .Where(d => d.minImportance <= node.Importance)
                    .ToList();
                if (valid.Count > 0)
                {
                    var purposes = valid.Select(d => d.purposeType).ToList();
                    var weights = valid.Select(d => d.weight).ToList();
                    return rng.ChooseWeighted(purposes, weights);
                }
            }
            // Faction-based purpose
            if (!string.IsNullOrEmpty(room.FactionOwner))
            {
                switch (room.FactionOwner)
                {
                    case "Cultists": return RoomPurposeType.RitualChamber;
                    case "Monsters": return RoomPurposeType.Nest;
                }
            }
            // Check for water damage
            bool hasWaterDamage = room.FloorTiles.Any(t =>
            {
                var td = context.SpatialMap.GetTile(t);
                return td != null && td.Tags.Contains("water_damage");
            });
            if (hasWaterDamage) return RoomPurposeType.FloodedArchive;
            // Default weighted random
            return rng.Choose(DefaultPurposes);
        }
    }
}
