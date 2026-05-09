using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration.Narrative.Authoring
{
    /// <summary>
    /// ScriptableObject defining a narrative event in the dungeon's history timeline.
    /// Events can be mandatory story beats, optional occurrences, or constrained outcomes.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNarrativeEvent", menuName = "Dungeon Generation/Narrative/Narrative Event")]
    public class NarrativeEventDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string eventId;
        public string displayName;
        [TextArea(2, 5)] public string description;

        [Header("Timeline")]
        public EventTiming timing;
        [Tooltip("Simulation step when this event occurs (if timing is FixedStep)")]
        public int fixedStep = -1;
        [Tooltip("Earliest possible step (if timing is Range)")]
        public int earliestStep = 0;
        [Tooltip("Latest possible step (if timing is Range)")]
        public int latestStep = 10;

        [Header("Preconditions")]
        public List<EventPrecondition> preconditions = new List<EventPrecondition>();

        [Header("Participants")]
        public List<FactionDefinition> involvedFactions = new List<FactionDefinition>();
        public List<CharacterArchetype> involvedCharacters = new List<CharacterArchetype>();

        [Header("Effects")]
        public List<EventEffect> effects = new List<EventEffect>();

        [Header("Constraints")]
        public bool isMandatory = false;
        public bool isProtectedOutcome = false;
        [TextArea(1, 2)] public string protectedOutcomeDescription;

        [Header("Cascading")]
        public List<NarrativeEventDefinition> triggeredEvents = new List<NarrativeEventDefinition>();
        [Range(0f, 1f)] public float cascadeProbability = 1f;

        [Header("Environmental Impact")]
        public EventSeverity severity = EventSeverity.Minor;
        public List<string> environmentalTags = new List<string>();
        [TextArea(1, 3)] public string spatialConsequenceHint;
    }

    public enum EventTiming
    {
        FixedStep,
        Range,
        Triggered,
        Conditional,
        AsSoonAsPossible
    }

    public enum EventSeverity
    {
        Trivial,
        Minor,
        Moderate,
        Major,
        Catastrophic
    }

    [System.Serializable]
    public class EventPrecondition
    {
        public PreconditionType type;
        public string targetId;
        public string parameterKey;
        public string expectedValue;
        [Range(0f, 1f)] public float threshold = 0.5f;
    }

    public enum PreconditionType
    {
        FactionControlsRoom,
        FactionExists,
        ResourceBelow,
        ResourceAbove,
        EventOccurred,
        EventNotOccurred,
        StepReached,
        DangerAbove,
        TerritorySize,
        DispositionBelow,
        CharacterAlive,
        CharacterDead
    }

    [System.Serializable]
    public class EventEffect
    {
        public EventEffectType type;
        public string targetId;
        public string parameterKey;
        public string value;
        [Range(-1f, 1f)] public float magnitude = 0f;
    }

    public enum EventEffectType
    {
        ClaimTerritory,
        AbandonTerritory,
        DestroyRoom,
        DamageRoom,
        CorruptRoom,
        FloodRoom,
        CreateBarricade,
        BlockPassage,
        OpenPassage,
        KillCharacter,
        DisplaceFaction,
        ChangeMorale,
        ConsumeResource,
        SpawnResource,
        ChangeDisposition,
        AddEnvironmentalTag,
        TriggerMigration,
        CollapseArea,
        CreateRitualSite,
        EstablishDefense,
        SpreadCorruption,
        ContaminateArea
    }
}

