// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Tactical Combat Rules
// =============================================================================
// IF-THEN rule system for combat behaviors.
// Extends the traversal BehaviorRules pattern for combat decisions.
// =============================================================================

using System;
using System.Collections.Generic;
using Combat.Core;
using Combat.Skills;
using UnityEngine;

namespace Combat.AI.Rules
{
    /// <summary>
    /// A combat behavior rule: IF conditions THEN combat action.
    /// Player/designer configurable tactical behaviors.
    /// </summary>
    [Serializable]
    public class CombatRule
    {
        public string RuleName = "New Combat Rule";
        public bool Enabled = true;
        public int Priority;
        public List<CombatCondition> Conditions = new List<CombatCondition>();
        public CombatRuleAction Action = new CombatRuleAction();

        public bool Evaluate(CombatContext context)
        {
            if (!Enabled || Conditions.Count == 0) return false;
            foreach (var condition in Conditions)
            {
                if (!condition.Evaluate(context)) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// A condition for a combat rule. Evaluates context parameters.
    /// </summary>
    [Serializable]
    public class CombatCondition
    {
        public CombatConditionType Type;
        public ComparisonOperator Operator = ComparisonOperator.GreaterThan;
        public float Threshold;
        public int IntThreshold;

        public bool Evaluate(CombatContext context)
        {
            float value = GetValue(context);
            return Compare(value);
        }

        private float GetValue(CombatContext context)
        {
            switch (Type)
            {
                case CombatConditionType.EnemyCount: return context.EnemyCount;
                case CombatConditionType.EngagementCount: return context.EngagementCount;
                case CombatConditionType.HealthRatio: return context.HealthRatio;
                case CombatConditionType.StaminaRatio: return context.StaminaRatio;
                case CombatConditionType.NearestEnemyDistance: return context.NearestEnemyDistance;
                case CombatConditionType.IsInCorridor: return context.IsInCorridor ? 1f : 0f;
                case CombatConditionType.IsInChokepoint: return context.IsInChokepoint ? 1f : 0f;
                case CombatConditionType.IsSurrounded: return context.IsSurrounded ? 1f : 0f;
                case CombatConditionType.HasFlankingOpportunity: return context.HasFlankingOpportunity ? 1f : 0f;
                case CombatConditionType.HasBackstabOpportunity: return context.HasBackstabOpportunity ? 1f : 0f;
                case CombatConditionType.IsBeingFlanked: return context.IsBeingFlanked ? 1f : 0f;
                case CombatConditionType.AllyCount: return context.AllyCount;
                case CombatConditionType.FormationIntegrity: return context.FormationIntegrity;
                case CombatConditionType.IncomingThreat: return context.IncomingThreatLevel;
                case CombatConditionType.NearWall: return context.DistanceToNearestWall < 2f ? 1f : 0f;
                case CombatConditionType.AllyLowHealth: return context.MostVulnerableAlly != null && context.MostVulnerableAlly.HealthRatio < 0.3f ? 1f : 0f;
                default: return 0f;
            }
        }

        private bool Compare(float value)
        {
            switch (Operator)
            {
                case ComparisonOperator.GreaterThan: return value > Threshold;
                case ComparisonOperator.LessThan: return value < Threshold;
                case ComparisonOperator.GreaterOrEqual: return value >= Threshold;
                case ComparisonOperator.LessOrEqual: return value <= Threshold;
                case ComparisonOperator.Equal: return Mathf.Approximately(value, Threshold);
                case ComparisonOperator.NotEqual: return !Mathf.Approximately(value, Threshold);
                default: return false;
            }
        }
    }

    /// <summary>Action to take when a combat rule fires.</summary>
    [Serializable]
    public class CombatRuleAction
    {
        public CombatRuleActionType ActionType;
        public SkillCategory PreferredCategory;
        public CombatStance ForceStance;
        public float PriorityBoost = 1.5f;

        [Tooltip("If set, force this specific skill")]
        public CombatSkillSO ForceSkill;
    }

    // --- Enums ---

    public enum CombatConditionType
    {
        EnemyCount,
        EngagementCount,
        HealthRatio,
        StaminaRatio,
        NearestEnemyDistance,
        IsInCorridor,
        IsInChokepoint,
        IsSurrounded,
        HasFlankingOpportunity,
        HasBackstabOpportunity,
        IsBeingFlanked,
        AllyCount,
        FormationIntegrity,
        IncomingThreat,
        NearWall,
        AllyLowHealth
    }

    public enum ComparisonOperator
    {
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual,
        Equal,
        NotEqual
    }

    public enum CombatRuleActionType
    {
        PreferCategory,    // Boost a skill category
        ForceSkill,        // Force a specific skill
        ForceStance,       // Change stance
        Retreat,           // Disengage and retreat
        ProtectAlly,       // Prioritize ally defense
        HoldPosition,      // Stop moving, defend
        Reposition         // Find better position
    }

    /// <summary>
    /// Evaluates combat rules against context and returns triggered actions.
    /// </summary>
    public class CombatRuleEvaluator
    {
        public List<CombatRuleAction> Evaluate(List<CombatRule> rules, CombatContext context)
        {
            var triggered = new List<CombatRuleAction>();
            // Sort by priority
            rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var rule in rules)
            {
                if (rule.Evaluate(context))
                {
                    triggered.Add(rule.Action);
                }
            }
            return triggered;
        }
    }
}

