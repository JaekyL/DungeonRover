using UnityEngine;

namespace DungeonGeneration.Data
{
    [System.Serializable]
    public class StoryMarker
    {
        public Vector2Int Position;
        public StoryMarkerType Type;
        public int RoomId;
        public string FactionSource;
        public string Description;
        public float Intensity;
    }
}
