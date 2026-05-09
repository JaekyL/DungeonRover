using System.Collections.Generic;
using UnityEngine;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using Debug = UnityEngine.Debug;

namespace DungeonGeneration.Rendering
{
    /// <summary>
    /// Renders a generated dungeon by instantiating prefabs from DungeonRenderConfig.
    /// Attach alongside DungeonGenerator. Automatically renders after generation.
    /// </summary>
    [RequireComponent(typeof(DungeonGenerator))]
    public class DungeonRenderer : MonoBehaviour
    {
        [Header("Render Config")]
        [SerializeField] private DungeonRenderConfig _renderConfig;

        [Header("Rendering")]
        [SerializeField] private bool _renderOnGenerate = true;
        [SerializeField] private bool _renderFloors = true;
        [SerializeField] private bool _renderWalls = true;
        [SerializeField] private bool _renderDoors = true;
        [SerializeField] private bool _renderStoryMarkers = true;
        [SerializeField] private bool _renderDecorations = true;
        [SerializeField] private bool _renderEncounters = false;

        [Header("Runtime")]
        [SerializeField] private bool _markStaticObjects = true;

        private DungeonGenerator _generator;
        private Transform _dungeonRoot;

        // Sub-containers for organization
        private Transform _floorContainer;
        private Transform _wallContainer;
        private Transform _doorContainer;
        private Transform _storyContainer;
        private Transform _decorContainer;
        private Transform _encounterContainer;

        public DungeonRenderConfig RenderConfig => _renderConfig;

        private void Awake()
        {
            _generator = GetComponent<DungeonGenerator>();
        }

        /// <summary>
        /// Clears all rendered dungeon objects and regenerates visuals from the current context.
        /// </summary>
        [ContextMenu("Render Dungeon")]
        public void Render()
        {
            var context = _generator.LastContext;
            if (context == null)
            {
                UnityEngine.Debug.LogWarning("[DungeonRenderer] No generation context available. Generate a dungeon first.");
                return;
            }

            Render(context);
        }

        /// <summary>
        /// Renders the dungeon from the given generation context.
        /// </summary>
        public void Render(GenerationContext context)
        {
            if (_renderConfig == null)
            {
                UnityEngine.Debug.LogError("[DungeonRenderer] No DungeonRenderConfig assigned!");
                return;
            }

            Clear();
            SetupContainers();

            var map = context.SpatialMap;
            if (map == null)
            {
                UnityEngine.Debug.LogError("[DungeonRenderer] No spatial map in context.");
                return;
            }

            float tileSize = _renderConfig.tileSize;

            if (_renderFloors) RenderFloors(map, tileSize);
            if (_renderWalls) RenderWalls(map, tileSize);
            if (_renderDoors) RenderDoors(map, tileSize);
            if (_renderStoryMarkers) RenderStoryMarkers(context, tileSize);
            if (_renderDecorations) RenderDecorations(context, tileSize);
            if (_renderEncounters) RenderEncounters(context, tileSize);

            if (_renderConfig.combineStaticMeshes)
                MarkStatic();

            UnityEngine.Debug.Log($"[DungeonRenderer] Dungeon rendered successfully.");
        }

        /// <summary>
        /// Removes all previously rendered dungeon objects.
        /// </summary>
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

        private void SetupContainers()
        {
            _dungeonRoot = new GameObject("DungeonRoot").transform;
            _dungeonRoot.SetParent(transform);
            _dungeonRoot.localPosition = Vector3.zero;

            _floorContainer = CreateContainer("Floors");
            _wallContainer = CreateContainer("Walls");
            _doorContainer = CreateContainer("Doors");
            _storyContainer = CreateContainer("StoryMarkers");
            _decorContainer = CreateContainer("Decorations");
            _encounterContainer = CreateContainer("Encounters");
        }

        private Transform CreateContainer(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_dungeonRoot);
            go.transform.localPosition = Vector3.zero;
            return go.transform;
        }

        // ─────────────────────────────────────────────
        //  FLOORS & SPECIAL TILES
        // ─────────────────────────────────────────────

        private void RenderFloors(SpatialMap map, float tileSize)
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(x, y);
                    if (tile.Type == TileType.Wall) continue; // Walls rendered separately

                    var prefab = _renderConfig.GetTilePrefab(tile.Type);
                    if (prefab == null) continue;

                    var pos = TileToWorld(x, y, 0f, tileSize);
                    var instance = Instantiate(prefab, pos, Quaternion.identity, _floorContainer);
                    instance.name = $"Floor_{x}_{y}";
                    instance.transform.localScale = Vector3.one * tileSize;

                    // Apply biome material override
                    if (!string.IsNullOrEmpty(tile.BiomeTag))
                    {
                        var mat = _renderConfig.GetBiomeMaterial(tile.BiomeTag);
                        if (mat != null)
                            ApplyMaterial(instance, mat);
                    }

                    // Apply water material
                    if (tile.Type == TileType.Water && _renderConfig.waterMaterial != null)
                        ApplyMaterial(instance, _renderConfig.waterMaterial);

                    // Damage visual variation
                    if (tile.DamageLevel > 0.3f)
                    {
                        // Slightly sink damaged tiles
                        var p = instance.transform.position;
                        p.y -= tile.DamageLevel * 0.1f * tileSize;
                        instance.transform.position = p;
                    }
                }
            }
        }

        // ─────────────────────────────────────────────
        //  WALLS
        // ─────────────────────────────────────────────

        private void RenderWalls(SpatialMap map, float tileSize)
        {
            if (_renderConfig.wallPrefab == null) return;

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(x, y);
                    if (tile.Type != TileType.Wall) continue;

                    // Only render walls adjacent to non-wall tiles (visible walls)
                    if (!IsAdjacentToNonWall(map, x, y)) continue;

                    var pos = TileToWorld(x, y, _renderConfig.wallHeight * 0.5f, tileSize);
                    var instance = Instantiate(_renderConfig.wallPrefab, pos, Quaternion.identity, _wallContainer);
                    instance.name = $"Wall_{x}_{y}";
                    instance.transform.localScale = new Vector3(tileSize, _renderConfig.wallHeight, tileSize);

                    // Apply wall material
                    if (_renderConfig.defaultWallMaterial != null)
                        ApplyMaterial(instance, _renderConfig.defaultWallMaterial);
                }
            }
        }

        private bool IsAdjacentToNonWall(SpatialMap map, int x, int y)
        {
            // Check 4-directional neighbors
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (map.InBounds(nx, ny) && map.GetTile(nx, ny).Type != TileType.Wall)
                    return true;
            }
            return false;
        }

        // ─────────────────────────────────────────────
        //  DOORS
        // ─────────────────────────────────────────────

        private void RenderDoors(SpatialMap map, float tileSize)
        {
            foreach (var door in map.Doors)
            {
                var prefab = _renderConfig.GetDoorPrefab(door.Type);
                if (prefab == null) continue;

                var pos = TileToWorld(door.Position.x, door.Position.y, 0f, tileSize);
                var instance = Instantiate(prefab, pos, Quaternion.identity, _doorContainer);
                instance.name = $"Door_{door.Position.x}_{door.Position.y}_{door.Type}";
                instance.transform.localScale = Vector3.one * tileSize;

                // Orient door based on corridor direction
                OrientDoor(instance, door, map);
            }
        }

        private void OrientDoor(GameObject door, DoorInstance doorData, SpatialMap map)
        {
            int x = doorData.Position.x;
            int y = doorData.Position.y;

            // If east/west neighbors are walls, door faces north/south
            bool wallEast = !map.InBounds(x + 1, y) || map.GetTile(x + 1, y).Type == TileType.Wall;
            bool wallWest = !map.InBounds(x - 1, y) || map.GetTile(x - 1, y).Type == TileType.Wall;

            if (wallEast || wallWest)
                door.transform.rotation = Quaternion.Euler(0, 90, 0);
        }

        // ─────────────────────────────────────────────
        //  STORY MARKERS
        // ─────────────────────────────────────────────

        private void RenderStoryMarkers(GenerationContext context, float tileSize)
        {
            foreach (var marker in context.StoryMarkers)
            {
                var prefab = _renderConfig.GetStoryMarkerPrefab(marker.Type);
                if (prefab == null) continue;

                var pos = TileToWorld(marker.Position.x, marker.Position.y, 0.01f, tileSize);
                var instance = Instantiate(prefab, pos, Quaternion.identity, _storyContainer);
                instance.name = $"Story_{marker.Type}_{marker.Position.x}_{marker.Position.y}";

                // Scale by intensity
                float scale = Mathf.Lerp(0.5f, 1.2f, marker.Intensity) * tileSize;
                instance.transform.localScale = Vector3.one * scale;

                // Random Y rotation for variety
                instance.transform.rotation = Quaternion.Euler(0, marker.Position.GetHashCode() % 360, 0);
            }
        }

        // ─────────────────────────────────────────────
        //  DECORATIONS
        // ─────────────────────────────────────────────

        private void RenderDecorations(GenerationContext context, float tileSize)
        {
            foreach (var deco in context.Decorations)
            {
                var prefab = _renderConfig.GetDecorationPrefab(deco.DecorationId);
                if (prefab == null) continue;

                var pos = TileToWorld(deco.Position.x, deco.Position.y, 0.01f, tileSize);
                var instance = Instantiate(prefab, pos, Quaternion.Euler(0, deco.Rotation, 0), _decorContainer);
                instance.name = $"Deco_{deco.DecorationId}_{deco.Position.x}_{deco.Position.y}";
                instance.transform.localScale = deco.Scale * tileSize;
            }
        }

        // ─────────────────────────────────────────────
        //  ENCOUNTERS
        // ─────────────────────────────────────────────

        private void RenderEncounters(GenerationContext context, float tileSize)
        {
            foreach (var encounter in context.Encounters)
            {
                foreach (var spawn in encounter.SpawnPoints)
                {
                    var prefab = _renderConfig.GetEnemyPrefab(spawn.EnemyTypeId);
                    if (prefab == null) continue;

                    var pos = TileToWorld(spawn.Position.x, spawn.Position.y, 0f, tileSize);
                    var instance = Instantiate(prefab, pos, Quaternion.identity, _encounterContainer);
                    instance.name = $"Enemy_{spawn.EnemyTypeId}_{spawn.Position.x}_{spawn.Position.y}";
                    instance.transform.localScale = Vector3.one * tileSize;
                }
            }
        }

        // ─────────────────────────────────────────────
        //  UTILITY
        // ─────────────────────────────────────────────

        private Vector3 TileToWorld(int x, int y, float yOffset, float tileSize)
        {
            return new Vector3(x * tileSize, yOffset, y * tileSize);
        }

        private void ApplyMaterial(GameObject go, Material mat)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = mat;
                r.sharedMaterials = mats;
            }
        }

        private void MarkStatic()
        {
            if (!_markStaticObjects) return;

            // Mark floor and wall objects as static for batching
            MarkChildrenStatic(_floorContainer);
            MarkChildrenStatic(_wallContainer);
        }

        private void MarkChildrenStatic(Transform parent)
        {
            if (parent == null) return;
            foreach (Transform child in parent)
            {
                child.gameObject.isStatic = true;
            }
        }
    }
}


