using System;
using UnityEngine;

namespace TraversalAI.BehaviorRules
{
    /// <summary>
    /// An action the AI should take when a behavior rule triggers.
    /// </summary>
    [Serializable]
    public class ActionDirective
    {
        [Tooltip("Type of action to perform.")]
        public DirectiveType type = DirectiveType.BoostGoalPriority;

        [Tooltip("Which goal type this directive affects.")]
        public string targetGoalType = "";

        [Tooltip("Numeric value for the directive.")]
        public float value = 1f;

        [Tooltip("Optional: target node tag for tag-based directives.")]
        public Core.NodeTag targetTag = Core.NodeTag.None;

        public override string ToString() => $"{type}: {targetGoalType} ({value:F1})";
    }

    public enum DirectiveType
    {
        BoostGoalPriority,
        ForceGoal,
        SuppressGoal,
        ChangeStance,
        ModifyDangerTolerance,
        Backtrack
    }
}

