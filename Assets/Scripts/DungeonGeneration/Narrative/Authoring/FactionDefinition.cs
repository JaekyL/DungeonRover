using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration.Narrative.Authoring
{
    /// <summary>
    /// ScriptableObject defining a faction's identity, motivations, and behavior profile.
    /// Used by the narrative simulation to drive faction behavior within the dungeon.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFaction", menuName = "Dungeon Generation/Narrative/Faction Definition")]
    public class FactionDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string factionId;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Color debugColor = Color.white;

        [Header("Motivations")]
        public List<MotivationEntry> motivations = new List<MotivationEntry>();

        [Header("Behavior Profile")]
        public FactionBehaviorProfile behaviorProfile;

        [Header("Relationships")]
        public List<FactionRelationship> relationships = new List<FactionRelationship>();

        [Header("Resources")]
        public List<ResourceRequirement> resourceNeeds = new List<ResourceRequirement>();

        [Header("Territorial Behavior")]
        [Range(0f, 1f)] public float expansionDrive = 0.3f;
        [Range(0f, 1f)] public float defensiveness = 0.5f;
        [Range(1, 20)] public int preferredTerritorySize = 3;
        public List<string> preferredRoomTags = new List<string>();

        [Header("Desperation")]
        [Range(0f, 1f)] public float desperationThreshold = 0.7f;
        public List<DesperationBehavior> desperationBehaviors = new List<DesperationBehavior>();

        [Header("Environmental Footprint")]
        public List<string> environmentalTags = new List<string>();
        [TextArea(1, 2)] public string occupationStyle = "organized";
    }

    [System.Serializable]
    public class MotivationEntry
    {
        public MotivationType type;
        [Range(0f, 1f)] public float weight = 0.5f;
        [TextArea(1, 2)] public string description;
    }

    public enum MotivationType
    {
        Survival,
        Power,
        Knowledge,
        Faith,
        Wealth,
        Revenge,
        Protection,
        Conquest,
        Awakening,
        Containment,
        Escape,
        Corruption,
        Preservation,
        Exploitation
    }

    [System.Serializable]
    public class FactionBehaviorProfile
    {
        [Range(0f, 1f)] public float aggression = 0.5f;
        [Range(0f, 1f)] public float rationality = 0.7f;
        [Range(0f, 1f)] public float cooperativeness = 0.3f;
        [Range(0f, 1f)] public float resourcefulness = 0.5f;
        [Range(0f, 1f)] public float fanaticism = 0.0f;
        [Range(0f, 1f)] public float adaptability = 0.5f;
        public bool canBecomeIrrational = false;
    }

    [System.Serializable]
    public class FactionRelationship
    {
        public FactionDefinition targetFaction;
        public RelationshipType type;
        [Range(-1f, 1f)] public float initialDisposition = 0f;
        public bool isVolatile = false;
    }

    public enum RelationshipType
    {
        Neutral,
        Allied,
        Hostile,
        Wary,
        Subservient,
        Dominant,
        Trading,
        Competing
    }

    [System.Serializable]
    public class ResourceRequirement
    {
        public string resourceId;
        [Range(0f, 1f)] public float criticality = 0.5f;
        public float consumptionRate = 1f;
    }

    [System.Serializable]
    public class DesperationBehavior
    {
        public string behaviorId;
        [TextArea(1, 2)] public string description;
        [Range(0f, 1f)] public float triggerThreshold = 0.8f;
        public DesperationAction action;
    }

    public enum DesperationAction
    {
        Fortify,
        Flee,
        SacrificeMembers,
        RecklessAttack,
        SeekAlliance,
        PerformRitual,
        Cannibalize,
        Abandon,
        Barricade,
        LastStand
    }
}

