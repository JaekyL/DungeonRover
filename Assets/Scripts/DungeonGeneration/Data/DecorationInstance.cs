using UnityEngine;

namespace DungeonGeneration.Data
{
    [System.Serializable]
    public class DecorationInstance
    {
        public Vector2Int Position;
        public string DecorationId;
        public int RoomId;
        public float Rotation;
        public Vector3 Scale = Vector3.one;
        public bool IsMirrored;
        public string Category;
    }
}
