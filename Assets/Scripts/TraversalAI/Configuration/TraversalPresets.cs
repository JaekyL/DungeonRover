using System.Collections.Generic;
using TraversalAI.BehaviorRules;
using UnityEngine;

namespace TraversalAI.Configuration
{
    /// <summary>
    /// Preset configurations for common exploration styles.
    /// </summary>
    public static class TraversalPresets
    {
        /// <summary>Diver: Goes deep fast, high risk tolerance.</summary>
        public static TraversalProfile CreateDiver()
        {
            var profile = ScriptableObject.CreateInstance<TraversalProfile>();
            profile.profileName = "Diver";
            profile.description = "Rushes deeper into the dungeon, prioritizing stairs and progression.";
            profile.stance = ExplorationStance.Aggressive;
            profile.primaryStrategy = TraversalStrategyType.DepthFirst;
            profile.dangerTolerance = 0.8f;
            profile.progressionWeight = 2f;
            profile.explorationWeight = 0.5f;
            profile.lootWeight = 0.2f;

            profile.behaviorRules = new List<BehaviorRule>
            {
                new BehaviorRule
                {
                    ruleName = "Always Push Forward",
                    order = 0,
                    enabled = true,
                    conditions = new List<Condition>
                    {
                        new Condition
                        {
                            parameter = ConditionParameter.StairsAvailable,
                            comparison = ComparisonOperator.GreaterThan,
                            threshold = 0.5f
                        }
                    },
                    action = new ActionDirective
                    {
                        type = DirectiveType.BoostGoalPriority,
                        targetGoalType = "DescendStairs",
                        value = 3f
                    }
                }
            };

            return profile;
        }

        /// <summary>Hunter: Seeks enemies and challenge.</summary>
        public static TraversalProfile CreateHunter()
        {
            var profile = ScriptableObject.CreateInstance<TraversalProfile>();
            profile.profileName = "Hunter";
            profile.description = "Seeks out danger and enemies. High risk, high reward.";
            profile.stance = ExplorationStance.Aggressive;
            profile.primaryStrategy = TraversalStrategyType.DepthFirst;
            profile.dangerTolerance = 0.9f;
            profile.dangerAvoidanceWeight = 0.1f;
            profile.lootWeight = 1.5f;
            return profile;
        }

        /// <summary>Scavenger: Prioritizes loot and treasure.</summary>
        public static TraversalProfile CreateScavenger()
        {
            var profile = ScriptableObject.CreateInstance<TraversalProfile>();
            profile.profileName = "Scavenger";
            profile.description = "Prioritizes finding and collecting loot.";
            profile.stance = ExplorationStance.Thorough;
            profile.primaryStrategy = TraversalStrategyType.BreadthFirst;
            profile.lootWeight = 2f;
            profile.explorationWeight = 1.2f;
            profile.dangerTolerance = 0.4f;

            profile.behaviorRules = new List<BehaviorRule>
            {
                new BehaviorRule
                {
                    ruleName = "Inventory Full - Descend",
                    order = 0,
                    enabled = true,
                    conditions = new List<Condition>
                    {
                        new Condition
                        {
                            parameter = ConditionParameter.InventoryFullness,
                            comparison = ComparisonOperator.GreaterThan,
                            threshold = 0.9f
                        }
                    },
                    action = new ActionDirective
                    {
                        type = DirectiveType.BoostGoalPriority,
                        targetGoalType = "DescendStairs",
                        value = 5f
                    }
                }
            };

            return profile;
        }

        /// <summary>Cartographer: Maximizes map coverage.</summary>
        public static TraversalProfile CreateCartographer()
        {
            var profile = ScriptableObject.CreateInstance<TraversalProfile>();
            profile.profileName = "Cartographer";
            profile.description = "Systematically explores every corner of the dungeon.";
            profile.stance = ExplorationStance.Thorough;
            profile.primaryStrategy = TraversalStrategyType.SectorSweep;
            profile.explorationWeight = 2f;
            profile.progressionWeight = 0.1f;
            profile.lootWeight = 0.3f;
            profile.dangerTolerance = 0.5f;
            return profile;
        }

        /// <summary>Ghost: Stealthy, avoids danger.</summary>
        public static TraversalProfile CreateGhost()
        {
            var profile = ScriptableObject.CreateInstance<TraversalProfile>();
            profile.profileName = "Ghost";
            profile.description = "Avoids all danger, sneaks through the dungeon silently.";
            profile.stance = ExplorationStance.Sneaky;
            profile.primaryStrategy = TraversalStrategyType.SafeRadius;
            profile.dangerTolerance = 0.15f;
            profile.dangerAvoidanceWeight = 2f;
            profile.safetyWeight = 2f;
            profile.explorationWeight = 0.8f;

            profile.behaviorRules = new List<BehaviorRule>
            {
                new BehaviorRule
                {
                    ruleName = "Avoid Enemies Always",
                    order = 0,
                    enabled = true,
                    conditions = new List<Condition>
                    {
                        new Condition
                        {
                            parameter = ConditionParameter.NearbyEnemyCount,
                            comparison = ComparisonOperator.GreaterThan,
                            threshold = 0f
                        }
                    },
                    action = new ActionDirective
                    {
                        type = DirectiveType.ForceGoal,
                        targetGoalType = "AvoidThreat"
                    }
                }
            };

            return profile;
        }
    }
}

