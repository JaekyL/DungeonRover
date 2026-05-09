using System.Collections.Generic;
using UnityEngine;
using DungeonGeneration.Data;

namespace DungeonGeneration.Rendering
{
    /// <summary>
    /// ScriptableObject that maps tile types and features to prefabs/materials.
    /// Assign your visual assets here.
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonRenderConfig", menuName = "Dungeon Generation/Render Config")]
    public class DungeonRenderConfig : ScriptableObject
    {
        [Header("Tile Size")]
        [Tooltip("World-space size of a single tile")]
        public float tileSize = 1f;

        [Header("Wall Height")]
        public float wallHeight = 2f;

        [Header("Core Tile Prefabs")]
        [Tooltip("Floor tile prefab – must be a 1x1 unit quad/plane/cube")]
        public GameObject floorPrefab;
        [Tooltip("Wall tile prefab – will be placed on edges of rooms")]
        public GameObject wallPrefab;
        [Tooltip("Corridor floor prefab (uses floor if null)")]
        public GameObject corridorFloorPrefab;
        [Tooltip("Ceiling prefab (optional, placed above rooms)")]
        public GameObject ceilingPrefab;

        [Header("Feature Prefabs")]
        public GameObject doorPrefab;
        public GameObject lockedDoorPrefab;
        public GameObject secretDoorPrefab;
        public GameObject stairsUpPrefab;
        public GameObject stairsDownPrefab;
        public GameObject waterPrefab;
        public GameObject rubblePrefab;
        public GameObject pitPrefab;

        [Header("Story Marker Prefabs")]
        public StoryMarkerPrefabEntry[] storyMarkerPrefabs;

        [Header("Decoration Prefabs")]
        public DecorationPrefabEntry[] decorationPrefabs;

        [Header("Encounter Prefabs")]
        public GameObject defaultEnemyPrefab;
        public EncounterPrefabEntry[] encounterPrefabs;

        [Header("Fallback")]
        [Tooltip("Used when no specific prefab is assigned for a tile type")]
        public GameObject fallbackPrefab;

        [Header("Materials")]
        public Material defaultFloorMaterial;
        public Material defaultWallMaterial;
        public Material waterMaterial;
        public Material corruptedMaterial;

        [Header("Rendering Options")]
        public bool generateCeiling = false;
        public bool useObjectPooling = true;
        public bool combineStaticMeshes = true;

        /// <summary>
        /// Gets the prefab for a given tile type.
        /// </summary>
        public GameObject GetTilePrefab(TileType type)
        {
            switch (type)
            {
                case TileType.Floor: return floorPrefab;
                case TileType.Corridor: return corridorFloorPrefab != null ? corridorFloorPrefab : floorPrefab;
                case TileType.Door: return doorPrefab;
                case TileType.SecretDoor: return secretDoorPrefab != null ? secretDoorPrefab : doorPrefab;
                case TileType.StairsUp: return stairsUpPrefab;
                case TileType.StairsDown: return stairsDownPrefab;
                case TileType.Water: return waterPrefab != null ? waterPrefab : floorPrefab;
                case TileType.Rubble: return rubblePrefab != null ? rubblePrefab : floorPrefab;
                case TileType.Pit: return pitPrefab;
                case TileType.Wall: return wallPrefab;
                default: return fallbackPrefab;
            }
        }

        /// <summary>
        /// Gets the door prefab for a given door type.
        /// </summary>
        public GameObject GetDoorPrefab(DoorType type)
        {
            switch (type)
            {
                case DoorType.Locked: return lockedDoorPrefab != null ? lockedDoorPrefab : doorPrefab;
                case DoorType.Secret: return secretDoorPrefab != null ? secretDoorPrefab : doorPrefab;
                case DoorType.Boss: return lockedDoorPrefab != null ? lockedDoorPrefab : doorPrefab;
                default: return doorPrefab;
            }
        }

        /// <summary>
        /// Gets a decoration prefab by decoration ID.
        /// </summary>
        public GameObject GetDecorationPrefab(string decorationId)
        {
            if (decorationPrefabs == null) return null;
            foreach (var entry in decorationPrefabs)
            {
                if (entry.decorationId == decorationId)
                    return entry.prefab;
            }
            return null;
        }

        /// <summary>
        /// Gets a story marker prefab by marker type.
        /// </summary>
        public GameObject GetStoryMarkerPrefab(StoryMarkerType type)
        {
            if (storyMarkerPrefabs == null) return null;
            foreach (var entry in storyMarkerPrefabs)
            {
                if (entry.markerType == type)
                    return entry.prefab;
            }
            return null;
        }

        /// <summary>
        /// Gets an enemy prefab by enemy type ID.
        /// </summary>
        public GameObject GetEnemyPrefab(string enemyTypeId)
        {
            if (encounterPrefabs != null)
            {
                foreach (var entry in encounterPrefabs)
                {
                    if (entry.enemyTypeId == enemyTypeId)
                        return entry.prefab;
                }
            }
            return defaultEnemyPrefab;
        }

        /// <summary>
        /// Gets a material override for a biome tag (e.g., "corrupted").
        /// </summary>
        public Material GetBiomeMaterial(string biomeTag)
        {
            if (string.IsNullOrEmpty(biomeTag)) return null;
            switch (biomeTag)
            {
                case "corrupted": return corruptedMaterial;
                default: return null;
            }
        }
    }

    [System.Serializable]
    public class StoryMarkerPrefabEntry
    {
        public StoryMarkerType markerType;
        public GameObject prefab;
    }

    [System.Serializable]
    public class DecorationPrefabEntry
    {
        public string decorationId;
        public GameObject prefab;
    }

    [System.Serializable]
    public class EncounterPrefabEntry
    {
        public string enemyTypeId;
        public GameObject prefab;
    }
}

