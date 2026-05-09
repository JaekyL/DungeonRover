using System.Collections.Generic;
using DungeonGeneration.Data;
using UnityEngine;

namespace DungeonGeneration.Core
{
    [CreateAssetMenu(fileName = "DungeonConfig", menuName = "Dungeon Generation/Dungeon Config")]
    public class DungeonConfig : ScriptableObject
    {
        [Header("General")]
        public string dungeonName = "Unnamed Dungeon";
        public Vector2Int dungeonSize = new Vector2Int(100, 100);
        public int floorCount = 1;
        [Header("Macro Graph")]
        public int minRooms = 8;
        public int maxRooms = 15;
        public int criticalPathLength = 5;
        [Range(0f, 1f)] public float branchProbability = 0.4f;
        [Range(0f, 1f)] public float loopProbability = 0.2f;
        [Range(0f, 1f)] public float secretRoomProbability = 0.15f;
        public int lockKeyPairs = 2;
        public bool placeBoss = true;
        [Header("Spatial Layout")]
        public LayoutAlgorithm primaryAlgorithm = LayoutAlgorithm.BSP;
        public Vector2Int minRoomSize = new Vector2Int(5, 5);
        public Vector2Int maxRoomSize = new Vector2Int(15, 15);
        public int corridorWidth = 2;
        public int roomPadding = 1;
        [Header("History Simulation")]
        public int historySteps = 10;
        public List<HistoryAgentConfig> historyAgents = new List<HistoryAgentConfig>();
        [Header("Decoration")]
        [Range(0f, 1f)] public float decorationDensity = 0.5f;
        public List<RoomPurposeDefinition> roomPurposeDefinitions = new List<RoomPurposeDefinition>();
        [Header("Encounters")]
        [Range(0f, 1f)] public float encounterDensity = 0.4f;
        public AnimationCurve difficultyEscalation = AnimationCurve.Linear(0, 0.2f, 1, 1f);
        [Header("Performance")]
        public bool useChunkedGeneration = false;
        public Vector2Int chunkSize = new Vector2Int(32, 32);
    }
    [System.Serializable]
    public class HistoryAgentConfig
    {
        public HistoryAgentType agentType;
        [Range(0f, 1f)] public float intensity = 0.5f;
        public int priority = 0;
    }
    [System.Serializable]
    public class RoomPurposeDefinition
    {
        public RoomPurposeType purposeType;
        public float weight = 1f;
        public int minImportance = 0;
        public List<string> requiredTags = new List<string>();
        public List<string> decorationCategories = new List<string>();
    }
}
