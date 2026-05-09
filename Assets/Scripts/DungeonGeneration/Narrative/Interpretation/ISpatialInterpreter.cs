using System.Collections.Generic;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using DungeonGeneration.Narrative.WorldState;

namespace DungeonGeneration.Narrative.Interpretation
{
    /// <summary>
    /// Interface for spatial interpreters that convert abstract simulation states
    /// into concrete spatial meaning (props, markers, modifications).
    /// Each interpreter handles a specific category of narrative consequence.
    /// </summary>
    public interface ISpatialInterpreter
    {
        string InterpreterName { get; }
        int Priority { get; }

        /// <summary>
        /// Returns true if this interpreter should process the given room.
        /// </summary>
        bool CanInterpret(RoomNarrativeState roomState, RoomInstance room);

        /// <summary>
        /// Converts narrative state into environmental storytelling results.
        /// </summary>
        void Interpret(RoomNarrativeState roomState, RoomInstance room,
            GenerationContext context, InterpretationResult result, SeededRandom rng);
    }

    /// <summary>
    /// Collects all spatial interpretation results for a single room.
    /// Multiple interpreters can contribute to the same room.
    /// </summary>
    public class InterpretationResult
    {
        public int RoomId;
        public List<StoryMarker> Markers { get; } = new List<StoryMarker>();
        public List<DecorationInstance> Decorations { get; } = new List<DecorationInstance>();
        public List<SpatialModification> Modifications { get; } = new List<SpatialModification>();
        public List<AtmosphereOverlay> Atmospheres { get; } = new List<AtmosphereOverlay>();
        public float ReadabilityScore = 1f;
    }

    /// <summary>
    /// Represents a spatial modification to room geometry or tiles.
    /// </summary>
    [System.Serializable]
    public class SpatialModification
    {
        public SpatialModificationType Type;
        public UnityEngine.Vector2Int Position;
        public string Description;
        public float Intensity;
        public Dictionary<string, string> Data = new Dictionary<string, string>();
    }

    public enum SpatialModificationType
    {
        BlockPassage,
        CreateRubble,
        DamageWall,
        FloodTile,
        CorruptTile,
        BurnTile,
        CollapseFloor,
        AddBarricade,
        CreateHiddenPath,
        SealDoor,
        BreakDoor,
        AddLighting,
        RemoveLighting,
        AddWebbing,
        AddFungalGrowth
    }

    /// <summary>
    /// Represents an atmospheric overlay applied to a room.
    /// </summary>
    [System.Serializable]
    public class AtmosphereOverlay
    {
        public string AtmosphereType;
        public float Intensity;
        public UnityEngine.Color TintColor = UnityEngine.Color.white;
        public string Description;
    }
}

