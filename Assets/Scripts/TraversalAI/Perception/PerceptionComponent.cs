using System.Collections.Generic;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Perception
{
    /// <summary>
    /// MonoBehaviour that handles perception updates for an AI explorer.
    /// Scans the dungeon graph within perception radius and updates the PerceivedDungeonState.
    /// </summary>
    public class PerceptionComponent : MonoBehaviour
    {
        [Header("Perception Settings")]
        [Tooltip("How many graph hops the AI can perceive from its current node.")]
        [SerializeField] private int _perceptionDepth = 2;

        [Tooltip("World-space radius for detecting entities and items.")]
        [SerializeField] private float _perceptionRadius = 10f;

        [Tooltip("Can this AI detect secret passages?")]
        [SerializeField] private bool _canDetectSecrets = false;

        [Tooltip("Perception update interval in seconds.")]
        [SerializeField] private float _updateInterval = 0.5f;

        private PerceivedDungeonState _perceivedState;
        private TraversalDungeonGraph _dungeonGraph;
        private int _currentNodeId = -1;
        private float _lastUpdateTime;

        public PerceivedDungeonState PerceivedState => _perceivedState;
        public int CurrentNodeId => _currentNodeId;
        public int PerceptionDepth
        {
            get => _perceptionDepth;
            set => _perceptionDepth = Mathf.Max(1, value);
        }
        public float PerceptionRadius
        {
            get => _perceptionRadius;
            set => _perceptionRadius = Mathf.Max(0f, value);
        }

        /// <summary>Initialize the perception system with the dungeon graph.</summary>
        public void Initialize(TraversalDungeonGraph graph, PerceivedDungeonState existingState = null)
        {
            _dungeonGraph = graph;
            _perceivedState = existingState ?? new PerceivedDungeonState();
        }

        /// <summary>Set the AI's current position in the graph.</summary>
        public void SetCurrentNode(int nodeId)
        {
            _currentNodeId = nodeId;
        }

        /// <summary>Perform a perception scan from the current node.</summary>
        public void UpdatePerception()
        {
            if (_dungeonGraph == null || _currentNodeId < 0) return;

            float currentTime = Time.time;

            // First, demote all currently visible nodes to remembered
            var previouslyVisible = _perceivedState.GetVisibleNodeIds();
            foreach (var id in previouslyVisible)
            {
                _perceivedState.UpdateNodeVisibility(id, VisibilityState.Remembered, currentTime);
            }

            // BFS from current node up to perception depth
            var visited = new HashSet<int>();
            var queue = new Queue<(int nodeId, int depth)>();
            queue.Enqueue((_currentNodeId, 0));
            visited.Add(_currentNodeId);

            // Record visit to current node
            _perceivedState.RecordVisit(_currentNodeId, currentTime);
            SyncNodePerception(_currentNodeId, currentTime);

            while (queue.Count > 0)
            {
                var (nodeId, depth) = queue.Dequeue();

                if (depth >= _perceptionDepth) continue;

                var edges = _dungeonGraph.GetEdgesFrom(nodeId);
                foreach (var edge in edges)
                {
                    // Skip secret edges unless we can detect them
                    if (edge.IsSecret && !_canDetectSecrets) continue;

                    int neighborId = edge.GetOtherNode(nodeId);
                    if (visited.Contains(neighborId)) continue;

                    visited.Add(neighborId);

                    // Reveal the edge
                    _perceivedState.RevealEdge(edge.Id);

                    // Update neighbor visibility
                    _perceivedState.UpdateNodeVisibility(neighborId, VisibilityState.Visible, currentTime);
                    SyncNodePerception(neighborId, currentTime);

                    queue.Enqueue((neighborId, depth + 1));
                }
            }

            // Decay old memories
            _perceivedState.DecayMemories(currentTime);

            _lastUpdateTime = currentTime;
        }

        /// <summary>Sync ground-truth node data to perceived data (what the AI can actually see).</summary>
        private void SyncNodePerception(int nodeId, float time)
        {
            var groundTruth = _dungeonGraph.GetNode(nodeId);
            if (groundTruth == null) return;

            var perceived = _perceivedState.GetNodeInfo(nodeId);
            perceived.PerceivedTags = groundTruth.Tags;
            perceived.PerceivedDanger = groundTruth.BaseDangerLevel;
            perceived.PerceivedLootValue = groundTruth.BaseLootValue;
            perceived.LastSeenTime = time;
        }

        /// <summary>Infer information about a node the AI hasn't directly seen (e.g., from sounds).</summary>
        public void InferNodeInfo(int nodeId, NodeTag inferredTags, float confidence)
        {
            var info = _perceivedState.GetNodeInfo(nodeId);
            if (info.Visibility == VisibilityState.Unknown)
                info.Visibility = VisibilityState.Inferred;
            info.PerceivedTags |= inferredTags;
            info.Confidence = Mathf.Max(info.Confidence, confidence);
        }

        private void Update()
        {
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdatePerception();
            }
        }
    }
}

