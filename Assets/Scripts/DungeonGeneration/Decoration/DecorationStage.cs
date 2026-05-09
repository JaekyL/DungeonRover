using System.Collections.Generic;
using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.Decoration
{
    /// <summary>
    /// Stage 7: Procedural decoration placement.
    /// Uses room purpose, history, and weighted randomization for variation.
    /// </summary>
    public class DecorationStage : IGenerationStage
    {
        public string StageName => "Decoration Pass";
        public int Priority => 700;
        private static readonly Dictionary<RoomPurposeType, string[]> PurposeDecorations = new Dictionary<RoomPurposeType, string[]>
        {
            { RoomPurposeType.Barracks, new[] { "bed", "weapon_rack", "chest", "stool", "table" } },
            { RoomPurposeType.Shrine, new[] { "altar", "candles", "statue", "incense", "offering_bowl" } },
            { RoomPurposeType.Library, new[] { "bookshelf", "desk", "candelabra", "scroll_pile", "reading_chair" } },
            { RoomPurposeType.Prison, new[] { "shackles", "iron_bars", "bucket", "straw_pile", "chains" } },
            { RoomPurposeType.Treasury, new[] { "gold_pile", "gem_cluster", "locked_chest", "display_case", "safe" } },
            { RoomPurposeType.Armory, new[] { "sword_rack", "armor_stand", "shield_display", "anvil", "grindstone" } },
            { RoomPurposeType.Kitchen, new[] { "cooking_pot", "barrel", "chopping_block", "pantry", "firepit" } },
            { RoomPurposeType.StorageRoom, new[] { "crate", "barrel", "sack", "shelf", "box" } },
            { RoomPurposeType.Workshop, new[] { "workbench", "tools", "material_pile", "blueprint", "mechanical" } },
            { RoomPurposeType.Crypt, new[] { "sarcophagus", "tombstone", "candle_cluster", "cobweb", "bones" } },
            { RoomPurposeType.RitualChamber, new[] { "ritual_circle", "candle_ring", "blood_bowl", "dark_altar", "rune_stone" } },
            { RoomPurposeType.Nest, new[] { "bone_pile", "webbing", "egg_cluster", "debris", "carcass" } },
            { RoomPurposeType.Entrance, new[] { "torch_sconce", "welcome_mat", "signpost", "gate_mechanism" } },
            { RoomPurposeType.ThroneRoom, new[] { "throne", "banner", "carpet", "pillar", "chandelier" } },
        };
        public void Execute(GenerationContext context)
        {
            var rng = context.Random.Fork("decoration");
            float density = context.Config.decorationDensity;
            foreach (var room in context.SpatialMap.Rooms)
            {
                DecorateRoom(room, context, rng, density);
            }
        }
        private void DecorateRoom(RoomInstance room, GenerationContext context, SeededRandom rng, float density)
        {
            string[] decorTypes;
            if (!PurposeDecorations.TryGetValue(room.Purpose, out decorTypes))
                decorTypes = new[] { "rubble", "cobweb", "moss", "crack" };
            int maxDecorations = (int)(room.FloorTiles.Count * density * 0.15f);
            for (int i = 0; i < maxDecorations && i < room.FloorTiles.Count; i++)
            {
                if (!rng.NextBool(density)) continue;
                var tile = rng.Choose(room.FloorTiles);
                var decorId = rng.Choose(decorTypes);
                // Microvariation: rotation, scale, mirroring
                float rotation = rng.Next(4) * 90f;
                bool mirrored = rng.NextBool(0.3f);
                float scaleVar = rng.NextFloat(0.85f, 1.15f);
                context.Decorations.Add(new DecorationInstance
                {
                    Position = tile,
                    DecorationId = decorId,
                    RoomId = room.Id,
                    Rotation = rotation,
                    Scale = new UnityEngine.Vector3(mirrored ? -scaleVar : scaleVar, scaleVar, scaleVar),
                    IsMirrored = mirrored,
                    Category = room.Purpose.ToString()
                });
            }
        }
    }
}
