using System.Collections.Generic;
using System.Linq;
using TraversalAI.Core;
using TraversalAI.Goals;
using UnityEngine;

namespace TraversalAI.BehaviorRules
{
    /// <summary>
    /// Evaluates the player-configured behavior rules list and applies directives.
    /// </summary>
    public class RuleEvaluator
    {
        private bool _cumulativeMode;

        /// <summary>Last triggered rules, for debug display.</summary>
        public List<BehaviorRule> LastTriggeredRules { get; private set; } = new List<BehaviorRule>();

        public RuleEvaluator(bool cumulativeMode = false)
        {
            _cumulativeMode = cumulativeMode;
        }

        /// <summary>Evaluate rules and return applicable directives.</summary>
        public List<ActionDirective> Evaluate(List<BehaviorRule> rules, BehaviorContext context)
        {
            LastTriggeredRules.Clear();
            var directives = new List<ActionDirective>();

            var sorted = rules.OrderBy(r => r.order).ToList();

            foreach (var rule in sorted)
            {
                if (rule.Evaluate(context))
                {
                    directives.Add(rule.action);
                    LastTriggeredRules.Add(rule);

                    if (!_cumulativeMode) break;
                }
            }

            return directives;
        }

        /// <summary>Apply directives to modify goal scoring.</summary>
        public void ApplyDirectives(List<ActionDirective> directives, List<ITraversalGoal> goals,
            GoalContext goalContext)
        {
            foreach (var directive in directives)
            {
                switch (directive.type)
                {
                    case DirectiveType.BoostGoalPriority:
                        foreach (var goal in goals.Where(g => g.GoalType == directive.targetGoalType))
                        {
                            if (goal is BaseTraversalGoal btg)
                                btg.BasePriority *= directive.value;
                        }
                        break;

                    case DirectiveType.SuppressGoal:
                        goals.RemoveAll(g => g.GoalType == directive.targetGoalType);
                        break;

                    case DirectiveType.ForceGoal:
                        foreach (var goal in goals.Where(g => g.GoalType == directive.targetGoalType))
                        {
                            if (goal is BaseTraversalGoal btg)
                                btg.BasePriority = 10f;
                        }
                        break;

                    case DirectiveType.ModifyDangerTolerance:
                        goalContext.DangerTolerance = Mathf.Clamp01(
                            goalContext.DangerTolerance + directive.value);
                        break;
                }
            }
        }

        /// <summary>Build a BehaviorContext from the current AI state.</summary>
        public BehaviorContext BuildContext(GoalContext goalContext, TraversalDungeonGraph graph)
        {
            var currentNode = graph.GetNode(goalContext.CurrentNodeId);
            var neighbors = graph.GetNeighbors(goalContext.CurrentNodeId);

            return new BehaviorContext
            {
                Health = goalContext.CurrentHealth,
                Resources = goalContext.CurrentResources,
                InventoryFullness = goalContext.InventoryFullness,
                CurrentDangerLevel = currentNode?.BaseDangerLevel ?? 0f,
                ExplorationProgress = goalContext.PerceivedState.VisitedNodeCount /
                    (float)Mathf.Max(1, graph.Nodes.Count),
                NearbyUnexploredCount = neighbors.Count(n => n.HasTag(NodeTag.Unexplored)),
                NearbyEnemyCount = neighbors.Count(n => n.HasTag(NodeTag.EnemyPresence)),
                CurrentNodeTags = currentNode?.Tags ?? NodeTag.None,
                StairsAvailable = neighbors.Any(n => n.HasTag(NodeTag.Staircase)),
                DistanceToSafeZone = CalculateDistanceToSafeZone(goalContext, graph)
            };
        }

        private float CalculateDistanceToSafeZone(GoalContext context, TraversalDungeonGraph graph)
        {
            var safeNodes = graph.GetNodesWithTag(NodeTag.SafeZone);
            if (safeNodes.Count == 0) return 10f;

            var current = graph.GetNode(context.CurrentNodeId);
            if (current == null) return 10f;

            float minDist = float.MaxValue;
            foreach (var safe in safeNodes)
            {
                float dist = Vector3.Distance(current.WorldPosition, safe.WorldPosition);
                if (dist < minDist) minDist = dist;
            }
            return minDist;
        }
    }
}

