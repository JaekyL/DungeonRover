using System;
using System.Collections.Generic;
using UnityEngine;

namespace TraversalAI.BehaviorRules
{
    /// <summary>
    /// A single configurable behavior rule: IF conditions THEN action.
    /// Players reorder these to define AI priorities.
    /// </summary>
    [Serializable]
    public class BehaviorRule
    {
        [Tooltip("Display name for this rule.")]
        public string ruleName = "New Rule";

        [Tooltip("Whether this rule is active.")]
        public bool enabled = true;

        [Tooltip("Priority order (lower = evaluated first).")]
        public int order;

        [Tooltip("All conditions must be true for the rule to fire (AND logic).")]
        public List<Condition> conditions = new List<Condition>();

        [Tooltip("Action to perform when all conditions are met.")]
        public ActionDirective action = new ActionDirective();

        /// <summary>Evaluate all conditions. Returns true if all conditions pass.</summary>
        public bool Evaluate(BehaviorContext context)
        {
            if (!enabled || conditions.Count == 0) return false;
            foreach (var condition in conditions)
            {
                if (!condition.Evaluate(context))
                    return false;
            }
            return true;
        }

        public override string ToString() => $"[{(enabled ? "ON" : "OFF")}] {ruleName}: {action}";
    }
}

