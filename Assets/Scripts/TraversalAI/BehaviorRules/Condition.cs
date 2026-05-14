using System;
using UnityEngine;

namespace TraversalAI.BehaviorRules
{
    /// <summary>
    /// A single condition that can be evaluated against the AI's current state.
    /// Inspired by Pillars of Eternity II's gambit conditions.
    /// </summary>
    [Serializable]
    public class Condition
    {
        [Tooltip("What parameter to evaluate.")]
        public ConditionParameter parameter = ConditionParameter.Health;

        [Tooltip("Comparison operator.")]
        public ComparisonOperator comparison = ComparisonOperator.LessThan;

        [Tooltip("Threshold value [0..1] for numeric comparisons.")]
        [Range(0f, 1f)]
        public float threshold = 0.5f;

        [Tooltip("For tag checks: which node tag to check.")]
        public Core.NodeTag tagToCheck = Core.NodeTag.None;

        /// <summary>Evaluate this condition against the given context.</summary>
        public bool Evaluate(BehaviorContext context)
        {
            float value = GetParameterValue(context);
            return Compare(value, threshold);
        }

        private float GetParameterValue(BehaviorContext context)
        {
            switch (parameter)
            {
                case ConditionParameter.Health: return context.Health;
                case ConditionParameter.Resources: return context.Resources;
                case ConditionParameter.InventoryFullness: return context.InventoryFullness;
                case ConditionParameter.DangerLevel: return context.CurrentDangerLevel;
                case ConditionParameter.ExplorationProgress: return context.ExplorationProgress;
                case ConditionParameter.NearbyUnexploredCount:
                    return Mathf.Clamp01(context.NearbyUnexploredCount / 5f);
                case ConditionParameter.NearbyEnemyCount:
                    return Mathf.Clamp01(context.NearbyEnemyCount / 3f);
                case ConditionParameter.DistanceToSafeZone:
                    return Mathf.Clamp01(context.DistanceToSafeZone / 10f);
                case ConditionParameter.CurrentNodeHasTag:
                    return context.CurrentNodeTags.HasFlag(tagToCheck) ? 1f : 0f;
                case ConditionParameter.StairsAvailable:
                    return context.StairsAvailable ? 1f : 0f;
                default: return 0f;
            }
        }

        private bool Compare(float value, float thresh)
        {
            switch (comparison)
            {
                case ComparisonOperator.LessThan: return value < thresh;
                case ComparisonOperator.LessOrEqual: return value <= thresh;
                case ComparisonOperator.GreaterThan: return value > thresh;
                case ComparisonOperator.GreaterOrEqual: return value >= thresh;
                case ComparisonOperator.Equal: return Mathf.Approximately(value, thresh);
                case ComparisonOperator.NotEqual: return !Mathf.Approximately(value, thresh);
                default: return false;
            }
        }

        public override string ToString() => $"{parameter} {comparison} {threshold:F2}";
    }

    public enum ConditionParameter
    {
        Health, Resources, InventoryFullness, DangerLevel,
        ExplorationProgress, NearbyUnexploredCount, NearbyEnemyCount,
        DistanceToSafeZone, CurrentNodeHasTag, StairsAvailable
    }

    public enum ComparisonOperator
    {
        LessThan, LessOrEqual, GreaterThan, GreaterOrEqual, Equal, NotEqual
    }
}

