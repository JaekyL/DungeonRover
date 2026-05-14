using System.Collections.Generic;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Strategy.Strategies
{
    /// <summary>
    /// Sector sweep: divides the dungeon into angular sectors and clears each systematically.
    /// </summary>
    public class SectorSweepTraversal : ITraversalStrategy
    {
        public string StrategyName => "Sector Sweep";

        private int _currentSector;
        private const int SECTOR_COUNT = 8;

        public TraversalDecision Evaluate(TraversalContext context)
        {
            var decision = new TraversalDecision { Reasoning = $"Sector Sweep: Clearing sector {_currentSector}/{SECTOR_COUNT}" };
            var currentNode = context.Graph.GetNode(context.CurrentNodeId);
            if (currentNode == null) return decision;

            int startNodeId = context.Memory.VisitHistory.Count > 0 ? context.Memory.VisitHistory[0] : context.CurrentNodeId;
            var center = context.Graph.GetNode(startNodeId)?.WorldPosition ?? Vector3.zero;

            float sectorAngleSize = 360f / SECTOR_COUNT;
            float sectorStart = _currentSector * sectorAngleSize;
            float sectorEnd = sectorStart + sectorAngleSize;

            var neighbors = context.Graph.GetNeighbors(context.CurrentNodeId);
            bool anySectorUnvisited = false;

            foreach (var neighbor in neighbors)
            {
                Vector3 dir = neighbor.WorldPosition - center;
                float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                bool inCurrentSector = angle >= sectorStart && angle < sectorEnd;
                float score;
                string reason;

                if (!context.Memory.HasVisited(neighbor.Id))
                {
                    if (inCurrentSector)
                    {
                        score = 1.0f;
                        reason = $"Unvisited in sector {_currentSector}";
                        anySectorUnvisited = true;
                    }
                    else
                    {
                        score = 0.3f;
                        reason = "Unvisited (different sector)";
                    }
                }
                else
                {
                    score = inCurrentSector ? 0.15f : 0.05f;
                    reason = "Already visited";
                }

                decision.RankedCandidates.Add(new ScoredNode(neighbor.Id, score, reason));
            }

            if (!anySectorUnvisited)
                _currentSector = (_currentSector + 1) % SECTOR_COUNT;

            decision.RankedCandidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            return decision;
        }

        public float GetEdgeWeightBias(DungeonEdge edge, TraversalContext context) => 1f;
    }
}

