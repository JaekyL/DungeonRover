using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration.Data
{
    [System.Serializable]
    public class EncounterInstance
    {
        public int RoomId;
        public EncounterType Type;
        public float Difficulty;
        public string FactionId;
        public List<SpawnPoint> SpawnPoints = new List<SpawnPoint>();
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
    }
    [System.Serializable]
    public class SpawnPoint
    {
        public Vector2Int Position;
        public string EnemyTypeId;
        public string BehaviorOverride;
    }
}
