using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using DungeonGeneration.Narrative.WorldState;

namespace DungeonGeneration.Narrative.EnvironmentalStorytelling
{
    /// <summary>
    /// Interface for scene generators that create coherent environmental storytelling
    /// compositions. Each scene represents a readable narrative moment within a room.
    /// </summary>
    public interface IStoryScene
    {
        string SceneName { get; }
        bool CanGenerate(RoomNarrativeState roomState, RoomInstance room);
        void Generate(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, SeededRandom rng);
    }

    /// <summary>
    /// Layer 6: Environmental Storytelling Stage.
    /// Generates coherent storytelling scenes and environmental evidence.
    /// Runs at priority 605 (after existing Storytelling at 600).
    /// IMPORTANT: Props are NEVER placed randomly. Every placement has narrative justification.
    /// </summary>
    public class EnvironmentalStorytellingStage : IGenerationStage
    {
        public string StageName => "Narrative Environmental Storytelling";
        public int Priority => 605;

        private readonly List<IStoryScene> _scenes;

        public EnvironmentalStorytellingStage()
        {
            _scenes = new List<IStoryScene>
            {
                new BattleScene(),
                new AbandonedCampScene(),
                new RitualScene(),
                new BarricadeScene(),
                new EscapeScene(),
                new DecayScene(),
                new DefensivePositionScene(),
                new LootingEvidenceScene()
            };
        }

        public void AddScene(IStoryScene scene) => _scenes.Add(scene);

        public void Execute(GenerationContext context)
        {
            var worldState = context.GetCustomData<NarrativeWorldState>("narrative_world_state");
            if (worldState == null || context.SpatialMap == null) return;

            var rng = context.Random.Fork("env_storytelling");
            int scenesGenerated = 0;

            foreach (var room in context.SpatialMap.Rooms)
            {
                var roomState = worldState.GetRoomState(room.Id);

                foreach (var scene in _scenes)
                {
                    if (scene.CanGenerate(roomState, room))
                    {
                        scene.Generate(roomState, room, context, rng.Fork($"{scene.SceneName}_{room.Id}"));
                        scenesGenerated++;
                    }
                }
            }

            UnityEngine.Debug.Log($"[Narrative] Environmental storytelling: {scenesGenerated} scenes generated");
        }
    }

    // --- Scene Implementations ---

    public class BattleScene : IStoryScene
    {
        public string SceneName => "Battle";

        public bool CanGenerate(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.IsWarzone && room.FloorTiles.Count > 6;
        }

        public void Generate(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, SeededRandom rng)
        {
            // Reconstruct a readable battle scene:
            // 1. Defenders near entry points (skeletons with weapons)
            // 2. Attackers scattered deeper in room
            // 3. Overturned furniture as cover
            // 4. Blood trails showing movement
            // The player should read: "There was a fight here. Defenders tried to hold this room."

            // Skip if already heavily marked
            if (context.StoryMarkers.Count(m => m.RoomId == room.Id) > 8) return;

            // Defensive positions near entries
            foreach (var entry in room.EntryPoints.Take(2))
            {
                context.StoryMarkers.Add(new StoryMarker
                {
                    Position = entry,
                    Type = StoryMarkerType.Skeleton,
                    RoomId = room.Id,
                    FactionSource = roomState.CurrentOwner ?? FindPreviousOwner(roomState),
                    Description = "Defender fell holding the entrance",
                    Intensity = 0.9f
                });
            }

            // Attacker bodies deeper in room
            if (room.FloorTiles.Count > 4)
            {
                var deepTiles = room.FloorTiles.Skip(room.FloorTiles.Count / 2).ToList();
                if (deepTiles.Count > 0)
                {
                    context.StoryMarkers.Add(new StoryMarker
                    {
                        Position = rng.Choose(deepTiles),
                        Type = StoryMarkerType.Skeleton,
                        RoomId = room.Id,
                        Description = "Fallen attacker deep in room — breached defenses",
                        Intensity = 0.7f
                    });
                }
            }
        }

        private string FindPreviousOwner(RoomNarrativeState state)
        {
            return state.OwnershipHistory.Count > 0
                ? state.OwnershipHistory[state.OwnershipHistory.Count - 1].PreviousOwner
                : "unknown";
        }
    }

    public class AbandonedCampScene : IStoryScene
    {
        public string SceneName => "AbandonedCamp";

        public bool CanGenerate(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.IsAbandoned && !roomState.IsCollapsed && room.FloorTiles.Count > 4;
        }

        public void Generate(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, SeededRandom rng)
        {
            // Readable scene: "People lived here. They left."
            var center = room.FloorTiles[room.FloorTiles.Count / 2];

            context.Decorations.Add(new DecorationInstance
            {
                Position = center,
                DecorationId = "cold_campfire",
                RoomId = room.Id,
                Category = "storytelling"
            });

            // Bedrolls around campfire
            var nearCenter = room.FloorTiles
                .Where(t => UnityEngine.Vector2Int.Distance(t, center) < 3 && t != center)
                .Take(3).ToList();

            foreach (var tile in nearCenter)
            {
                context.Decorations.Add(new DecorationInstance
                {
                    Position = tile,
                    DecorationId = "abandoned_bedroll",
                    RoomId = room.Id,
                    Rotation = rng.NextFloat(0, 360),
                    Category = "storytelling"
                });
            }

            // Scattered supplies
            if (room.FloorTiles.Count > 0)
            {
                context.Decorations.Add(new DecorationInstance
                {
                    Position = rng.Choose(room.FloorTiles),
                    DecorationId = "scattered_supplies",
                    RoomId = room.Id,
                    Category = "storytelling"
                });
            }
        }
    }

    public class RitualScene : IStoryScene
    {
        public string SceneName => "Ritual";

        public bool CanGenerate(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.IsRitualSite && room.FloorTiles.Count > 8;
        }

        public void Generate(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, SeededRandom rng)
        {
            // Readable scene: "Someone performed a dark ritual here."
            var center = room.FloorTiles[room.FloorTiles.Count / 2];

            context.Decorations.Add(new DecorationInstance
            {
                Position = center,
                DecorationId = "dark_altar",
                RoomId = room.Id,
                Category = "ritual"
            });

            // Offering items around altar
            var nearCenter = room.FloorTiles
                .Where(t => UnityEngine.Vector2Int.Distance(t, center) < 5 && t != center)
                .Take(4).ToList();

            foreach (var tile in nearCenter)
            {
                var decorId = rng.Choose(new[] { "offering_bowl", "blood_stain", "rune_stone", "ritual_candle" });
                context.Decorations.Add(new DecorationInstance
                {
                    Position = tile,
                    DecorationId = decorId,
                    RoomId = room.Id,
                    Rotation = rng.NextFloat(0, 360),
                    Category = "ritual"
                });
            }
        }
    }

    public class BarricadeScene : IStoryScene
    {
        public string SceneName => "Barricade";

        public bool CanGenerate(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.IsBarricaded && room.EntryPoints.Count > 0;
        }

        public void Generate(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, SeededRandom rng)
        {
            // Readable scene: "Someone fortified this room to keep something out."
            foreach (var entry in room.EntryPoints)
            {
                context.Decorations.Add(new DecorationInstance
                {
                    Position = entry,
                    DecorationId = "overturned_table_barricade",
                    RoomId = room.Id,
                    Rotation = rng.NextFloat(-15f, 15f),
                    Category = "barricade"
                });
            }

            // Supplies stacked inside (they were preparing for siege)
            if (room.FloorTiles.Count > 3)
            {
                context.Decorations.Add(new DecorationInstance
                {
                    Position = rng.Choose(room.FloorTiles),
                    DecorationId = "stacked_crates",
                    RoomId = room.Id,
                    Category = "barricade"
                });
            }
        }
    }

    public class EscapeScene : IStoryScene
    {
        public string SceneName => "Escape";

        public bool CanGenerate(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.SemanticTags.Contains("abandoned_in_haste") && room.EntryPoints.Count > 0;
        }

        public void Generate(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, SeededRandom rng)
        {
            // Readable scene: "People fled this room in panic."
            // Trail of dropped items leading to exit
            if (room.EntryPoints.Count > 0 && room.FloorTiles.Count > 0)
            {
                var exitPoint = room.EntryPoints[room.EntryPoints.Count - 1];

                // Drop trail of items from center to exit
                var center = room.FloorTiles[room.FloorTiles.Count / 2];
                var trailTiles = room.FloorTiles
                    .OrderBy(t => UnityEngine.Vector2Int.Distance(t, exitPoint))
                    .Take(4).ToList();

                var dropItems = new[] { "dropped_lantern", "torn_cloth", "dropped_pack", "broken_vial" };
                for (int i = 0; i < trailTiles.Count && i < dropItems.Length; i++)
                {
                    context.Decorations.Add(new DecorationInstance
                    {
                        Position = trailTiles[i],
                        DecorationId = dropItems[i],
                        RoomId = room.Id,
                        Rotation = rng.NextFloat(0, 360),
                        Category = "escape_evidence"
                    });
                }
            }
        }
    }

    public class DecayScene : IStoryScene
    {
        public string SceneName => "Decay";

        public bool CanGenerate(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.Decay > 0.4f && room.FloorTiles.Count > 2;
        }

        public void Generate(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, SeededRandom rng)
        {
            // Readable scene: "This place has been empty for a very long time."
            float decay = roomState.Decay;

            // Cobwebs in corners (wall tiles)
            foreach (var wall in room.WallTiles.Take(4))
            {
                if (rng.NextBool(decay * 0.3f))
                {
                    context.Decorations.Add(new DecorationInstance
                    {
                        Position = wall,
                        DecorationId = "thick_cobweb",
                        RoomId = room.Id,
                        Category = "decay"
                    });
                }
            }

            // Crumbling furniture
            if (room.FloorTiles.Count > 0 && rng.NextBool(0.5f))
            {
                context.Decorations.Add(new DecorationInstance
                {
                    Position = rng.Choose(room.FloorTiles),
                    DecorationId = "collapsed_shelf",
                    RoomId = room.Id,
                    Category = "decay"
                });
            }
        }
    }

    public class DefensivePositionScene : IStoryScene
    {
        public string SceneName => "DefensivePosition";

        public bool CanGenerate(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.SemanticTags.Contains("fortified") && !roomState.IsWarzone;
        }

        public void Generate(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, SeededRandom rng)
        {
            // Readable scene: "This room was prepared for an attack that may or may not have come."
            // Arrow slits / murder holes near entries
            foreach (var entry in room.EntryPoints)
            {
                context.Decorations.Add(new DecorationInstance
                {
                    Position = entry,
                    DecorationId = "defensive_position",
                    RoomId = room.Id,
                    Category = "fortification"
                });
            }

            // Stored weapons
            if (room.FloorTiles.Count > 0)
            {
                context.Decorations.Add(new DecorationInstance
                {
                    Position = rng.Choose(room.FloorTiles),
                    DecorationId = "weapon_cache",
                    RoomId = room.Id,
                    Category = "fortification"
                });
            }
        }
    }

    public class LootingEvidenceScene : IStoryScene
    {
        public string SceneName => "LootingEvidence";

        public bool CanGenerate(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.OwnershipHistory.Count >= 2 && !roomState.IsRitualSite;
        }

        public void Generate(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, SeededRandom rng)
        {
            // Readable scene: "This room changed hands. The new occupants ransacked what was left."
            if (room.FloorTiles.Count > 0)
            {
                int lootCount = rng.Next(1, 4);
                for (int i = 0; i < lootCount; i++)
                {
                    context.StoryMarkers.Add(new StoryMarker
                    {
                        Position = rng.Choose(room.FloorTiles),
                        Type = StoryMarkerType.LootRemains,
                        RoomId = room.Id,
                        Description = "Ransacked container — emptied by new occupants",
                        Intensity = 0.5f
                    });
                }
            }
        }
    }
}

