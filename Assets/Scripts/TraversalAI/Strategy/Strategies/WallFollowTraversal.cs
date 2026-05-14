using System.Collections.Generic;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Strategy.Strategies
{
    /// <summary>
    /// Wall-follow traversal: follows a consistent directional bias (right/left-hand rule).
    /// Classic maze-solving strategy.
    /// </summary>
    public class WallFollowTraversal : ITraversalStrategy
    {
        private bool _rightHand;

        public WallFollowTraversal(bool rightHand = true) { _rightHand = rightHand; }

        public string StrategyName => _rightHand ? "Wall-Follow (Right)" : "Wall-Follow (Left)";

        public TraversalDecision Evaluate(TraversalContext context)
        {
            var decision = new TraversalDecision
            {
                Reasoning = $"Wall-Follow: {(_rightHand ? "Right" : "Left")}-hand rule"
            };

            var currentNode = context.Graph.GetNode(context.CurrentNodeId);
            if (currentNode == null) return decision;

            var neighbors = context.Graph.GetNeighbors(context.CurrentNodeId);

            var history = context.Memory.VisitHistory;
            int previousNodeId = history.Count >= 2 ? history[history.Count - 2] : -1;

            var previousNode = previousNodeId >= 0 ? context.Graph.GetNode(previousNodeId) : null;
            Vector3 forwardDir = previousNode != null
                ? (currentNode.WorldPosition - previousNode.WorldPosition).normalized
                : Vector3.forward;

            var scored = new List<ScoredNode>();
            foreach (var neighbor in neighbors)
            {
                Vector3 toNeighbor = (neighbor.WorldPosition - currentNode.WorldPosition).normalized;
                float angle = Vector3.SignedAngle(forwardDir, toNeighbor, Vector3.up);

                float normalizedAngle = _rightHand
                    ? (180f - angle) / 360f
                    : (180f + angle) / 360f;

                float score = normalizedAngle;
                if (!context.Memory.HasVisited(neighbor.Id))
                    score += 0.5f;

                scored.Add(new ScoredNode(neighbor.Id, score,
                    $"Angle: {angle:F0}° {(context.Memory.HasVisited(neighbor.Id) ? "(visited)" : "(new)")}"));
            }

            scored.Sort((a, b) => b.Score.CompareTo(a.Score));
            decision.RankedCandidates = scored;
            return decision;
        }

        public float GetEdgeWeightBias(DungeonEdge edge, TraversalContext context) => 1f;
    }
}

