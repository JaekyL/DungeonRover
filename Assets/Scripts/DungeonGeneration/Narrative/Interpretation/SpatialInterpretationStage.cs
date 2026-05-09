using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using DungeonGeneration.Narrative.WorldState;

namespace DungeonGeneration.Narrative.Interpretation
{
    /// <summary>
    /// Layer 5: Spatial Interpretation Stage — THE MOST IMPORTANT SYSTEM.
    /// Converts abstract simulation state into spatial/environmental meaning.
    /// Runs all registered ISpatialInterpreter instances on each room,
    /// producing markers, decorations, spatial modifications, and atmosphere overlays.
    /// Priority 450: after RoomPurpose (400), before TraversalValidation (500).
    /// </summary>
    public class SpatialInterpretationStage : IGenerationStage
    {
        public string StageName => "Narrative Spatial Interpretation";
        public int Priority => 450;

        private readonly List<ISpatialInterpreter> _interpreters;

        public SpatialInterpretationStage()
        {
            _interpreters = new List<ISpatialInterpreter>
            {
                new Interpreters.ConflictInterpreter(),
                new Interpreters.AbandonmentInterpreter(),
                new Interpreters.OccupationInterpreter(),
                new Interpreters.RitualInterpreter(),
                new Interpreters.CollapseInterpreter(),
                new Interpreters.CorruptionInterpreter(),
                new Interpreters.FloodInterpreter(),
                new Interpreters.DesperationInterpreter()
            };
        }

        public void AddInterpreter(ISpatialInterpreter interpreter)
        {
            _interpreters.Add(interpreter);
            _interpreters.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public void Execute(GenerationContext context)
        {
            var worldState = context.GetCustomData<NarrativeWorldState>("narrative_world_state");
            if (worldState == null || context.SpatialMap == null) return;

            var rng = context.Random.Fork("spatial_interpretation");
            var allResults = new Dictionary<int, InterpretationResult>();

            foreach (var room in context.SpatialMap.Rooms)
            {
                var roomState = worldState.GetRoomState(room.Id);
                var result = new InterpretationResult { RoomId = room.Id };

                foreach (var interpreter in _interpreters.OrderBy(i => i.Priority))
                {
                    if (interpreter.CanInterpret(roomState, room))
                    {
                        interpreter.Interpret(roomState, room, context, result,
                            rng.Fork($"{interpreter.InterpreterName}_{room.Id}"));
                    }
                }

                allResults[room.Id] = result;
            }

            // Apply results to context
            ApplyResults(allResults, context);

            // Store results for debugging
            context.SetCustomData("interpretation_results", allResults);

            int markerCount = allResults.Values.Sum(r => r.Markers.Count);
            int modCount = allResults.Values.Sum(r => r.Modifications.Count);
            UnityEngine.Debug.Log($"[Narrative] Spatial interpretation: {markerCount} markers, {modCount} modifications across {allResults.Count} rooms");
        }

        private void ApplyResults(Dictionary<int, InterpretationResult> results, GenerationContext context)
        {
            foreach (var kvp in results)
            {
                var result = kvp.Value;

                // Add markers
                context.StoryMarkers.AddRange(result.Markers);

                // Add decorations
                context.Decorations.AddRange(result.Decorations);

                // Apply spatial modifications
                foreach (var mod in result.Modifications)
                {
                    ApplyModification(mod, context);
                }
            }
        }

        private void ApplyModification(SpatialModification mod, GenerationContext context)
        {
            var tile = context.SpatialMap.GetTile(mod.Position);
            if (tile == null) return;

            switch (mod.Type)
            {
                case SpatialModificationType.BlockPassage:
                    tile.Type = TileType.Rubble;
                    tile.DamageLevel = mod.Intensity;
                    break;
                case SpatialModificationType.CreateRubble:
                    tile.Tags.Add("rubble");
                    tile.DamageLevel = System.Math.Max(tile.DamageLevel, mod.Intensity);
                    break;
                case SpatialModificationType.DamageWall:
                    tile.Tags.Add("damaged_wall");
                    tile.DamageLevel += mod.Intensity;
                    break;
                case SpatialModificationType.FloodTile:
                    tile.Type = TileType.Water;
                    tile.Tags.Add("flooded");
                    break;
                case SpatialModificationType.CorruptTile:
                    tile.Tags.Add("corrupted");
                    tile.BiomeTag = "corruption";
                    break;
                case SpatialModificationType.BurnTile:
                    tile.Tags.Add("burned");
                    tile.DamageLevel += mod.Intensity;
                    break;
                case SpatialModificationType.CollapseFloor:
                    tile.Type = TileType.Pit;
                    tile.DamageLevel = 1f;
                    break;
                case SpatialModificationType.AddBarricade:
                    tile.Tags.Add("barricade");
                    break;
                case SpatialModificationType.SealDoor:
                    tile.Tags.Add("sealed");
                    break;
                case SpatialModificationType.AddFungalGrowth:
                    tile.Tags.Add("fungal_growth");
                    break;
            }
        }
    }
}

