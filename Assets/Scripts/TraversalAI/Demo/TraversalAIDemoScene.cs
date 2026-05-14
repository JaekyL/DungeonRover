using System.Collections.Generic;
using DungeonGeneration.Core;
using TraversalAI.Bridge;
using TraversalAI.Configuration;
using TraversalAI.Core;
using TraversalAI.Perception;
using UnityEngine;
using DungeonGeneration.Data;

namespace TraversalAI.Demo
{
    /// <summary>
    /// Demo scene bootstrapper. Can work in two modes:
    ///
    /// 1. With DungeonGenerator: Finds a DungeonGenerator in the scene, generates a dungeon,
    ///    and uses DungeonTraversalBridge to spawn AI explorers on the real dungeon.
    ///
    /// 2. Standalone: Creates a test dungeon graph and spawns explorers manually (fallback).
    ///
    /// Usage: Add this to a GameObject in the scene and press Play.
    /// If a DungeonGenerator exists in the scene, it will be used automatically.
    /// </summary>
    public class TraversalAIDemoScene : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private int _explorerCount = 4;
        [SerializeField] private float _nodeSpacing = 8f;
        [SerializeField] private bool _createDefaultProfiles = true;

        [Header("Generator Integration")]
        [Tooltip("If set, uses this DungeonGenerator. Otherwise searches the scene.")]
        [SerializeField] private DungeonGenerator _dungeonGenerator;

        [Tooltip("If true, generates a new dungeon on Start. If false, uses existing generation.")]
        [SerializeField] private bool _generateOnStart = true;

        [Tooltip("Seed for generation. -1 = random.")]
        [SerializeField] private int _seed = -1;

        [Header("Explorer Profiles (Optional)")]
        [SerializeField] private List<TraversalProfile> _customProfiles = new List<TraversalProfile>();

        [Header("Debug")]
        [SerializeField] private bool _drawNodeLabels = true;

        private TraversalDungeonGraph _traversalGraph;
        private InfluenceMap.InfluenceMap _sharedInfluenceMap;
        private List<TraversalAIController> _explorers = new List<TraversalAIController>();
        private DungeonTraversalBridge _bridge;

        private void Start()
        {
            // Try to find/use DungeonGenerator
            if (_dungeonGenerator == null)
                _dungeonGenerator = FindAnyObjectByType<DungeonGenerator>();

            if (_dungeonGenerator != null)
            {
                InitializeWithGenerator();
            }
            else
            {
                // Fallback: standalone mode with manual test dungeon
                UnityEngine.Debug.Log("[Demo] No DungeonGenerator found. Creating standalone test dungeon.");
                InitializeStandalone();
            }
        }

        /// <summary>
        /// Initialize using the DungeonGenerator + DungeonTraversalBridge.
        /// </summary>
        private void InitializeWithGenerator()
        {
            // Ensure bridge component exists on the generator
            _bridge = _dungeonGenerator.GetComponent<DungeonTraversalBridge>();
            if (_bridge == null)
                _bridge = _dungeonGenerator.gameObject.AddComponent<DungeonTraversalBridge>();

            // Configure the bridge with our profiles
            ConfigureBridge(_bridge);

            // Generate if needed
            if (_generateOnStart || _dungeonGenerator.LastContext == null)
            {
                if (_seed >= 0)
                    _bridge.GenerateAndInitialize(_seed);
                else
                    _bridge.GenerateAndInitialize();
            }
            else
            {
                _bridge.InitializeFromContext(_dungeonGenerator.LastContext);
            }

            // Cache references
            _traversalGraph = _bridge.TraversalGraph;
            _sharedInfluenceMap = _bridge.SharedInfluenceMap;
            foreach (var explorer in _bridge.ActiveExplorers)
                _explorers.Add(explorer);

            UnityEngine.Debug.Log($"[Demo] Initialized with DungeonGenerator: " +
                $"{_traversalGraph.Nodes.Count} nodes, {_traversalGraph.Edges.Count} edges, " +
                $"{_explorers.Count} explorers.");
        }

        private void ConfigureBridge(DungeonTraversalBridge bridge)
        {
            var profilesField = typeof(DungeonTraversalBridge).GetField("_explorerProfiles",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxField = typeof(DungeonTraversalBridge).GetField("_maxExplorers",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var presetsField = typeof(DungeonTraversalBridge).GetField("_useDefaultPresets",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (_customProfiles.Count > 0)
                profilesField?.SetValue(bridge, _customProfiles);

            maxField?.SetValue(bridge, _explorerCount);
            presetsField?.SetValue(bridge, _createDefaultProfiles);
        }

        /// <summary>
        /// Fallback: creates a manual test dungeon when no DungeonGenerator is present.
        /// </summary>
        private void InitializeStandalone()
        {
            var sourceGraph = BuildTestDungeon();

            _traversalGraph = new TraversalDungeonGraph();
            _traversalGraph.BuildFromGenerationData(sourceGraph, null);

            _sharedInfluenceMap = new InfluenceMap.InfluenceMap(100, 100, 1f);
            SetupInitialInfluence();
            SpawnExplorers();

            UnityEngine.Debug.Log($"[Demo] Standalone dungeon: {sourceGraph.Nodes.Count} nodes, " +
                $"{sourceGraph.Edges.Count} edges. Spawned {_explorers.Count} explorers.");
        }

        private DungeonGraph BuildTestDungeon()
        {
            var graph = new DungeonGraph();
            float s = _nodeSpacing;

            // === FLOOR 1 ===
            // Start room (node 0) — safe zone
            var start = graph.AddNode(RoomType.Start);
            start.Bounds = new RectInt(0, 0, 4, 4);

            // Hub room (node 1) — central intersection
            var hub = graph.AddNode(RoomType.Hub);
            hub.Bounds = new RectInt((int)s, 0, 6, 6);
            hub.Importance = 3;

            // North branch: Corridor → Dead end with loot
            var northCorr = graph.AddNode(RoomType.Normal);
            northCorr.Bounds = new RectInt((int)s, (int)s, 3, 3);

            var northDead = graph.AddNode(RoomType.DeadEnd);
            northDead.Bounds = new RectInt((int)s, (int)(s * 2), 3, 3);

            // East branch: Enemy zone → Boss
            var eastRoom = graph.AddNode(RoomType.Normal);
            eastRoom.Bounds = new RectInt((int)(s * 2), 0, 4, 4);
            eastRoom.DifficultyTier = 3;

            var bossRoom = graph.AddNode(RoomType.Boss);
            bossRoom.Bounds = new RectInt((int)(s * 3), 0, 5, 5);
            bossRoom.DifficultyTier = 8;

            // South branch: Treasure room (through locked door)
            var southCorr = graph.AddNode(RoomType.Normal);
            southCorr.Bounds = new RectInt((int)s, -(int)s, 3, 3);

            var treasureRoom = graph.AddNode(RoomType.Treasure);
            treasureRoom.Bounds = new RectInt((int)s, -(int)(s * 2), 4, 4);

            // West branch: Secret room
            var westRoom = graph.AddNode(RoomType.Normal);
            westRoom.Bounds = new RectInt(-(int)s, 0, 3, 3);

            var secretRoom = graph.AddNode(RoomType.Secret);
            secretRoom.Bounds = new RectInt(-(int)(s * 2), 0, 3, 3);
            secretRoom.IsSecret = true;

            // Stairs to floor 2
            var stairsRoom = graph.AddNode(RoomType.Transition);
            stairsRoom.Bounds = new RectInt((int)(s * 2), (int)s, 3, 3);

            // Extra rooms for complexity
            var guardRoom = graph.AddNode(RoomType.Normal);
            guardRoom.Bounds = new RectInt((int)(s * 2), -(int)s, 4, 4);
            guardRoom.DifficultyTier = 5;

            var storageRoom = graph.AddNode(RoomType.Normal);
            storageRoom.Bounds = new RectInt(-(int)s, (int)s, 3, 3);

            var shortcutRoom = graph.AddNode(RoomType.Normal);
            shortcutRoom.Bounds = new RectInt(-(int)s, -(int)s, 3, 3);

            // === EDGES ===
            // Main connections
            graph.AddEdge(start.Id, hub.Id, EdgeType.Normal);             // Start → Hub
            graph.AddEdge(hub.Id, northCorr.Id, EdgeType.Normal);         // Hub → North corridor
            graph.AddEdge(northCorr.Id, northDead.Id, EdgeType.Normal);   // North corridor → Dead end
            graph.AddEdge(hub.Id, eastRoom.Id, EdgeType.Normal);          // Hub → East (enemy)
            graph.AddEdge(eastRoom.Id, bossRoom.Id, EdgeType.Normal);     // East → Boss
            graph.AddEdge(hub.Id, southCorr.Id, EdgeType.Normal);         // Hub → South corridor
            graph.AddEdge(southCorr.Id, treasureRoom.Id, EdgeType.Locked); // South → Treasure (locked)
            graph.AddEdge(hub.Id, westRoom.Id, EdgeType.Normal);          // Hub → West
            graph.AddEdge(westRoom.Id, secretRoom.Id, EdgeType.Secret);   // West → Secret
            graph.AddEdge(eastRoom.Id, stairsRoom.Id, EdgeType.Normal);   // East → Stairs
            graph.AddEdge(eastRoom.Id, guardRoom.Id, EdgeType.Normal);    // East → Guard room
            graph.AddEdge(westRoom.Id, storageRoom.Id, EdgeType.Normal);  // West → Storage
            graph.AddEdge(hub.Id, shortcutRoom.Id, EdgeType.Normal);      // Hub → Shortcut room

            // Shortcut: connects distant parts of the dungeon
            graph.AddEdge(shortcutRoom.Id, stairsRoom.Id, EdgeType.Shortcut); // Shortcut!

            // Loop: guard room connects back to south
            graph.AddEdge(guardRoom.Id, southCorr.Id, EdgeType.Normal);

            return graph;
        }

        private void SetupInitialInfluence()
        {
            // Add danger influence near enemy rooms
            foreach (var node in _traversalGraph.Nodes)
            {
                if (node.HasTag(NodeTag.Dangerous) || node.HasTag(NodeTag.BossArea))
                {
                    _sharedInfluenceMap.SetInfluence(
                        InfluenceMap.InfluenceLayerType.Danger,
                        node.WorldPosition, node.BaseDangerLevel);
                }

                if (node.HasTag(NodeTag.Loot))
                {
                    _sharedInfluenceMap.SetInfluence(
                        InfluenceMap.InfluenceLayerType.LootDensity,
                        node.WorldPosition, node.BaseLootValue);
                }

                if (node.HasTag(NodeTag.SafeZone))
                {
                    _sharedInfluenceMap.SetInfluence(
                        InfluenceMap.InfluenceLayerType.Safety,
                        node.WorldPosition, 0.8f);
                }

                // Everything starts with high curiosity
                _sharedInfluenceMap.SetInfluence(
                    InfluenceMap.InfluenceLayerType.ExplorationCuriosity,
                    node.WorldPosition, 0.7f);
            }
        }

        private void SpawnExplorers()
        {
            // Default profiles if none provided
            TraversalProfile[] profiles;
            if (_customProfiles.Count > 0)
            {
                profiles = _customProfiles.ToArray();
            }
            else if (_createDefaultProfiles)
            {
                profiles = new[]
                {
                    TraversalPresets.CreateDiver(),
                    TraversalPresets.CreateCartographer(),
                    TraversalPresets.CreateGhost(),
                    TraversalPresets.CreateScavenger()
                };
            }
            else
            {
                profiles = new TraversalProfile[0];
            }

            int count = Mathf.Max(1, Mathf.Min(_explorerCount, profiles.Length > 0 ? profiles.Length : 4));
            var startNode = _traversalGraph.GetNode(0);
            Vector3 startPos = startNode?.WorldPosition ?? Vector3.zero;

            Color[] explorerColors = {
                Color.cyan, Color.magenta, Color.yellow, Color.green,
                new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f)
            };

            for (int i = 0; i < count; i++)
            {
                // Create explorer GameObject
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = profiles.Length > i
                    ? $"Explorer_{profiles[i].profileName}"
                    : $"Explorer_{i}";

                // Offset start positions slightly
                go.transform.position = startPos + new Vector3(i * 1.5f, 0.5f, 0);
                go.transform.localScale = new Vector3(0.6f, 1f, 0.6f);

                // Set color
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    renderer.material.color = explorerColors[i % explorerColors.Length];
                }

                // Remove collider (not needed for demo)
                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                // Add AI components
                var controller = go.AddComponent<TraversalAIController>();
                go.AddComponent<Debug.TraversalDebugVisualizer>();

                // Set profile via serialized field (use reflection since _profile is private)
                var profileField = typeof(TraversalAIController).GetField("_profile",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (profileField != null && profiles.Length > i)
                {
                    profileField.SetValue(controller, profiles[i]);
                }

                // Initialize
                controller.Initialize(_traversalGraph, 0, _sharedInfluenceMap);
                _explorers.Add(controller);
            }
        }

        private void Update()
        {
            // Only update influence in standalone mode (bridge handles its own)
            if (_bridge == null && Time.frameCount % 30 == 0)
            {
                _sharedInfluenceMap?.UpdateAll();
            }
        }

        private void OnDrawGizmos()
        {
            if (_traversalGraph == null || !_drawNodeLabels) return;

            // Draw floor plane markers for each node
            foreach (var node in _traversalGraph.Nodes)
            {
                // Draw a flat disc at ground level
                Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                Gizmos.DrawCube(
                    node.WorldPosition - Vector3.up * 0.4f,
                    new Vector3(
                        node.GridBounds.width * 0.8f,
                        0.1f,
                        node.GridBounds.height * 0.8f
                    ));
            }
        }
    }
}

