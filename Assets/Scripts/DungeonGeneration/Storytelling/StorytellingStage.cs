using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.Storytelling
{
    /// <summary>
    /// Stage 6: Generates environmental storytelling markers based on history simulation results.
    /// Infers visual storytelling from historical events.
    /// </summary>
    public class StorytellingStage : IGenerationStage
    {
        public string StageName => "Environmental Storytelling";
        public int Priority => 600;
        private readonly List<IStoryRule> _rules;
        public StorytellingStage()
        {
            _rules = new List<IStoryRule>
            {
                new InvasionBattleRule(),
                new CultistRitualRule(),
                new FloodDamageRule(),
                new FireDamageRule(),
                new MonsterNestRule(),
                new CorruptionRule(),
                new DecayRule()
            };
        }
        public void AddRule(IStoryRule rule) => _rules.Add(rule);
        public void Execute(GenerationContext context)
        {
            var rng = context.Random.Fork("story");
            if (context.HistoryLog == null) return;
            foreach (var room in context.SpatialMap.Rooms)
            {
                var events = context.HistoryLog.GetEventsForRoom(room.Id);
                foreach (var rule in _rules)
                {
                    rule.Apply(room, events, context, rng);
                }
            }
        }
    }
    public interface IStoryRule
    {
        void Apply(RoomInstance room, List<HistoryEvent> events, GenerationContext context, SeededRandom rng);
    }
    public class InvasionBattleRule : IStoryRule
    {
        public void Apply(RoomInstance room, List<HistoryEvent> events, GenerationContext context, SeededRandom rng)
        {
            var invasionEvents = events.Where(e => e.EventType == "invasion").ToList();
            if (invasionEvents.Count == 0) return;
            // Add corpses near chokepoints (entry points)
            foreach (var entry in room.EntryPoints)
            {
                if (rng.NextBool(0.6f))
                    context.StoryMarkers.Add(new StoryMarker
                    {
                        Position = entry, Type = StoryMarkerType.Skeleton,
                        RoomId = room.Id, FactionSource = "Invaders",
                        Description = "Fallen defender at chokepoint", Intensity = 0.8f
                    });
            }
            // Scatter weapons and barricades
            foreach (var tile in room.FloorTiles)
            {
                if (rng.NextBool(0.08f))
                    context.StoryMarkers.Add(new StoryMarker
                    {
                        Position = tile, Type = rng.NextBool() ? StoryMarkerType.WeaponScatter : StoryMarkerType.Barricade,
                        RoomId = room.Id, FactionSource = "Invaders", Intensity = 0.5f
                    });
            }
            // Blood trails
            if (room.FloorTiles.Count > 0 && rng.NextBool(0.7f))
            {
                context.StoryMarkers.Add(new StoryMarker
                {
                    Position = rng.Choose(room.FloorTiles), Type = StoryMarkerType.BloodTrail,
                    RoomId = room.Id, FactionSource = "Invaders",
                    Description = "Blood trail from battle", Intensity = 0.6f
                });
            }
        }
    }
    public class CultistRitualRule : IStoryRule
    {
        public void Apply(RoomInstance room, List<HistoryEvent> events, GenerationContext context, SeededRandom rng)
        {
            if (!events.Any(e => e.AgentType == "Cultists")) return;
            // Ritual markings at room center
            if (room.FloorTiles.Count > 0)
            {
                var center = room.FloorTiles[room.FloorTiles.Count / 2];
                context.StoryMarkers.Add(new StoryMarker
                {
                    Position = center, Type = StoryMarkerType.RitualMarking,
                    RoomId = room.Id, FactionSource = "Cultists",
                    Description = "Ritual circle", Intensity = 1f
                });
            }
            // Notes/scrolls
            if (rng.NextBool(0.4f) && room.FloorTiles.Count > 0)
            {
                context.StoryMarkers.Add(new StoryMarker
                {
                    Position = rng.Choose(room.FloorTiles), Type = StoryMarkerType.Note,
                    RoomId = room.Id, FactionSource = "Cultists",
                    Description = "Cultist's journal entry", Intensity = 0.5f
                });
            }
        }
    }
    public class FloodDamageRule : IStoryRule
    {
        public void Apply(RoomInstance room, List<HistoryEvent> events, GenerationContext context, SeededRandom rng)
        {
            if (!events.Any(e => e.EventType == "flood")) return;
            foreach (var tile in room.FloorTiles)
            {
                if (rng.NextBool(0.15f))
                    context.StoryMarkers.Add(new StoryMarker
                    {
                        Position = tile, Type = StoryMarkerType.WaterDamage,
                        RoomId = room.Id, Description = "Water stains on walls", Intensity = 0.4f
                    });
            }
            if (rng.NextBool(0.3f) && room.FloorTiles.Count > 0)
                context.StoryMarkers.Add(new StoryMarker
                {
                    Position = rng.Choose(room.FloorTiles), Type = StoryMarkerType.Decay,
                    RoomId = room.Id, Description = "Rotting furniture from flooding", Intensity = 0.6f
                });
        }
    }
    public class FireDamageRule : IStoryRule
    {
        public void Apply(RoomInstance room, List<HistoryEvent> events, GenerationContext context, SeededRandom rng)
        {
            if (!events.Any(e => e.EventType == "fire")) return;
            foreach (var tile in room.FloorTiles)
            {
                if (rng.NextBool(0.2f))
                    context.StoryMarkers.Add(new StoryMarker
                    {
                        Position = tile, Type = StoryMarkerType.BurnMarks,
                        RoomId = room.Id, Description = "Scorch marks", Intensity = 0.7f
                    });
            }
            if (rng.NextBool(0.5f) && room.FloorTiles.Count > 0)
                context.StoryMarkers.Add(new StoryMarker
                {
                    Position = rng.Choose(room.FloorTiles), Type = StoryMarkerType.CollapsedWall,
                    RoomId = room.Id, Description = "Wall collapsed from fire", Intensity = 0.8f
                });
        }
    }
    public class MonsterNestRule : IStoryRule
    {
        public void Apply(RoomInstance room, List<HistoryEvent> events, GenerationContext context, SeededRandom rng)
        {
            if (!events.Any(e => e.EventType == "create_nest")) return;
            if (room.FloorTiles.Count > 0)
            {
                context.StoryMarkers.Add(new StoryMarker
                {
                    Position = rng.Choose(room.FloorTiles), Type = StoryMarkerType.LootRemains,
                    RoomId = room.Id, FactionSource = "Monsters",
                    Description = "Bones and remains near nest", Intensity = 0.7f
                });
            }
        }
    }
    public class CorruptionRule : IStoryRule
    {
        public void Apply(RoomInstance room, List<HistoryEvent> events, GenerationContext context, SeededRandom rng)
        {
            var mods = context.HistoryLog.RoomModifications;
            if (!mods.ContainsKey(room.Id) || !mods[room.Id].Contains("corruption")) return;
            foreach (var tile in room.FloorTiles)
            {
                if (rng.NextBool(0.1f))
                    context.StoryMarkers.Add(new StoryMarker
                    {
                        Position = tile, Type = StoryMarkerType.FungalGrowth,
                        RoomId = room.Id, Description = "Strange fungal growth", Intensity = 0.5f
                    });
            }
        }
    }
    public class DecayRule : IStoryRule
    {
        public void Apply(RoomInstance room, List<HistoryEvent> events, GenerationContext context, SeededRandom rng)
        {
            // General decay for rooms with no faction
            if (!string.IsNullOrEmpty(room.FactionOwner)) return;
            if (!rng.NextBool(0.3f)) return;
            foreach (var tile in room.FloorTiles)
            {
                if (rng.NextBool(0.05f))
                    context.StoryMarkers.Add(new StoryMarker
                    {
                        Position = tile, Type = StoryMarkerType.Decay,
                        RoomId = room.Id, Description = "Natural decay", Intensity = 0.3f
                    });
            }
        }
    }
}
