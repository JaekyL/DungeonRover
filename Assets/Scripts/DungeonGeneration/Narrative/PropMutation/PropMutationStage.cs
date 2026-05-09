using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using DungeonGeneration.Narrative.WorldState;
using UnityEngine;

namespace DungeonGeneration.Narrative.PropMutation
{
    /// <summary>
    /// Interface for prop mutators that modify existing decorations based on
    /// narrative state. Enables layered storytelling on the same props.
    /// </summary>
    public interface IPropMutator
    {
        string MutatorName { get; }
        int Priority { get; }
        bool CanMutate(DecorationInstance decoration, RoomNarrativeState roomState);
        void Mutate(DecorationInstance decoration, RoomNarrativeState roomState, SeededRandom rng);
    }

    /// <summary>
    /// Layer 7: Prop & Decoration Mutation Stage.
    /// Extends existing decoration systems by mutating props based on narrative state.
    /// Supports layered storytelling: the same room can be an old library → later barricaded
    /// → partially flooded → later occupied by cult.
    /// Priority 705: runs after existing Decoration stage (700).
    /// </summary>
    public class PropMutationStage : IGenerationStage
    {
        public string StageName => "Narrative Prop Mutation";
        public int Priority => 705;

        private readonly List<IPropMutator> _mutators;

        public PropMutationStage()
        {
            _mutators = new List<IPropMutator>
            {
                new DamageMutator(),
                new WaterDamageMutator(),
                new FireDamageMutator(),
                new CorruptionMutator(),
                new AgingMutator(),
                new LootingMutator()
            };
        }

        public void AddMutator(IPropMutator mutator) => _mutators.Add(mutator);

        public void Execute(GenerationContext context)
        {
            var worldState = context.GetCustomData<NarrativeWorldState>("narrative_world_state");
            if (worldState == null) return;

            var rng = context.Random.Fork("prop_mutation");
            int mutationCount = 0;

            foreach (var decoration in context.Decorations)
            {
                var roomState = worldState.GetRoomState(decoration.RoomId);

                foreach (var mutator in _mutators.OrderBy(m => m.Priority))
                {
                    if (mutator.CanMutate(decoration, roomState))
                    {
                        mutator.Mutate(decoration, roomState, rng.Fork($"{mutator.MutatorName}_{decoration.RoomId}"));
                        mutationCount++;
                    }
                }
            }

            UnityEngine.Debug.Log($"[Narrative] Prop mutation: {mutationCount} mutations applied");
        }
    }

    // --- Mutator Implementations ---

    /// <summary>
    /// Applies damage variants to props in structurally damaged rooms.
    /// Example: "bookshelf" → "bookshelf_damaged"
    /// </summary>
    public class DamageMutator : IPropMutator
    {
        public string MutatorName => "Damage";
        public int Priority => 10;

        public bool CanMutate(DecorationInstance decoration, RoomNarrativeState roomState)
        {
            return roomState.StructuralDamage > 0.3f;
        }

        public void Mutate(DecorationInstance decoration, RoomNarrativeState roomState, SeededRandom rng)
        {
            if (rng.NextBool(roomState.StructuralDamage))
            {
                decoration.DecorationId += "_damaged";
                // Tilt slightly to show disrepair
                float tilt = rng.NextFloat(-10f, 10f) * roomState.StructuralDamage;
                decoration.Rotation += tilt;
            }
        }
    }

    /// <summary>
    /// Applies water damage to props in flooded rooms.
    /// Example: "desk" → "desk_waterlogged"
    /// </summary>
    public class WaterDamageMutator : IPropMutator
    {
        public string MutatorName => "WaterDamage";
        public int Priority => 20;

        public bool CanMutate(DecorationInstance decoration, RoomNarrativeState roomState)
        {
            return roomState.IsFlooded || roomState.WaterLevel > 0.2f;
        }

        public void Mutate(DecorationInstance decoration, RoomNarrativeState roomState, SeededRandom rng)
        {
            if (rng.NextBool(roomState.WaterLevel))
            {
                decoration.DecorationId += "_waterlogged";
            }
        }
    }

    /// <summary>
    /// Applies fire damage to props in burned rooms.
    /// Example: "chest" → "chest_burned"
    /// </summary>
    public class FireDamageMutator : IPropMutator
    {
        public string MutatorName => "FireDamage";
        public int Priority => 30;

        public bool CanMutate(DecorationInstance decoration, RoomNarrativeState roomState)
        {
            return roomState.FireDamage > 0.2f;
        }

        public void Mutate(DecorationInstance decoration, RoomNarrativeState roomState, SeededRandom rng)
        {
            if (rng.NextBool(roomState.FireDamage))
            {
                decoration.DecorationId += "_burned";
                // Shrink slightly (burned away)
                float shrink = 1f - roomState.FireDamage * 0.2f;
                decoration.Scale *= shrink;
            }
        }
    }

    /// <summary>
    /// Applies corruption visual changes to props in corrupted rooms.
    /// Example: "statue" → "statue_corrupted"
    /// </summary>
    public class CorruptionMutator : IPropMutator
    {
        public string MutatorName => "Corruption";
        public int Priority => 40;

        public bool CanMutate(DecorationInstance decoration, RoomNarrativeState roomState)
        {
            return roomState.Corruption > 0.3f;
        }

        public void Mutate(DecorationInstance decoration, RoomNarrativeState roomState, SeededRandom rng)
        {
            if (rng.NextBool(roomState.Corruption))
            {
                decoration.DecorationId += "_corrupted";
                // Slightly distort scale for unnatural look
                float distort = rng.NextFloat(0.9f, 1.1f);
                decoration.Scale = new Vector3(
                    decoration.Scale.x * distort,
                    decoration.Scale.y * (2f - distort),
                    decoration.Scale.z * distort
                );
            }
        }
    }

    /// <summary>
    /// Applies aging/decay to props in long-abandoned rooms.
    /// Example: "chair" → "chair_decayed"
    /// </summary>
    public class AgingMutator : IPropMutator
    {
        public string MutatorName => "Aging";
        public int Priority => 50;

        public bool CanMutate(DecorationInstance decoration, RoomNarrativeState roomState)
        {
            return roomState.Decay > 0.3f;
        }

        public void Mutate(DecorationInstance decoration, RoomNarrativeState roomState, SeededRandom rng)
        {
            if (rng.NextBool(roomState.Decay * 0.6f))
            {
                decoration.DecorationId += "_decayed";
            }
        }
    }

    /// <summary>
    /// Applies looting effects to props in rooms that changed ownership.
    /// Example: "locked_chest" → "chest_broken_open"
    /// </summary>
    public class LootingMutator : IPropMutator
    {
        public string MutatorName => "Looting";
        public int Priority => 60;

        private static readonly HashSet<string> LootableProps = new HashSet<string>
        {
            "chest", "locked_chest", "gold_pile", "gem_cluster", "safe",
            "display_case", "supply_crate", "pantry"
        };

        public bool CanMutate(DecorationInstance decoration, RoomNarrativeState roomState)
        {
            return roomState.OwnershipHistory.Count >= 2 &&
                   LootableProps.Any(p => decoration.DecorationId.Contains(p));
        }

        public void Mutate(DecorationInstance decoration, RoomNarrativeState roomState, SeededRandom rng)
        {
            if (rng.NextBool(0.6f))
            {
                decoration.DecorationId += "_looted";
                // Open/broken state
                decoration.Rotation += rng.NextFloat(-20f, 20f);
            }
        }
    }
}

