// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Skill Conditions
// =============================================================================
// ScriptableObject conditions that must be met for a skill to be usable.
// Enables data-driven skill gating (positional, terrain, situational).
// =============================================================================

using Combat.Core;
using UnityEngine;

namespace Combat.Skills.Conditions
{
    /// <summary>
    /// Base class for skill conditions. All conditions must be met for skill execution.
    /// </summary>
    public abstract class SkillConditionSO : ScriptableObject
    {
        [SerializeField] protected string _conditionName = "Condition";
        [SerializeField] protected bool _invertResult = false;

        public string ConditionName => _conditionName;

        public bool IsMet(CombatContext context)
        {
            bool result = Evaluate(context);
            return _invertResult ? !result : result;
        }

        protected abstract bool Evaluate(CombatContext context);
    }

    /// <summary>Requires the caster to be behind the target.</summary>
    [CreateAssetMenu(fileName = "BehindTargetCondition", menuName = "Combat/Conditions/Behind Target")]
    public class BehindTargetConditionSO : SkillConditionSO
    {
        protected override bool Evaluate(CombatContext context)
        {
            if (context.PrimaryTarget == null) return false;
            var dir = context.GetRelativeDirectionTo(context.PrimaryTarget);
            // We are behind them if they face away from us
            Vector3 targetFacing = context.PrimaryTarget.Facing != null
                ? context.PrimaryTarget.Facing.Direction
                : context.PrimaryTarget.transform.forward;
            Vector3 toUs = (context.Position - context.PrimaryTarget.transform.position).normalized;
            float dot = Vector3.Dot(targetFacing, toUs);
            return dot < -0.3f; // Behind the target
        }
    }

    /// <summary>Requires being near a wall.</summary>
    [CreateAssetMenu(fileName = "NearWallCondition", menuName = "Combat/Conditions/Near Wall")]
    public class NearWallConditionSO : SkillConditionSO
    {
        [SerializeField] private float _maxWallDistance = 2f;

        protected override bool Evaluate(CombatContext context)
        {
            return context.DistanceToNearestWall <= _maxWallDistance;
        }
    }

    /// <summary>Requires being in a corridor.</summary>
    [CreateAssetMenu(fileName = "InCorridorCondition", menuName = "Combat/Conditions/In Corridor")]
    public class InCorridorConditionSO : SkillConditionSO
    {
        protected override bool Evaluate(CombatContext context)
        {
            return context.IsInCorridor;
        }
    }

    /// <summary>Requires minimum stamina percentage.</summary>
    [CreateAssetMenu(fileName = "MinStaminaCondition", menuName = "Combat/Conditions/Min Stamina")]
    public class MinStaminaConditionSO : SkillConditionSO
    {
        [SerializeField, Range(0, 1)] private float _minStaminaRatio = 0.5f;

        protected override bool Evaluate(CombatContext context)
        {
            return context.StaminaRatio >= _minStaminaRatio;
        }
    }

    /// <summary>Requires being engaged with N enemies.</summary>
    [CreateAssetMenu(fileName = "EngagementCountCondition", menuName = "Combat/Conditions/Engagement Count")]
    public class EngagementCountConditionSO : SkillConditionSO
    {
        [SerializeField] private int _minEngaged = 2;
        [SerializeField] private int _maxEngaged = 10;

        protected override bool Evaluate(CombatContext context)
        {
            return context.EngagementCount >= _minEngaged && context.EngagementCount <= _maxEngaged;
        }
    }

    /// <summary>Requires a specific stance.</summary>
    [CreateAssetMenu(fileName = "StanceCondition", menuName = "Combat/Conditions/Stance Required")]
    public class StanceConditionSO : SkillConditionSO
    {
        [SerializeField] private CombatStance _requiredStance = CombatStance.Defensive;

        protected override bool Evaluate(CombatContext context)
        {
            return context.CurrentStance == _requiredStance;
        }
    }

    /// <summary>Requires the target to be executing a skill (for counter-attacks).</summary>
    [CreateAssetMenu(fileName = "TargetExecutingCondition", menuName = "Combat/Conditions/Target Executing")]
    public class TargetExecutingConditionSO : SkillConditionSO
    {
        protected override bool Evaluate(CombatContext context)
        {
            return context.PrimaryTarget != null &&
                   context.PrimaryTarget.SkillExecutor.IsExecuting;
        }
    }

    /// <summary>Requires being near a cliff edge.</summary>
    [CreateAssetMenu(fileName = "NearCliffCondition", menuName = "Combat/Conditions/Near Cliff")]
    public class NearCliffConditionSO : SkillConditionSO
    {
        [SerializeField] private float _maxCliffDistance = 3f;

        protected override bool Evaluate(CombatContext context)
        {
            return context.DistanceToNearestCliff <= _maxCliffDistance;
        }
    }
}

