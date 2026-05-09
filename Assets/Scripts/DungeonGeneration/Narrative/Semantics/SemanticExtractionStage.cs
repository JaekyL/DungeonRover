using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Narrative.Authoring;
using DungeonGeneration.Narrative.WorldState;

namespace DungeonGeneration.Narrative.Semantics
{
    /// <summary>
    /// Layer 2: Converts authored narrative content into structured simulation data.
    /// Extracts factions, tensions, conflicts, alliances, and narrative states from
    /// NarrativeConfig ScriptableObjects into the NarrativeWorldState.
    /// Runs as pipeline stage at priority 250 (after spatial layout, before simulation).
    /// </summary>
    public class SemanticExtractionStage : IGenerationStage
    {
        public string StageName => "Narrative Semantic Extraction";
        public int Priority => 250;

        public void Execute(GenerationContext context)
        {
            var config = context.GetCustomData<NarrativeConfig>("narrative_config");
            if (config == null) return;

            var worldState = new NarrativeWorldState();
            context.SetCustomData("narrative_world_state", worldState);

            var rng = context.Random.Fork("narrative_semantics");

            ExtractFactions(config, worldState);
            ExtractCharacters(config, worldState);
            ExtractResources(config, worldState);
            ExtractRelationships(config, worldState);
            ExtractInitialTerritories(config, worldState, context, rng);
            ExtractSemanticTags(config, worldState, context);

            UnityEngine.Debug.Log($"[Narrative] Extracted {worldState.FactionStates.Count} factions, " +
                                  $"{worldState.CharacterStates.Count} characters, " +
                                  $"{worldState.Resources.Count} resources");
        }

        private void ExtractFactions(NarrativeConfig config, NarrativeWorldState worldState)
        {
            foreach (var faction in config.factions)
            {
                var state = worldState.GetFactionState(faction.factionId);
                state.IsActive = true;
                state.Morale = 1f;
                state.Strength = 1f;

                // Extract primary goals from motivations
                foreach (var motivation in faction.motivations)
                {
                    if (motivation.weight > 0.5f)
                        state.ActiveGoals.Add(motivation.type.ToString());
                }
            }
        }

        private void ExtractCharacters(NarrativeConfig config, NarrativeWorldState worldState)
        {
            foreach (var character in config.characters)
            {
                var state = new CharacterState
                {
                    CharacterId = character.characterId,
                    FactionId = character.faction != null ? character.faction.factionId : "",
                    IsAlive = true,
                    Health = 1f
                };
                worldState.CharacterStates[character.characterId] = state;
            }
        }

        private void ExtractResources(NarrativeConfig config, NarrativeWorldState worldState)
        {
            foreach (var resource in config.worldResources)
            {
                worldState.Resources[resource.resourceId] = resource.initialSupply;
            }
        }

        private void ExtractRelationships(NarrativeConfig config, NarrativeWorldState worldState)
        {
            foreach (var faction in config.factions)
            {
                var state = worldState.GetFactionState(faction.factionId);
                foreach (var relationship in faction.relationships)
                {
                    if (relationship.targetFaction == null) continue;
                    state.Dispositions[relationship.targetFaction.factionId] = relationship.initialDisposition;
                }
            }
        }

        private void ExtractInitialTerritories(NarrativeConfig config, NarrativeWorldState worldState,
            GenerationContext context, SeededRandom rng)
        {
            if (context.SpatialMap == null || context.SpatialMap.Rooms.Count == 0) return;

            var availableRooms = new List<int>();
            for (int i = 0; i < context.SpatialMap.Rooms.Count; i++)
                availableRooms.Add(i);

            rng.Shuffle(availableRooms);

            int roomIndex = 0;
            foreach (var faction in config.factions)
            {
                int claimCount = System.Math.Min(faction.preferredTerritorySize,
                    availableRooms.Count - roomIndex);

                for (int i = 0; i < claimCount && roomIndex < availableRooms.Count; i++)
                {
                    int roomId = availableRooms[roomIndex++];
                    var roomState = worldState.GetRoomState(roomId);
                    roomState.SetOwner(faction.factionId, 0);
                    worldState.Territories.SetOwner(roomId, faction.factionId, 0);
                    worldState.GetFactionState(faction.factionId).ControlledRoomIds.Add(roomId);
                    worldState.GetFactionState(faction.factionId).TerritorySize++;

                    // Apply faction's environmental tags
                    foreach (var tag in faction.environmentalTags)
                        roomState.SemanticTags.Add(tag);

                    worldState.Timeline.Record(0, "claim_territory",
                        $"{faction.displayName} claims Room {roomId}",
                        faction.factionId, roomId, 0.3f);
                }
            }
        }

        private void ExtractSemanticTags(NarrativeConfig config, NarrativeWorldState worldState,
            GenerationContext context)
        {
            // Pre-tag rooms based on narrative constraints
            foreach (var constraint in config.constraints)
            {
                if (constraint.type == ConstraintType.RoomMustBeOccupied && !string.IsNullOrEmpty(constraint.targetRoomTag))
                {
                    // Find rooms matching the tag
                    if (context.SpatialMap != null)
                    {
                        foreach (var room in context.SpatialMap.Rooms)
                        {
                            var node = context.Graph?.GetNode(room.GraphNodeId);
                            if (node != null && node.NarrativeTags.Contains(constraint.targetRoomTag))
                            {
                                var state = worldState.GetRoomState(room.Id);
                                state.SemanticTags.Add("narrative_target:" + constraint.targetRoomTag);
                            }
                        }
                    }
                }
            }
        }
    }
}

