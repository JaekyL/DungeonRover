using System.Collections.Generic;

namespace TraversalAI.Strategy
{
    /// <summary>
    /// Result of a traversal strategy's evaluation.
    /// Contains scored candidate nodes with reasoning.
    /// </summary>
    [System.Serializable]
    public class TraversalDecision
    {
        public List<ScoredNode> RankedCandidates = new List<ScoredNode>();
        public int PreferredNodeId => RankedCandidates.Count > 0 ? RankedCandidates[0].NodeId : -1;
        public string Reasoning = "";
    }

    [System.Serializable]
    public struct ScoredNode
    {
        public int NodeId;
        public float Score;
        public string Reason;

        public ScoredNode(int nodeId, float score, string reason = "")
        {
            NodeId = nodeId;
            Score = score;
            Reason = reason;
        }
    }
}

