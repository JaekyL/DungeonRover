using UnityEngine;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using Debug = UnityEngine.Debug;

namespace DungeonGeneration.Rendering
{
    /// <summary>
    /// Generates simple primitive-based dungeon visuals without requiring any prefab assets.
    /// Useful for prototyping and testing. Uses colored cubes/quads directly.
    /// Attach alongside DungeonGenerator for instant visual results.
    /// </summary>
    [RequireComponent(typeof(DungeonGenerator))]
    public class DungeonProceduralRenderer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _tileSize = 1f;
        [SerializeField] private float _wallHeight = 2f;
        [SerializeField] private bool _renderOnGenerate = true;

        [Header("Floor Colors")]
        [SerializeField] private Color _floorColor = new Color(0.55f, 0.55f, 0.45f);
        [SerializeField] private Color _corridorColor = new Color(0.45f, 0.45f, 0.4f);
        [SerializeField] private Color _waterColor = new Color(0.2f, 0.35f, 0.75f, 0.8f);
        [SerializeField] private Color _rubbleColor = new Color(0.5f, 0.35f, 0.25f);

        [Header("Wall Colors")]
        [SerializeField] private Color _wallColor = new Color(0.3f, 0.3f, 0.3f);

        [Header("Feature Colors")]
        [SerializeField] private Color _doorColor = new Color(0.6f, 0.4f, 0.15f);
        [SerializeField] private Color _lockedDoorColor = new Color(0.7f, 0.15f, 0.15f);
        [SerializeField] private Color _secretDoorColor = new Color(0.5f, 0.2f, 0.7f);

        [Header("Rendering Layers")]
        [SerializeField] private bool _renderFloors = true;
        [SerializeField] private bool _renderWalls = true;
        [SerializeField] private bool _renderDoors = true;
        [SerializeField] private bool _renderStoryMarkerProps = true;

        private DungeonGenerator _generator;
        private Transform _dungeonRoot;

        // Cached materials
        private Material _floorMat;
        private Material _corridorMat;
        private Material _wallMat;
        private Material _waterMat;
        private Material _rubbleMat;
        private Material _doorMat;
        private Material _lockedDoorMat;
        private Material _secretDoorMat;

        private void Awake()
        {
            _generator = GetComponent<DungeonGenerator>();
        }

        [ContextMenu("Render Dungeon (Procedural)")]
        public void Render()
        {
            var ctx = _generator.LastContext;
            if (ctx == null)
            {
                UnityEngine.Debug.LogWarning("[ProceduralRenderer] No context. Generate first.");
                return;
            }
            Render(ctx);
        }

        public void Render(GenerationContext context)
        {
            Clear();
            CreateMaterials();

            _dungeonRoot = new GameObject("DungeonRoot_Procedural").transform;
            _dungeonRoot.SetParent(transform);
            _dungeonRoot.localPosition = Vector3.zero;

            var map = context.SpatialMap;
            if (map == null) return;

            var floorParent = CreateChild("Floors");
            var wallParent = CreateChild("Walls");
            var doorParent = CreateChild("Doors");
            var markerParent = CreateChild("StoryMarkers");

            // ── Floors ──
            if (_renderFloors)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    for (int y = 0; y < map.Height; y++)
                    {
                        var tile = map.GetTile(x, y);
                        if (tile.Type == TileType.Wall) continue;

                        Material mat = GetFloorMaterial(tile);
                        var floor = CreatePrimitive(PrimitiveType.Cube, floorParent,
                            $"Floor_{x}_{y}",
                            new Vector3(x * _tileSize, -0.05f, y * _tileSize),
                            new Vector3(_tileSize, 0.1f, _tileSize),
                            mat);

                        // Slight Y offset for damaged tiles
                        if (tile.DamageLevel > 0.3f)
                        {
                            var p = floor.transform.position;
                            p.y -= tile.DamageLevel * 0.08f;
                            floor.transform.position = p;
                        }

                        floor.isStatic = true;
                    }
                }
            }

            // ── Walls ──
            if (_renderWalls)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    for (int y = 0; y < map.Height; y++)
                    {
                        var tile = map.GetTile(x, y);
                        if (tile.Type != TileType.Wall) continue;
                        if (!IsAdjacentToNonWall(map, x, y)) continue;

                        var wall = CreatePrimitive(PrimitiveType.Cube, wallParent,
                            $"Wall_{x}_{y}",
                            new Vector3(x * _tileSize, _wallHeight * 0.5f, y * _tileSize),
                            new Vector3(_tileSize, _wallHeight, _tileSize),
                            _wallMat);

                        wall.isStatic = true;
                    }
                }
            }

            // ── Doors ──
            if (_renderDoors)
            {
                foreach (var door in map.Doors)
                {
                    Material dmat;
                    switch (door.Type)
                    {
                        case DoorType.Locked:
                        case DoorType.Boss:
                            dmat = _lockedDoorMat;
                            break;
                        case DoorType.Secret:
                            dmat = _secretDoorMat;
                            break;
                        default:
                            dmat = _doorMat;
                            break;
                    }

                    // Determine orientation
                    bool wallEW = IsWallAt(map, door.Position.x + 1, door.Position.y) ||
                                  IsWallAt(map, door.Position.x - 1, door.Position.y);

                    Vector3 doorScale = wallEW
                        ? new Vector3(_tileSize * 0.2f, _wallHeight * 0.8f, _tileSize * 0.9f)
                        : new Vector3(_tileSize * 0.9f, _wallHeight * 0.8f, _tileSize * 0.2f);

                    CreatePrimitive(PrimitiveType.Cube, doorParent,
                        $"Door_{door.Type}_{door.Position.x}_{door.Position.y}",
                        new Vector3(door.Position.x * _tileSize, _wallHeight * 0.4f, door.Position.y * _tileSize),
                        doorScale,
                        dmat);
                }
            }

            // ── Story Markers (simple colored cubes) ──
            if (_renderStoryMarkerProps)
            {
                foreach (var marker in context.StoryMarkers)
                {
                    Color markerColor = GetMarkerColor(marker.Type);
                    var mat = CreateUnlitMaterial(markerColor);

                    float size = Mathf.Lerp(0.15f, 0.35f, marker.Intensity) * _tileSize;

                    CreatePrimitive(PrimitiveType.Cube, markerParent,
                        $"Marker_{marker.Type}",
                        new Vector3(marker.Position.x * _tileSize, size * 0.5f, marker.Position.y * _tileSize),
                        Vector3.one * size,
                        mat);
                }
            }

            UnityEngine.Debug.Log($"[ProceduralRenderer] Rendered dungeon with {map.Rooms.Count} rooms.");
        }

        [ContextMenu("Clear Dungeon")]
        public void Clear()
        {
            if (_dungeonRoot != null)
            {
                if (Application.isPlaying)
                    Destroy(_dungeonRoot.gameObject);
                else
                    DestroyImmediate(_dungeonRoot.gameObject);
            }
            _dungeonRoot = null;
        }

        // ─── Helpers ───

        private Transform CreateChild(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_dungeonRoot);
            go.transform.localPosition = Vector3.zero;
            return go.transform;
        }

        private GameObject CreatePrimitive(PrimitiveType type, Transform parent,
            string name, Vector3 position, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;

            // Remove collider to improve performance (thousands of tiles)
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            if (mat != null)
            {
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = mat;
            }

            return go;
        }

        private bool IsAdjacentToNonWall(SpatialMap map, int x, int y)
        {
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i], ny = y + dy[i];
                if (map.InBounds(nx, ny) && map.GetTile(nx, ny).Type != TileType.Wall)
                    return true;
            }
            return false;
        }

        private bool IsWallAt(SpatialMap map, int x, int y)
        {
            return !map.InBounds(x, y) || map.GetTile(x, y).Type == TileType.Wall;
        }

        private Material GetFloorMaterial(TileData tile)
        {
            // Biome override
            if (tile.BiomeTag == "corrupted")
                return CreateUnlitMaterial(new Color(0.3f, 0.15f, 0.4f));

            switch (tile.Type)
            {
                case TileType.Water: return _waterMat;
                case TileType.Corridor: return _corridorMat;
                case TileType.Rubble: return _rubbleMat;
                default: return _floorMat;
            }
        }

        private Color GetMarkerColor(StoryMarkerType type)
        {
            switch (type)
            {
                case StoryMarkerType.BloodTrail: return new Color(0.6f, 0.05f, 0.05f);
                case StoryMarkerType.Skeleton: return Color.white;
                case StoryMarkerType.BurnMarks: return new Color(0.15f, 0.1f, 0.05f);
                case StoryMarkerType.RitualMarking: return new Color(0.5f, 0.0f, 0.5f);
                case StoryMarkerType.WaterDamage: return new Color(0.3f, 0.5f, 0.7f);
                case StoryMarkerType.FungalGrowth: return new Color(0.15f, 0.5f, 0.15f);
                case StoryMarkerType.Barricade: return new Color(0.5f, 0.35f, 0.15f);
                case StoryMarkerType.WeaponScatter: return new Color(0.6f, 0.6f, 0.6f);
                case StoryMarkerType.CollapsedWall: return new Color(0.4f, 0.35f, 0.3f);
                case StoryMarkerType.Note: return new Color(0.9f, 0.85f, 0.6f);
                case StoryMarkerType.Campfire: return new Color(1f, 0.5f, 0.1f);
                case StoryMarkerType.Decay: return new Color(0.35f, 0.3f, 0.2f);
                case StoryMarkerType.LootRemains: return new Color(0.8f, 0.7f, 0.2f);
                default: return Color.gray;
            }
        }

        // ─── Material Creation ───

        private void CreateMaterials()
        {
            _floorMat = CreateUnlitMaterial(_floorColor);
            _corridorMat = CreateUnlitMaterial(_corridorColor);
            _wallMat = CreateUnlitMaterial(_wallColor);
            _waterMat = CreateUnlitMaterial(_waterColor);
            _rubbleMat = CreateUnlitMaterial(_rubbleColor);
            _doorMat = CreateUnlitMaterial(_doorColor);
            _lockedDoorMat = CreateUnlitMaterial(_lockedDoorColor);
            _secretDoorMat = CreateUnlitMaterial(_secretDoorColor);
        }

        private Material CreateUnlitMaterial(Color color)
        {
            // Try URP Lit shader first, fall back to Standard
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.color = color;

            if (color.a < 1f)
            {
                // Enable transparency
                mat.SetFloat("_Surface", 1); // URP transparent
                mat.SetFloat("_Mode", 3); // Standard transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            return mat;
        }
    }
}


