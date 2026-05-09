using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration.Data
{
    public class SpatialMap
    {
        public int Width { get; }
        public int Height { get; }
        public TileData[,] Tiles { get; }
        public List<RoomInstance> Rooms { get; } = new List<RoomInstance>();
        public List<CorridorInstance> Corridors { get; } = new List<CorridorInstance>();
        public List<DoorInstance> Doors { get; } = new List<DoorInstance>();
        public SpatialMap(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new TileData[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Tiles[x, y] = new TileData { Position = new Vector2Int(x, y), Type = TileType.Wall };
        }
        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        public bool InBounds(Vector2Int pos) => InBounds(pos.x, pos.y);
        public TileData GetTile(int x, int y) => InBounds(x, y) ? Tiles[x, y] : null;
        public TileData GetTile(Vector2Int pos) => GetTile(pos.x, pos.y);
        public void SetTile(int x, int y, TileType type)
        {
            if (InBounds(x, y)) Tiles[x, y].Type = type;
        }
        public void CarveRoom(RectInt bounds, int roomId)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    if (!InBounds(x, y)) continue;
                    Tiles[x, y].Type = TileType.Floor;
                    Tiles[x, y].RoomId = roomId;
                }
        }
        public List<Vector2Int> GetNeighbors(Vector2Int pos, bool includeDiagonals = false)
        {
            var result = new List<Vector2Int>();
            var offsets = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var o in offsets)
            {
                var p = pos + o;
                if (InBounds(p)) result.Add(p);
            }
            if (includeDiagonals)
            {
                var diag = new[] {
                    new Vector2Int(1, 1), new Vector2Int(-1, 1),
                    new Vector2Int(1, -1), new Vector2Int(-1, -1)
                };
                foreach (var o in diag)
                {
                    var p = pos + o;
                    if (InBounds(p)) result.Add(p);
                }
            }
            return result;
        }
    }
    [System.Serializable]
    public class TileData
    {
        public Vector2Int Position;
        public TileType Type;
        public int RoomId = -1;
        public int CorridorId = -1;
        public string BiomeTag;
        public float DamageLevel;
        public List<string> Tags = new List<string>();
    }
    [System.Serializable]
    public class RoomInstance
    {
        public int Id;
        public int GraphNodeId;
        public RectInt Bounds;
        public RoomPurposeType Purpose;
        public string FactionOwner;
        public List<Vector2Int> FloorTiles = new List<Vector2Int>();
        public List<Vector2Int> WallTiles = new List<Vector2Int>();
        public List<Vector2Int> EntryPoints = new List<Vector2Int>();
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
    }
    [System.Serializable]
    public class CorridorInstance
    {
        public int Id;
        public int FromRoomId;
        public int ToRoomId;
        public List<Vector2Int> Tiles = new List<Vector2Int>();
        public int Width = 1;
    }
    [System.Serializable]
    public class DoorInstance
    {
        public Vector2Int Position;
        public DoorType Type;
        public string RequiredKey;
        public int ConnectsRoomA;
        public int ConnectsRoomB;
    }
}
