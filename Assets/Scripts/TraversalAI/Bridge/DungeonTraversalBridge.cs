using System.Collections.Generic;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using TraversalAI.Configuration;
using TraversalAI.Core;
using TraversalAI.Perception;
using UnityEngine;

namespace TraversalAI.Bridge
{
    /// <summary>
    /// Bridges the DungeonGenerator output to the TraversalAI system.
    /// Place alongside a DungeonGenerator to automatically spawn and initialize
    /// AI explorers on the generated dungeon.
    ///
    /// Workflow:
    /// 1. DungeonGenerator produces GenerationContext (graph + spatial map)
    /// 2. This bridge converts it to a TraversalDungeonGraph
    /// 3. Spawns AI explorers at the start node with their configured profiles
    /// 4. Manages the shared influence map for all explorers
    /// </summary>
    [RequireComponent(typeof(DungeonGenerator))]
    public class DungeonTraversalBridge : MonoBehaviour
    {
        [Header("Explorer Setup")]
        [Tooltip("Profiles to assign to spawned explorers. Each entry spawns one explorer.")]
        [SerializeField] private List<TraversalProfile> _explorerProfiles = new List<TraversalProfile>();

        [Tooltip("If no profiles are assigned, create default presets.")]
        [SerializeField] private bool _useDefaultPresets = true;

        [Tooltip("Maximum number of explorers to spawn.")]
        [SerializeField] private int _maxExplorers = 4;

        [Header("Explorer Appearance")]
        [SerializeField] private GameObject _explorerPrefab;
        [SerializeField] private float _explorerScale = 0.6f;

        [Header("Influence Map")]
        [Tooltip("Resolution of the influence map. Higher = more granular but slower.")]
        [SerializeField] private int _influenceMapWidth = 100;
        [SerializeField] private int _influenceMapHeight = 100;
        [SerializeField] private float _influenceCellSize = 1f;

        [Header("Spawn Settings")]
        [Tooltip("Auto-spawn explorers after dungeon generation.")]
        [SerializeField] private bool _autoSpawnOnGenerate = true;

        [Tooltip("Auto-initialize when DungeonGenerator fires OnDungeonGenerated event.")]
        [SerializeField] private bool _listenToGeneratorEvent = true;

        [Tooltip("Vertical offset for spawned explorers.")]
        [SerializeField] private float _spawnYOffset = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool _logDebugInfo = true;

        private DungeonGenerator _generator;
        private TraversalDungeonGraph _traversalGraph;
        private InfluenceMap.InfluenceMap _sharedInfluenceMap;
        private List<TraversalAIController> _activeExplorers = new List<TraversalAIController>();
        private GenerationContext _lastContext;

        // Public accessors
        public TraversalDungeonGraph TraversalGraph => _traversalGraph;
        public InfluenceMap.InfluenceMap SharedInfluenceMap => _sharedInfluenceMap;
        public IReadOnlyList<TraversalAIController> ActiveExplorers => _activeExplorers;
        public DungeonGenerator Generator => _generator;

        private static readonly Color[] ExplorerColors =
        {
            Color.cyan, Color.magenta, Color.yellow, Color.green,
            new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f),
            new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.7f)
        };

        private void Awake()
        {
            _generator = GetComponent<DungeonGenerator>();
        }

        private void OnEnable()
        {
            if (_listenToGeneratorEvent && _generator != null)
                _generator.OnDungeonGenerated += OnGeneratorCompleted;
        }

        private void OnDisable()
        {
            if (_generator != null)
                _generator.OnDungeonGenerated -= OnGeneratorCompleted;
        }

        /// <summary>
        /// Called automatically when the DungeonGenerator fires its event.
        /// </summary>
        private void OnGeneratorCompleted(GenerationContext context)
        {
            InitializeFromContext(context);
        }

        /// <summary>
        /// Initialize the traversal AI system from the last generated dungeon.
        /// Call after DungeonGenerator.Generate() has completed.
        /// </summary>
        [ContextMenu("Initialize Traversal AI")]
        public void InitializeFromGenerator()
        {
            var context = _generator.LastContext;
            if (context == null)
            {
                UnityEngine.Debug.LogError("[DungeonTraversalBridge] No generation context found. Generate a dungeon first.");
                return;
            }

            InitializeFromContext(context);
        }

        /// <summary>
        /// Initialize from a specific GenerationContext.
        /// </summary>
        public void InitializeFromContext(GenerationContext context)
        {
            _lastContext = context;

            // 1. Clean up any existing explorers
            DespawnExplorers();

            // 2. Build traversal graph from generation data
            _traversalGraph = new TraversalDungeonGraph();
            _traversalGraph.BuildFromGenerationData(context.Graph, context.SpatialMap);

            if (_logDebugInfo)
            {
                UnityEngine.Debug.Log($"[DungeonTraversalBridge] Built traversal graph: " +
                    $"{_traversalGraph.Nodes.Count} nodes, {_traversalGraph.Edges.Count} edges");
            }

            // 3. Create shared influence map sized to dungeon
            int mapW = _influenceMapWidth;
            int mapH = _influenceMapHeight;
            if (context.SpatialMap != null)
            {
                mapW = Mathf.Max(mapW, context.SpatialMap.Width);
                mapH = Mathf.Max(mapH, context.SpatialMap.Height);
            }
            _sharedInfluenceMap = new InfluenceMap.InfluenceMap(mapW, mapH, _influenceCellSize);

            // 4. Seed initial influence from dungeon data
            SeedInfluenceMap();

            // 5. Enrich traversal graph from encounter data
            EnrichFromEncounters(context);

            // 6. Spawn explorers
            if (_autoSpawnOnGenerate)
            {
                SpawnExplorers();
            }
        }

        /// <summary>
        /// Generate a dungeon and immediately initialize the traversal AI.
        /// Convenience method that combines both steps.
        /// </summary>
        [ContextMenu("Generate & Initialize")]
        public void GenerateAndInitialize()
        {
            var context = _generator.Generate();
            InitializeFromContext(context);
        }

        /// <summary>
        /// Generate with a specific seed and initialize.
        /// </summary>
        public void GenerateAndInitialize(int seed)
        {
            var context = _generator.Generate(seed);
            InitializeFromContext(context);
        }

        /// <summary>
        /// Spawn AI explorers on the current traversal graph.
        /// </summary>
        [ContextMenu("Spawn Explorers")]
        public void SpawnExplorers()
        {
            if (_traversalGraph == null)
            {
                UnityEngine.Debug.LogError("[DungeonTraversalBridge] No traversal graph. Initialize first.");
                return;
            }

            // Determine which profiles to use
            var profiles = GetProfilesToUse();

            // Find start node
            int startNodeId = FindStartNodeId();
            var startNode = _traversalGraph.GetNode(startNodeId);
            Vector3 startPos = startNode?.WorldPosition ?? Vector3.zero;

            int count = Mathf.Min(profiles.Count, _maxExplorers);

            for (int i = 0; i < count; i++)
            {
                var explorer = SpawnSingleExplorer(profiles[i], startPos, startNodeId, i);
                _activeExplorers.Add(explorer);
            }

            if (_logDebugInfo)
            {
                UnityEngine.Debug.Log($"[DungeonTraversalBridge] Spawned {count} explorers at node {startNodeId}");
            }
        }

        /// <summary>
        /// Spawn a single explorer with a specific profile at runtime.
        /// </summary>
        public TraversalAIController SpawnExplorer(TraversalProfile profile, int startNodeId = -1)
        {
            if (_traversalGraph == null)
            {
                UnityEngine.Debug.LogError("[DungeonTraversalBridge] No traversal graph. Initialize first.");
                return null;
            }

            if (startNodeId < 0) startNodeId = FindStartNodeId();
            var startNode = _traversalGraph.GetNode(startNodeId);
            Vector3 startPos = startNode?.WorldPosition ?? Vector3.zero;

            var explorer = SpawnSingleExplorer(profile, startPos, startNodeId, _activeExplorers.Count);
            _activeExplorers.Add(explorer);
            return explorer;
        }

        /// <summary>
        /// Remove and destroy all active explorers.
        /// </summary>
        [ContextMenu("Despawn Explorers")]
        public void DespawnExplorers()
        {
            foreach (var explorer in _activeExplorers)
            {
                if (explorer != null)
                {
                    if (Application.isPlaying)
                        Destroy(explorer.gameObject);
                    else
                        DestroyImmediate(explorer.gameObject);
                }
            }
            _activeExplorers.Clear();
        }

        private TraversalAIController SpawnSingleExplorer(TraversalProfile profile, Vector3 startPos, int startNodeId, int index)
        {
            GameObject go;

            if (_explorerPrefab != null)
            {
                go = Instantiate(_explorerPrefab);
            }
            else
            {
                // Create a simple capsule as fallback
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);

                // Remove collider (not needed for AI traversal demo)
                var collider = go.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying) Destroy(collider);
                    else DestroyImmediate(collider);
                }

                // Set color
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    renderer.material = new Material(shader);
                    renderer.material.color = ExplorerColors[index % ExplorerColors.Length];
                }
            }

            go.name = $"Explorer_{profile.profileName}";
            go.transform.position = startPos + new Vector3(index * 1.2f, _spawnYOffset, 0);
            go.transform.localScale = Vector3.one * _explorerScale;
            go.transform.SetParent(transform); // Parent under bridge for clean hierarchy

            // Add required components if not already present
            if (!go.TryGetComponent<PerceptionComponent>(out _))
                go.AddComponent<PerceptionComponent>();

            var controller = go.GetComponent<TraversalAIController>();
            if (controller == null)
                controller = go.AddComponent<TraversalAIController>();

            // Add debug visualizer
            if (!go.TryGetComponent<Debug.TraversalDebugVisualizer>(out _))
                go.AddComponent<Debug.TraversalDebugVisualizer>();

            // Apply profile via reflection (since _profile is serialized private)
            var profileField = typeof(TraversalAIController).GetField("_profile",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            profileField?.SetValue(controller, profile);

            // Initialize the AI
            controller.Initialize(_traversalGraph, startNodeId, _sharedInfluenceMap);

            return controller;
        }

        private List<TraversalProfile> GetProfilesToUse()
        {
            if (_explorerProfiles != null && _explorerProfiles.Count > 0)
            {
                // Filter out nulls
                var valid = new List<TraversalProfile>();
                foreach (var p in _explorerProfiles)
                {
                    if (p != null) valid.Add(p);
                }
                if (valid.Count > 0) return valid;
            }

            if (_useDefaultPresets)
            {
                return new List<TraversalProfile>
                {
                    TraversalPresets.CreateDiver(),
                    TraversalPresets.CreateCartographer(),
                    TraversalPresets.CreateGhost(),
                    TraversalPresets.CreateScavenger()
                };
            }

            // Fallback: single default explorer
            var fallback = ScriptableObject.CreateInstance<TraversalProfile>();
            fallback.profileName = "Default";
            return new List<TraversalProfile> { fallback };
        }

        private int FindStartNodeId()
        {
            // Look for a node tagged as start/safe zone
            foreach (var node in _traversalGraph.Nodes)
            {
                if (node.Label != null && node.Label.StartsWith("Start"))
                    return node.Id;
            }

            // Look for SafeZone tag
            var safeNodes = _traversalGraph.GetNodesWithTag(NodeTag.SafeZone);
            if (safeNodes.Count > 0) return safeNodes[0].Id;

            // Fallback to node 0
            return _traversalGraph.Nodes.Count > 0 ? _traversalGraph.Nodes[0].Id : 0;
        }

        private void SeedInfluenceMap()
        {
            foreach (var node in _traversalGraph.Nodes)
            {
                // Danger near enemy/boss rooms
                if (node.HasTag(NodeTag.Dangerous) || node.HasTag(NodeTag.BossArea))
                {
                    _sharedInfluenceMap.SetInfluence(
                        InfluenceMap.InfluenceLayerType.Danger,
                        node.WorldPosition, node.BaseDangerLevel);
                }

                // Loot density
                if (node.HasTag(NodeTag.Loot))
                {
                    _sharedInfluenceMap.SetInfluence(
                        InfluenceMap.InfluenceLayerType.LootDensity,
                        node.WorldPosition, node.BaseLootValue);
                }

                // Safety at safe zones
                if (node.HasTag(NodeTag.SafeZone))
                {
                    _sharedInfluenceMap.SetInfluence(
                        InfluenceMap.InfluenceLayerType.Safety,
                        node.WorldPosition, 0.8f);
                }

                // Everything starts with curiosity
                _sharedInfluenceMap.SetInfluence(
                    InfluenceMap.InfluenceLayerType.ExplorationCuriosity,
                    node.WorldPosition, 0.7f);
            }
        }

        /// <summary>
        /// Enrich traversal nodes with encounter data from the generation context.
        /// Maps encounters to node danger levels and tags via their RoomId.
        /// </summary>
        private void EnrichFromEncounters(GenerationContext context)
        {
            if (context.Encounters == null) return;

            foreach (var encounter in context.Encounters)
            {
                // EncounterInstance has a RoomId that maps to a SpatialMap room
                // Find the corresponding graph node via room → graph node mapping
                int graphNodeId = -1;

                if (context.SpatialMap != null && encounter.RoomId >= 0
                    && encounter.RoomId < context.SpatialMap.Rooms.Count)
                {
                    graphNodeId = context.SpatialMap.Rooms[encounter.RoomId].GraphNodeId;
                }
                else
                {
                    // Fallback: treat RoomId as graph node ID directly
                    graphNodeId = encounter.RoomId;
                }

                var node = _traversalGraph.GetNode(graphNodeId);
                if (node == null) continue;

                // Boost danger based on encounter type
                float dangerBoost = 0f;
                switch (encounter.Type)
                {
                    case EncounterType.Boss:
                        dangerBoost = 0.9f;
                        node.AddTag(NodeTag.BossArea);
                        break;
                    case EncounterType.MiniBoss:
                        dangerBoost = 0.6f;
                        node.AddTag(NodeTag.Dangerous);
                        break;
                    case EncounterType.Ambush:
                        dangerBoost = 0.5f;
                        node.AddTag(NodeTag.Dangerous);
                        node.AddTag(NodeTag.EnemyPresence);
                        break;
                    case EncounterType.Guard:
                    case EncounterType.Patrol:
                        dangerBoost = 0.3f;
                        node.AddTag(NodeTag.EnemyPresence);
                        break;
                    case EncounterType.Trap:
                        dangerBoost = 0.2f;
                        node.AddTag(NodeTag.Trapped);
                        break;
                }

                node.BaseDangerLevel = Mathf.Max(node.BaseDangerLevel, dangerBoost);

                // Update influence map
                _sharedInfluenceMap.AddInfluence(
                    InfluenceMap.InfluenceLayerType.Danger,
                    node.WorldPosition, dangerBoost);
            }
        }

        private void Update()
        {
            // Update shared influence maps periodically
            if (_sharedInfluenceMap != null && Time.frameCount % 30 == 0)
            {
                _sharedInfluenceMap.UpdateAll();
            }
        }

        private void OnDrawGizmos()
        {
            if (_traversalGraph == null) return;

            // Draw node positions for debugging
            foreach (var node in _traversalGraph.Nodes)
            {
                Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                Gizmos.DrawCube(
                    node.WorldPosition - Vector3.up * 0.4f,
                    new Vector3(
                        node.GridBounds.width * 0.8f,
                        0.1f,
                        node.GridBounds.height * 0.8f));
            }
        }
    }
}




