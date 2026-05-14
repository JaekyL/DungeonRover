// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Skill Effect System
// =============================================================================
// Base ScriptableObject for skill effects. Effects are applied when a skill hits.
// Concrete implementations handle damage, displacement, CC, etc.
// =============================================================================

using Combat.Core;
using UnityEngine;

namespace Combat.Skills.Effects
{
    /// <summary>
    /// Context passed to skill effects when they are applied.
    /// </summary>
    public struct SkillEffectContext
    {
        public CombatAgent Caster;
        public CombatAgent Target;
        public CombatSkillSO Skill;
        public RelativeDirection HitDirection;
        public Vector3 SkillDirection;
        public Vector3 HitPoint;
    }

    /// <summary>
    /// Base class for all skill effects. Create specific effects as ScriptableObjects.
    /// </summary>
    public abstract class SkillEffectSO : ScriptableObject
    {
        [SerializeField] protected string _effectName = "Effect";
        [SerializeField, TextArea] protected string _description;

        public string EffectName => _effectName;

        /// <summary>Apply this effect given the context.</summary>
        public abstract void Apply(SkillEffectContext context);
    }

    // --- Concrete Effect Implementations ---

    /// <summary>Deals direct damage based on weapon + skill modifiers.</summary>
    [CreateAssetMenu(fileName = "DamageEffect", menuName = "Combat/Effects/Damage")]
    public class DamageEffectSO : SkillEffectSO
    {
        [Header("Damage")]
        [SerializeField] private float _baseDamage = 20f;
        [SerializeField] private float _weaponDamageMultiplier = 1f;
        [SerializeField] private float _backstabMultiplier = 1.5f;
        [SerializeField] private float _flankMultiplier = 1.25f;

        public override void Apply(SkillEffectContext context)
        {
            float damage = _baseDamage;

            // Weapon scaling
            if (context.Caster.Weapon != null)
                damage += context.Caster.Weapon.BaseDamage * _weaponDamageMultiplier;

            // Positional multipliers
            if (DirectionUtility.IsFromBehind(context.HitDirection))
                damage *= _backstabMultiplier;
            else if (DirectionUtility.IsFromSide(context.HitDirection))
                damage *= _flankMultiplier;

            context.Target.TakeDamage(damage, context.Caster, context.HitDirection);
        }
    }

    /// <summary>Pushes the target in a direction (knockback, push).</summary>
    [CreateAssetMenu(fileName = "DisplacementEffect", menuName = "Combat/Effects/Displacement")]
    public class DisplacementEffectSO : SkillEffectSO
    {
        [Header("Displacement")]
        [SerializeField] private float _force = 3f;
        [SerializeField] private bool _useSkillDirection = true;
        [SerializeField] private bool _awayFromCaster = false;

        public override void Apply(SkillEffectContext context)
        {
            Vector3 dir;
            if (_awayFromCaster)
                dir = (context.Target.transform.position - context.Caster.transform.position).normalized;
            else if (_useSkillDirection)
                dir = context.SkillDirection;
            else
                dir = -context.Target.Facing.Direction; // Push backward

            dir.y = 0;
            context.Target.ApplyDisplacement(dir * _force, context.Caster);
        }
    }

    /// <summary>Stuns the target for a duration.</summary>
    [CreateAssetMenu(fileName = "StunEffect", menuName = "Combat/Effects/Stun")]
    public class StunEffectSO : SkillEffectSO
    {
        [Header("Stun")]
        [SerializeField] private float _duration = 1f;
        [SerializeField] private bool _longerFromBehind = true;

        public override void Apply(SkillEffectContext context)
        {
            float duration = _duration;
            if (_longerFromBehind && DirectionUtility.IsFromBehind(context.HitDirection))
                duration *= 1.5f;

            context.Target.ApplyStun(duration);
        }
    }

    /// <summary>Changes the target's stance.</summary>
    [CreateAssetMenu(fileName = "StanceChangeEffect", menuName = "Combat/Effects/Stance Change")]
    public class StanceChangeEffectSO : SkillEffectSO
    {
        [Header("Stance")]
        [SerializeField] private CombatStance _newStance = CombatStance.Defensive;
        [SerializeField] private bool _applySelf = true;

        public override void Apply(SkillEffectContext context)
        {
            var target = _applySelf ? context.Caster : context.Target;
            target.SetStance(_newStance);
        }
    }

    /// <summary>Drains stamina from the target (guard break).</summary>
    [CreateAssetMenu(fileName = "StaminaDrainEffect", menuName = "Combat/Effects/Stamina Drain")]
    public class StaminaDrainEffectSO : SkillEffectSO
    {
        [Header("Drain")]
        [SerializeField] private float _drainAmount = 30f;

        public override void Apply(SkillEffectContext context)
        {
            context.Target.ConsumeStamina(_drainAmount);
        }
    }

    /// <summary>Interrupts the target's current skill execution.</summary>
    [CreateAssetMenu(fileName = "InterruptEffect", menuName = "Combat/Effects/Interrupt")]
    public class InterruptEffectSO : SkillEffectSO
    {
        public override void Apply(SkillEffectContext context)
        {
            if (context.Target.SkillExecutor.IsExecuting)
            {
                context.Target.SkillExecutor.InterruptCurrent();
            }
        }
    }

    /// <summary>Pulls the target towards the caster.</summary>
    [CreateAssetMenu(fileName = "PullEffect", menuName = "Combat/Effects/Pull")]
    public class PullEffectSO : SkillEffectSO
    {
        [Header("Pull")]
        [SerializeField] private float _pullDistance = 2f;

        public override void Apply(SkillEffectContext context)
        {
            Vector3 dir = (context.Caster.transform.position - context.Target.transform.position).normalized;
            dir.y = 0;
            context.Target.ApplyDisplacement(dir * _pullDistance, context.Caster);
        }
    }
}

