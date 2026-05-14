using System.Collections.Generic;
using System.Linq;
using TraversalAI.Core;
using TraversalAI.Goals;
using TraversalAI.InfluenceMap;
using UnityEngine;

namespace TraversalAI.UtilityAI
{
    /// <summary>
    /// Evaluates all candidate goals and selects the best one using utility scoring.
    /// This is the central decision-making nexus of the AI.
    /// </summary>
    public class GoalEvaluator
    {
        private UtilityScorer _scorer;
        private float _maxGraphDistance = 20f;

        /// <summary>Last evaluation results, for debug visualization.</summary>
        public List<GoalScore> LastEvaluation { get; private set; } = new List<GoalScore>();

        public GoalEvaluator(UtilityScorer scorer)
        {
            _scorer = scorer;
        }

        /// <summary>Evaluate all candidate goals and return the best valid one.</summary>
        public ITraversalGoal EvaluateBest(List<ITraversalGoal> candidates, GoalContext context,
            InfluenceSampler influenceSampler = null)
        {
            LastEvaluation.Clear();

            foreach (var goal in candidates)
            {
                if (!goal.IsValid(context)) continue;
                if (goal.IsComplete(context)) continue;

                var scoringCtx = BuildScoringContext(goal, context, influenceSampler);
                float score = _scorer.Score(goal, context, scoringCtx);

                LastEvaluation.Add(new GoalScore { Goal = goal, Score = score });
            }

            LastEvaluation.Sort((a, b) => b.Score.CompareTo(a.Score));
            return LastEvaluation.Count > 0 ? LastEvaluation[0].Goal : null;
        }

        /// <summary>Get top N goals ranked by score.</summary>
        public List<GoalScore> EvaluateTop(List<ITraversalGoal> candidates, GoalContext context,
            int topN, InfluenceSampler influenceSampler = null)
        {
            EvaluateBest(candidates, context, influenceSampler);
            return LastEvaluation.Take(topN).ToList();
        }

        private ScoringContext BuildScoringContext(ITraversalGoal goal, GoalContext context,
            InfluenceSampler sampler)
        {
            var scoringCtx = new ScoringContext();

            if (goal.TargetNodeId >= 0)
            {
                var targetNode = context.DungeonGraph.GetNode(goal.TargetNodeId);
                var currentNode = context.DungeonGraph.GetNode(context.CurrentNodeId);

                if (targetNode != null && currentNode != null)
                {
                    float dist = Vector3.Distance(currentNode.WorldPosition, targetNode.WorldPosition);
                    scoringCtx.NormalizedDistance = Mathf.Clamp01(dist / _maxGraphDistance);
                }

                var info = context.PerceivedState.GetNodeInfo(goal.TargetNodeId);
                scoringCtx.NormalizedVisitCount = Mathf.Clamp01(info.VisitCount / 5f);
                scoringCtx.NormalizedStaleness = Mathf.Clamp01(
                    info.Staleness(context.CurrentTime) / 120f);

                if (targetNode != null)
                {
                    var neighbors = context.DungeonGraph.GetNeighbors(goal.TargetNodeId);
                    int unexplored = neighbors.Count(n => n.HasTag(NodeTag.Unexplored));
                    scoringCtx.ExplorationValue = neighbors.Count > 0
                        ? (float)unexplored / neighbors.Count : 0f;
                }

                if (sampler != null && targetNode != null)
                {
                    scoringCtx.NoiseLevel = sampler.SampleLayer(
                        InfluenceLayerType.Noise, targetNode.WorldPosition);
                    scoringCtx.AllyProximity = sampler.SampleLayer(
                        InfluenceLayerType.AllySupport, targetNode.WorldPosition);
                }
            }

            return scoringCtx;
        }
    }

    /// <summary>Pairs a goal with its computed utility score.</summary>
    [System.Serializable]
    public struct GoalScore
    {
        public ITraversalGoal Goal;
        public float Score;
    }
}

