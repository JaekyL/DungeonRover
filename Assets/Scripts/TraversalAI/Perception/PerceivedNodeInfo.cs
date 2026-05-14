using System.Collections.Generic;
using TraversalAI.Core;

namespace TraversalAI.Perception
{
    /// <summary>
    /// Represents the AI's knowledge about a specific node.
    /// This is what the AI "believes" about a node, which may differ from ground truth.
    /// </summary>
    [System.Serializable]
    public class PerceivedNodeInfo
    {
        public int NodeId;
        public VisibilityState Visibility = VisibilityState.Unknown;
        public NodeTag PerceivedTags = NodeTag.Unexplored;
        public float PerceivedDanger;
        public float PerceivedLootValue;
        public float LastSeenTime = -1f;
        public int VisitCount;
        public float Confidence; // [0..1] how confident the AI is in its perception
        public List<string> DetectedEntities = new List<string>();
        public List<string> DetectedItems = new List<string>();

        /// <summary>How stale this information is (in seconds since last observation).</summary>
        public float Staleness(float currentTime)
        {
            if (LastSeenTime < 0f) return float.MaxValue;
            return currentTime - LastSeenTime;
        }
    }
}

