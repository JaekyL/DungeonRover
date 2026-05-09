using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using DungeonGeneration.Narrative.WorldState;
using UnityEngine;

namespace DungeonGeneration.Narrative.Interpretation.Interpreters
{
    /// <summary>
    /// Interprets conflict/battle zones: skeletons near chokepoints, weapon scatter,
    /// barricades, blood trails, damaged walls, broken furniture.
    /// </summary>
    public class ConflictInterpreter : ISpatialInterpreter
    {
        public string InterpreterName => "Conflict";
        public int Priority => 10;

        public bool CanInterpret(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.IsWarzone || roomState.ConflictIntensity > 0.2f ||
                   roomState.SemanticTags.Contains("battle_site") ||
                   roomState.SemanticTags.Contains("last_stand");
        }

        public void Interpret(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, InterpretationResult result, SeededRandom rng)
        {
            float intensity = roomState.ConflictIntensity;
            bool isLastStand = roomState.SemanticTags.Contains("last_stand");

            // Skeletons near chokepoints (entry points)
            foreach (var entry in room.EntryPoints)
            {
                if (rng.NextBool(0.5f + intensity * 0.3f))
                {
                    result.Markers.Add(new StoryMarker
                    {
                        Position = entry,
                        Type = StoryMarkerType.Skeleton,
                        RoomId = room.Id,
                        FactionSource = roomState.CurrentOwner ?? "unknown",
                        Description = isLastStand ? "Fallen defender in desperate last stand" : "Fallen combatant at chokepoint",
                        Intensity = 0.8f
                    });
                }
            }

            // Weapon scatter and barricade debris
            int markerCount = (int)(room.FloorTiles.Count * intensity * 0.12f);
            for (int i = 0; i < markerCount && room.FloorTiles.Count > 0; i++)
            {
                var tile = rng.Choose(room.FloorTiles);
                var markerType = rng.NextFloat() < 0.4f ? StoryMarkerType.WeaponScatter :
                    rng.NextFloat() < 0.6f ? StoryMarkerType.Barricade : StoryMarkerType.BrokenDoor;

                result.Markers.Add(new StoryMarker
                {
                    Position = tile,
                    Type = markerType,
                    RoomId = room.Id,
                    FactionSource = "combat",
                    Description = GetCombatDescription(markerType, isLastStand),
                    Intensity = intensity
                });
            }

            // Blood trails
            if (rng.NextBool(0.6f + intensity * 0.2f) && room.FloorTiles.Count > 0)
            {
                result.Markers.Add(new StoryMarker
                {
                    Position = rng.Choose(room.FloorTiles),
                    Type = StoryMarkerType.BloodTrail,
                    RoomId = room.Id,
                    Description = "Blood trail from battle",
                    Intensity = intensity * 0.8f
                });
            }

            // Wall damage from combat
            foreach (var wallTile in room.WallTiles)
            {
                if (rng.NextBool(intensity * 0.15f))
                {
                    result.Modifications.Add(new SpatialModification
                    {
                        Type = SpatialModificationType.DamageWall,
                        Position = wallTile,
                        Description = "Wall damaged during battle",
                        Intensity = intensity * 0.5f
                    });
                }
            }

            // Atmosphere
            result.Atmospheres.Add(new AtmosphereOverlay
            {
                AtmosphereType = isLastStand ? "dread" : "tension",
                Intensity = intensity,
                TintColor = new Color(0.8f, 0.3f, 0.3f, 0.3f),
                Description = isLastStand ? "Heavy air of last desperate battle" : "Remnants of violent conflict"
            });
        }

        private string GetCombatDescription(StoryMarkerType type, bool isLastStand)
        {
            string prefix = isLastStand ? "From desperate last stand: " : "";
            switch (type)
            {
                case StoryMarkerType.WeaponScatter: return prefix + "Scattered weapons from battle";
                case StoryMarkerType.Barricade: return prefix + "Improvised barricade from furniture";
                case StoryMarkerType.BrokenDoor: return prefix + "Door smashed during assault";
                default: return prefix + "Battle debris";
            }
        }
    }

    /// <summary>
    /// Interprets abandoned zones: dropped tools, abandoned camps, warning signs,
    /// scattered personal items, signs of hasty departure.
    /// </summary>
    public class AbandonmentInterpreter : ISpatialInterpreter
    {
        public string InterpreterName => "Abandonment";
        public int Priority => 20;

        public bool CanInterpret(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.IsAbandoned || roomState.SemanticTags.Contains("abandoned") ||
                   roomState.SemanticTags.Contains("abandoned_in_haste");
        }

        public void Interpret(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, InterpretationResult result, SeededRandom rng)
        {
            bool inHaste = roomState.SemanticTags.Contains("abandoned_in_haste");
            float decay = roomState.Decay;

            // Abandoned campfire remains
            if (room.FloorTiles.Count > 4 && rng.NextBool(0.5f))
            {
                var center = room.FloorTiles[room.FloorTiles.Count / 2];
                result.Markers.Add(new StoryMarker
                {
                    Position = center,
                    Type = StoryMarkerType.Campfire,
                    RoomId = room.Id,
                    Description = "Cold campfire remains, abandoned long ago",
                    Intensity = 0.4f + decay * 0.3f
                });
            }

            if (inHaste)
            {
                // Dropped belongings
                int dropCount = rng.Next(2, 5);
                for (int i = 0; i < dropCount && room.FloorTiles.Count > 0; i++)
                {
                    result.Markers.Add(new StoryMarker
                    {
                        Position = rng.Choose(room.FloorTiles),
                        Type = StoryMarkerType.LootRemains,
                        RoomId = room.Id,
                        Description = "Personal belongings dropped in hasty flight",
                        Intensity = 0.5f
                    });
                }

                // Warning scratched on wall
                if (rng.NextBool(0.4f) && room.WallTiles.Count > 0)
                {
                    result.Markers.Add(new StoryMarker
                    {
                        Position = rng.Choose(room.WallTiles),
                        Type = StoryMarkerType.Graffiti,
                        RoomId = room.Id,
                        Description = "Desperate warning scratched into wall",
                        Intensity = 0.7f
                    });
                }
            }

            // General decay
            foreach (var tile in room.FloorTiles)
            {
                if (rng.NextBool(decay * 0.08f))
                {
                    result.Markers.Add(new StoryMarker
                    {
                        Position = tile,
                        Type = StoryMarkerType.Decay,
                        RoomId = room.Id,
                        Description = "Dust and decay from long abandonment",
                        Intensity = decay
                    });
                }
            }

            // Atmosphere
            result.Atmospheres.Add(new AtmosphereOverlay
            {
                AtmosphereType = "desolation",
                Intensity = 0.4f + decay * 0.4f,
                TintColor = new Color(0.6f, 0.6f, 0.5f, 0.3f),
                Description = inHaste ? "Air of panic and hasty departure" : "Silent emptiness of a forgotten place"
            });
        }
    }

    /// <summary>
    /// Interprets occupied territory: faction-specific decorations, supplies,
    /// territorial markers, organized camps.
    /// </summary>
    public class OccupationInterpreter : ISpatialInterpreter
    {
        public string InterpreterName => "Occupation";
        public int Priority => 30;

        public bool CanInterpret(RoomNarrativeState roomState, RoomInstance room)
        {
            return !string.IsNullOrEmpty(roomState.CurrentOwner) && !roomState.IsAbandoned;
        }

        public void Interpret(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, InterpretationResult result, SeededRandom rng)
        {
            bool isFortified = roomState.SemanticTags.Contains("fortified") ||
                               roomState.SemanticTags.Contains("emergency_fortification");
            bool isBarricaded = roomState.IsBarricaded;

            // Territorial markings
            if (room.WallTiles.Count > 0 && rng.NextBool(0.5f))
            {
                result.Markers.Add(new StoryMarker
                {
                    Position = rng.Choose(room.WallTiles),
                    Type = StoryMarkerType.Graffiti,
                    RoomId = room.Id,
                    FactionSource = roomState.CurrentOwner,
                    Description = $"Territorial marking of {roomState.CurrentOwner}",
                    Intensity = 0.4f
                });
            }

            // Barricades at entry points
            if (isBarricaded || isFortified)
            {
                foreach (var entry in room.EntryPoints)
                {
                    if (rng.NextBool(0.7f))
                    {
                        result.Markers.Add(new StoryMarker
                        {
                            Position = entry,
                            Type = StoryMarkerType.Barricade,
                            RoomId = room.Id,
                            FactionSource = roomState.CurrentOwner,
                            Description = isFortified ? "Reinforced defensive barricade" : "Improvised emergency barricade",
                            Intensity = isFortified ? 0.8f : 0.5f
                        });

                        result.Modifications.Add(new SpatialModification
                        {
                            Type = SpatialModificationType.AddBarricade,
                            Position = entry,
                            Description = "Barricade blocking entry",
                            Intensity = 0.6f
                        });
                    }
                }
            }

            // Supplies and camp furnishings
            int supplyCount = (int)(room.FloorTiles.Count * 0.06f);
            for (int i = 0; i < supplyCount && room.FloorTiles.Count > 0; i++)
            {
                result.Decorations.Add(new DecorationInstance
                {
                    Position = rng.Choose(room.FloorTiles),
                    DecorationId = GetOccupationDecoration(roomState.CurrentOwner, rng),
                    RoomId = room.Id,
                    Rotation = rng.Next(4) * 90f,
                    Scale = Vector3.one * rng.NextFloat(0.9f, 1.1f),
                    Category = "occupation"
                });
            }

            // Notes/messages
            if (rng.NextBool(0.3f) && room.FloorTiles.Count > 0)
            {
                result.Markers.Add(new StoryMarker
                {
                    Position = rng.Choose(room.FloorTiles),
                    Type = StoryMarkerType.Note,
                    RoomId = room.Id,
                    FactionSource = roomState.CurrentOwner,
                    Description = $"Written message left by {roomState.CurrentOwner}",
                    Intensity = 0.4f
                });
            }
        }

        private string GetOccupationDecoration(string factionId, SeededRandom rng)
        {
            var general = new[] { "bedroll", "supply_crate", "water_barrel", "torch_stand", "stool" };
            return rng.Choose(general);
        }
    }

    /// <summary>
    /// Interprets ritual sites: ritual circles, candles, offerings, blood bowls,
    /// rune stones, dark altar arrangements.
    /// </summary>
    public class RitualInterpreter : ISpatialInterpreter
    {
        public string InterpreterName => "Ritual";
        public int Priority => 40;

        public bool CanInterpret(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.IsRitualSite || roomState.SemanticTags.Contains("ritual_site") ||
                   roomState.SemanticTags.Contains("desperate_ritual");
        }

        public void Interpret(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, InterpretationResult result, SeededRandom rng)
        {
            bool isDesperate = roomState.SemanticTags.Contains("desperate_ritual");

            // Central ritual circle
            if (room.FloorTiles.Count > 0)
            {
                var center = room.FloorTiles[room.FloorTiles.Count / 2];
                result.Markers.Add(new StoryMarker
                {
                    Position = center,
                    Type = StoryMarkerType.RitualMarking,
                    RoomId = room.Id,
                    FactionSource = roomState.CurrentOwner,
                    Description = isDesperate ? "Hastily drawn ritual circle, blood-inked" : "Elaborate ritual circle with precision markings",
                    Intensity = isDesperate ? 0.9f : 0.7f
                });

                // Candle ring around center
                var nearCenter = room.FloorTiles.Where(t =>
                    Vector2Int.Distance(t, center) < 4 && t != center).ToList();
                foreach (var tile in nearCenter.Take(6))
                {
                    result.Decorations.Add(new DecorationInstance
                    {
                        Position = tile,
                        DecorationId = isDesperate ? "blood_candle" : "ritual_candle",
                        RoomId = room.Id,
                        Rotation = rng.NextFloat(0, 360),
                        Category = "ritual"
                    });
                }
            }

            // Corruption from ritual
            if (roomState.Corruption > 0.3f)
            {
                foreach (var tile in room.FloorTiles)
                {
                    if (rng.NextBool(roomState.Corruption * 0.1f))
                    {
                        result.Modifications.Add(new SpatialModification
                        {
                            Type = SpatialModificationType.CorruptTile,
                            Position = tile,
                            Description = "Floor stained by ritual energy",
                            Intensity = roomState.Corruption
                        });
                    }
                }
            }

            // Offerings / sacrifice evidence
            if (isDesperate && room.FloorTiles.Count > 0)
            {
                result.Markers.Add(new StoryMarker
                {
                    Position = rng.Choose(room.FloorTiles),
                    Type = StoryMarkerType.Skeleton,
                    RoomId = room.Id,
                    FactionSource = roomState.CurrentOwner,
                    Description = "Sacrificial remains at ritual altar",
                    Intensity = 0.9f
                });
            }

            // Dark atmosphere
            result.Atmospheres.Add(new AtmosphereOverlay
            {
                AtmosphereType = isDesperate ? "eldritch_dread" : "dark_sanctum",
                Intensity = 0.7f + roomState.Corruption * 0.3f,
                TintColor = new Color(0.4f, 0.1f, 0.5f, 0.4f),
                Description = isDesperate ? "Oppressive darkness pulsing with failed ritual energy" : "Eerie silence of a profane ritual space"
            });
        }
    }

    /// <summary>
    /// Interprets collapsed areas: rubble, broken supports, cave-ins,
    /// blocked tunnels, structural failure evidence.
    /// </summary>
    public class CollapseInterpreter : ISpatialInterpreter
    {
        public string InterpreterName => "Collapse";
        public int Priority => 50;

        public bool CanInterpret(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.IsCollapsed || roomState.StructuralDamage > 0.5f ||
                   roomState.SemanticTags.Contains("collapsed");
        }

        public void Interpret(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, InterpretationResult result, SeededRandom rng)
        {
            float damage = roomState.StructuralDamage;

            // Rubble piles
            int rubbleCount = (int)(room.FloorTiles.Count * damage * 0.2f);
            for (int i = 0; i < rubbleCount && room.FloorTiles.Count > 0; i++)
            {
                var tile = rng.Choose(room.FloorTiles);
                result.Modifications.Add(new SpatialModification
                {
                    Type = SpatialModificationType.CreateRubble,
                    Position = tile,
                    Description = "Rubble from structural collapse",
                    Intensity = damage
                });
            }

            // Collapsed walls
            foreach (var wall in room.WallTiles)
            {
                if (rng.NextBool(damage * 0.2f))
                {
                    result.Markers.Add(new StoryMarker
                    {
                        Position = wall,
                        Type = StoryMarkerType.CollapsedWall,
                        RoomId = room.Id,
                        Description = "Wall section collapsed from structural failure",
                        Intensity = damage
                    });
                }
            }

            // Block some passages if severely damaged
            if (damage > 0.7f)
            {
                foreach (var entry in room.EntryPoints)
                {
                    if (rng.NextBool(0.3f))
                    {
                        result.Modifications.Add(new SpatialModification
                        {
                            Type = SpatialModificationType.BlockPassage,
                            Position = entry,
                            Description = "Passage blocked by cave-in",
                            Intensity = 1f
                        });
                    }
                }
            }

            result.Atmospheres.Add(new AtmosphereOverlay
            {
                AtmosphereType = "dusty_ruin",
                Intensity = damage,
                TintColor = new Color(0.7f, 0.6f, 0.5f, 0.4f),
                Description = "Dust-choked air from ongoing structural decay"
            });
        }
    }

    /// <summary>
    /// Interprets corruption spread: fungal growth, discoloration, unnatural flora,
    /// warped surfaces, eldritch contamination.
    /// </summary>
    public class CorruptionInterpreter : ISpatialInterpreter
    {
        public string InterpreterName => "Corruption";
        public int Priority => 60;

        public bool CanInterpret(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.Corruption > 0.1f || roomState.SemanticTags.Contains("corrupted") ||
                   roomState.SemanticTags.Contains("corruption_spread");
        }

        public void Interpret(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, InterpretationResult result, SeededRandom rng)
        {
            float corruption = roomState.Corruption;

            // Fungal growth
            foreach (var tile in room.FloorTiles)
            {
                if (rng.NextBool(corruption * 0.12f))
                {
                    result.Markers.Add(new StoryMarker
                    {
                        Position = tile,
                        Type = StoryMarkerType.FungalGrowth,
                        RoomId = room.Id,
                        Description = corruption > 0.7f ? "Pulsating unnatural growth" : "Strange fungal growth",
                        Intensity = corruption
                    });
                }
            }

            // Wall corruption
            foreach (var wall in room.WallTiles)
            {
                if (rng.NextBool(corruption * 0.08f))
                {
                    result.Modifications.Add(new SpatialModification
                    {
                        Type = SpatialModificationType.AddFungalGrowth,
                        Position = wall,
                        Intensity = corruption,
                        Description = "Walls discolored by spreading corruption"
                    });
                }
            }

            // Tile corruption (biome change)
            foreach (var tile in room.FloorTiles)
            {
                if (rng.NextBool(corruption * 0.05f))
                {
                    result.Modifications.Add(new SpatialModification
                    {
                        Type = SpatialModificationType.CorruptTile,
                        Position = tile,
                        Intensity = corruption,
                        Description = "Floor warped by corruption"
                    });
                }
            }

            result.Atmospheres.Add(new AtmosphereOverlay
            {
                AtmosphereType = "corruption",
                Intensity = corruption,
                TintColor = new Color(0.3f, 0.5f, 0.2f, corruption * 0.5f),
                Description = corruption > 0.7f ? "Thick, oppressive miasma of corruption" : "Subtle wrongness in the air"
            });
        }
    }

    /// <summary>
    /// Interprets flooded areas: water damage, waterlogged items, tide marks,
    /// damaged supplies, drowned evidence.
    /// </summary>
    public class FloodInterpreter : ISpatialInterpreter
    {
        public string InterpreterName => "Flood";
        public int Priority => 55;

        public bool CanInterpret(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.IsFlooded || roomState.WaterLevel > 0.1f ||
                   roomState.SemanticTags.Contains("flooded");
        }

        public void Interpret(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, InterpretationResult result, SeededRandom rng)
        {
            float waterLevel = roomState.WaterLevel;

            // Water damage on walls
            foreach (var wall in room.WallTiles)
            {
                if (rng.NextBool(0.3f + waterLevel * 0.3f))
                {
                    result.Markers.Add(new StoryMarker
                    {
                        Position = wall,
                        Type = StoryMarkerType.WaterDamage,
                        RoomId = room.Id,
                        Description = "Water stain marking flood height",
                        Intensity = waterLevel
                    });
                }
            }

            // Flood floor tiles
            foreach (var tile in room.FloorTiles)
            {
                if (rng.NextBool(waterLevel * 0.4f))
                {
                    result.Modifications.Add(new SpatialModification
                    {
                        Type = SpatialModificationType.FloodTile,
                        Position = tile,
                        Description = "Standing water from flooding",
                        Intensity = waterLevel
                    });
                }
            }

            // Waterlogged debris
            if (room.FloorTiles.Count > 0 && rng.NextBool(0.5f))
            {
                result.Markers.Add(new StoryMarker
                {
                    Position = rng.Choose(room.FloorTiles),
                    Type = StoryMarkerType.Decay,
                    RoomId = room.Id,
                    Description = "Rotting furniture waterlogged beyond repair",
                    Intensity = waterLevel * 0.8f
                });
            }

            result.Atmospheres.Add(new AtmosphereOverlay
            {
                AtmosphereType = "damp",
                Intensity = waterLevel,
                TintColor = new Color(0.3f, 0.4f, 0.6f, waterLevel * 0.4f),
                Description = waterLevel > 0.5f ? "Cold stagnant water fills the space" : "Persistent dampness and dripping"
            });
        }
    }

    /// <summary>
    /// Interprets desperation evidence: improvised weapons, torn clothing,
    /// scratched messages, last-resort fortifications, signs of panic.
    /// </summary>
    public class DesperationInterpreter : ISpatialInterpreter
    {
        public string InterpreterName => "Desperation";
        public int Priority => 70;

        public bool CanInterpret(RoomNarrativeState roomState, RoomInstance room)
        {
            return roomState.SemanticTags.Contains("emergency_fortification") ||
                   roomState.SemanticTags.Contains("faction_destroyed_here");
        }

        public void Interpret(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, InterpretationResult result, SeededRandom rng)
        {
            bool factionDied = roomState.SemanticTags.Contains("faction_destroyed_here");

            if (factionDied)
            {
                // Multiple skeletons
                int bodyCount = rng.Next(2, 5);
                for (int i = 0; i < bodyCount && room.FloorTiles.Count > 0; i++)
                {
                    result.Markers.Add(new StoryMarker
                    {
                        Position = rng.Choose(room.FloorTiles),
                        Type = StoryMarkerType.Skeleton,
                        RoomId = room.Id,
                        Description = "Remains of faction's final members",
                        Intensity = 0.9f
                    });
                }

                // Last journal entry
                if (rng.NextBool(0.6f) && room.FloorTiles.Count > 0)
                {
                    result.Markers.Add(new StoryMarker
                    {
                        Position = rng.Choose(room.FloorTiles),
                        Type = StoryMarkerType.Note,
                        RoomId = room.Id,
                        Description = "Final journal entry: a record of everything going wrong",
                        Intensity = 1.0f
                    });
                }
            }

            // Improvised barricades from furniture
            foreach (var entry in room.EntryPoints)
            {
                if (rng.NextBool(0.6f))
                {
                    result.Markers.Add(new StoryMarker
                    {
                        Position = entry,
                        Type = StoryMarkerType.Barricade,
                        RoomId = room.Id,
                        Description = "Desperate furniture barricade, partially collapsed",
                        Intensity = 0.7f
                    });
                }
            }

            // Scratched messages on walls
            if (room.WallTiles.Count > 0)
            {
                result.Markers.Add(new StoryMarker
                {
                    Position = rng.Choose(room.WallTiles),
                    Type = StoryMarkerType.Graffiti,
                    RoomId = room.Id,
                    Description = "Scratched message: plea for help or warning",
                    Intensity = 0.8f
                });
            }

            result.Atmospheres.Add(new AtmosphereOverlay
            {
                AtmosphereType = factionDied ? "tomb" : "despair",
                Intensity = 0.8f,
                TintColor = new Color(0.5f, 0.4f, 0.3f, 0.4f),
                Description = factionDied ? "Oppressive silence of a mass grave" : "Palpable desperation hangs in the air"
            });
        }
    }
}

