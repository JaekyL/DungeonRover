using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration.Narrative.Authoring
{
    /// <summary>
    /// ScriptableObject defining a character archetype participating in the dungeon's history.
    /// Characters belong to factions and carry motivations that influence simulation outcomes.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Dungeon Generation/Narrative/Character Archetype")]
    public class CharacterArchetype : ScriptableObject
    {
        [Header("Identity")]
        public string characterId;
        public string displayName;
        [TextArea(2, 4)] public string backstory;

        [Header("Affiliation")]
        public FactionDefinition faction;
        public CharacterRole role;

        [Header("Motivations")]
        public List<MotivationEntry> personalMotivations = new List<MotivationEntry>();

        [Header("Personality")]
        [Range(0f, 1f)] public float bravery = 0.5f;
        [Range(0f, 1f)] public float loyalty = 0.5f;
        [Range(0f, 1f)] public float cruelty = 0.0f;
        [Range(0f, 1f)] public float wisdom = 0.5f;
        [Range(0f, 1f)] public float desperation = 0.0f;

        [Header("Capabilities")]
        public List<string> skills = new List<string>();
        public bool canLeadGroup = false;
        public bool isEssentialToFaction = false;

        [Header("Death / Removal")]
        public bool canDie = true;
        public bool isProtected = false;
        [TextArea(1, 2)] public string deathScenario;

        [Header("Environmental Evidence")]
        [TextArea(1, 3)] public string personalItemDescription;
        public List<string> evidenceTags = new List<string>();
    }

    public enum CharacterRole
    {
        Leader,
        Follower,
        Scout,
        Guard,
        Scholar,
        Priest,
        Miner,
        Soldier,
        Spy,
        Healer,
        Outcast,
        Merchant,
        Prisoner,
        Survivor
    }
}

