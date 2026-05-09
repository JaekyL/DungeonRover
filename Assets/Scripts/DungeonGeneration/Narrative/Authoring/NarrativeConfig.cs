using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration.Narrative.Authoring
{
    /// <summary>
    /// Master ScriptableObject that defines an entire narrative scenario for a dungeon.
    /// Combines factions, characters, timeline events, constraints, and tuning parameters
    /// into a single authored configuration that drives the narrative simulation.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNarrativeConfig", menuName = "Dungeon Generation/Narrative/Narrative Config")]
    public class NarrativeConfig : ScriptableObject
    {
        [Header("Scenario")]
        public string scenarioName = "Unnamed Scenario";
        [TextArea(3, 6)] public string scenarioDescription;

        [Header("Factions")]
        public List<FactionDefinition> factions = new List<FactionDefinition>();

        [Header("Characters")]
        public List<CharacterArchetype> characters = new List<CharacterArchetype>();

        [Header("Timeline")]
        public int simulationSteps = 20;
        public List<NarrativeEventDefinition> timelineEvents = new List<NarrativeEventDefinition>();

        [Header("Constraints")]
        public List<NarrativeConstraint> constraints = new List<NarrativeConstraint>();

        [Header("Resources")]
        public List<WorldResource> worldResources = new List<WorldResource>();

        [Header("Simulation Tuning")]
        [Range(0f, 1f)] public float conflictEscalationRate = 0.3f;
        [Range(0f, 1f)] public float decayRate = 0.1f;
        [Range(0f, 1f)] public float migrationFrequency = 0.2f;
        [Range(0f, 2f)] public float consequenceAmplification = 1.0f;
        [Range(0f, 1f)] public float chaosThreshold = 0.8f;

        [Header("Storytelling Tuning")]
        [Range(0f, 2f)] public float storytellingDensity = 1.0f;
        [Range(0f, 1f)] public float subtlety = 0.3f;
        [Range(0f, 1f)] public float exaggeration = 0.6f;
        public bool preferDramaticReadability = true;

        [Header("Protected Outcomes")]
        public List<ProtectedOutcome> protectedOutcomes = new List<ProtectedOutcome>();
    }

    [System.Serializable]
    public class NarrativeConstraint
    {
        public ConstraintType type;
        [TextArea(1, 2)] public string description;
        public string targetFactionId;
        public string targetRoomTag;
        public string parameterKey;
        public string parameterValue;
        public bool isHard = true;
    }

    public enum ConstraintType
    {
        FactionMustSurvive,
        FactionMustFall,
        RoomMustBeOccupied,
        RoomMustBeAbandoned,
        EventMustOccur,
        EventMustNotOccur,
        TerritoryMinimum,
        TerritoryMaximum,
        ConflictMustResolve,
        CharacterMustSurvive,
        CorruptionMustSpread,
        CorruptionMustBeContained
    }

    [System.Serializable]
    public class WorldResource
    {
        public string resourceId;
        public string displayName;
        public float initialSupply = 100f;
        public float regenerationRate = 0f;
        public bool isFinite = true;
        public bool isContested = false;
    }

    [System.Serializable]
    public class ProtectedOutcome
    {
        [TextArea(1, 2)] public string description;
        public string factionId;
        public ProtectedOutcomeType type;
        public string targetRoomTag;
        public int lockAtStep = -1;
    }

    public enum ProtectedOutcomeType
    {
        FactionSurvives,
        FactionControlsArea,
        EventHappens,
        AreaRemainsSafe,
        AreaBecomesCorrupted,
        CharacterSurvives,
        BarricadeExists,
        RitualCompletes
    }
}

