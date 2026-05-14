using System.Collections.Generic;
using System.Linq;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Perception
{
    /// <summary>
    /// The AI's subjective model of the dungeon based on perception and memory.
    /// This is the "fog of war" layer — the AI only knows what it has perceived.
    /// </summary>
    [System.Serializable]
    public class PerceivedDungeonState
    {
        private Dictionary<int, PerceivedNodeInfo> _nodeInfos = new Dictionary<int, PerceivedNodeInfo>();
        private HashSet<int> _knownEdges = new HashSet<int>();

        /// <summary>Total nodes the AI has discovered.</summary>
        public int DiscoveredNodeCount => _nodeInfos.Count(kvp => kvp.Value.Visibility != VisibilityState.Unknown);

        /// <summary>Total nodes the AI has visited.</summary>
        public int VisitedNodeCount => _nodeInfos.Count(kvp => kvp.Value.VisitCount > 0);

        public PerceivedNodeInfo GetNodeInfo(int nodeId)
        {
            if (!_nodeInfos.TryGetValue(nodeId, out var info))
            {
                info = new PerceivedNodeInfo { NodeId = nodeId };
                _nodeInfos[nodeId] = info;
            }
            return info;
        }

        public void UpdateNodeVisibility(int nodeId, VisibilityState state, float time)
        {
            var info = GetNodeInfo(nodeId);
            info.Visibility = state;
            if (state == VisibilityState.Visible)
            {
                info.LastSeenTime = time;
                info.Confidence = 1f;
            }
        }

        public void RecordVisit(int nodeId, float time)
        {
            var info = GetNodeInfo(nodeId);
            info.VisitCount++;
            info.LastSeenTime = time;
            info.Visibility = VisibilityState.Visible;
            info.PerceivedTags &= ~NodeTag.Unexplored;
            info.PerceivedTags |= NodeTag.Explored;
            info.Confidence = 1f;
        }

        public void RevealEdge(int edgeId) => _knownEdges.Add(edgeId);
        public bool IsEdgeKnown(int edgeId) => _knownEdges.Contains(edgeId);

        public List<int> GetUnexploredNodeIds()
        {
            return _nodeInfos
                .Where(kvp => kvp.Value.PerceivedTags.HasFlag(NodeTag.Unexplored) &&
                              kvp.Value.Visibility != VisibilityState.Unknown)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public List<int> GetVisibleNodeIds()
        {
            return _nodeInfos
                .Where(kvp => kvp.Value.Visibility == VisibilityState.Visible)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public List<int> GetKnownNodeIds()
        {
            return _nodeInfos
                .Where(kvp => kvp.Value.Visibility != VisibilityState.Unknown)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>Decay confidence of old memories over time.</summary>
        public void DecayMemories(float currentTime, float decayRate = 0.01f)
        {
            foreach (var kvp in _nodeInfos)
            {
                if (kvp.Value.Visibility == VisibilityState.Visible) continue;
                var staleness = kvp.Value.Staleness(currentTime);
                kvp.Value.Confidence = Mathf.Max(0f, kvp.Value.Confidence - staleness * decayRate);
                if (kvp.Value.Visibility == VisibilityState.Remembered && kvp.Value.Confidence < 0.1f)
                    kvp.Value.Visibility = VisibilityState.Inferred;
            }
        }

        /// <summary>Serialize state for save/load.</summary>
        public PerceivedDungeonStateData ToSaveData()
        {
            return new PerceivedDungeonStateData
            {
                NodeInfos = _nodeInfos.Values.ToList(),
                KnownEdgeIds = _knownEdges.ToList()
            };
        }

        /// <summary>Restore from saved data.</summary>
        public void LoadFromSaveData(PerceivedDungeonStateData data)
        {
            _nodeInfos.Clear();
            _knownEdges.Clear();
            foreach (var info in data.NodeInfos)
                _nodeInfos[info.NodeId] = info;
            foreach (var edgeId in data.KnownEdgeIds)
                _knownEdges.Add(edgeId);
        }
    }

    /// <summary>Serializable save data for PerceivedDungeonState.</summary>
    [System.Serializable]
    public class PerceivedDungeonStateData
    {
        public List<PerceivedNodeInfo> NodeInfos = new List<PerceivedNodeInfo>();
        public List<int> KnownEdgeIds = new List<int>();
    }
}

