// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Combat Skill ScriptableObject
// =============================================================================
// Core ScriptableObject definition for all melee combat skills.
// Fully data-driven: designers create new skills without touching code.
// =============================================================================

using System.Collections.Generic;
using Combat.Core;
using Combat.Skills.Conditions;
using Combat.Skills.Effects;
using UnityEngine;

namespace Combat.Skills
{
    /// <summary>
    /// ScriptableObject defining a complete melee combat skill.
    /// Skills are data-driven, equippable, and evaluated by utility AI.
    /// 
    /// Design Philosophy:
    /// - Skills manipulate SPACE, not just deal damage
    /// - Skills have COMMITMENT (windups, recovery)
    /// - Skills interact with TERRAIN and POSITIONING
    /// - Skills affect ENGAGEMENT geometry
    /// </summary>
    [CreateAssetMenu(fileName = "NewCombatSkill", menuName = "Combat/Skill")]
    public class CombatSkillSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _skillName = "New Skill";
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private SkillCategory _category;

        [Header("Targeting")]
        [SerializeField] private SkillTargetingProfile _targetingProfile;

        [Header("Timing (seconds)")]
        [SerializeField] private float _windupDuration = 0.3f;
        [SerializeField] private float _activeDuration = 0.2f;
        [SerializeField] private float _recoveryDuration = 0.4f;
        [SerializeField] private bool _interruptibleDuringWindup = true;
        [SerializeField] private bool _interruptibleDuringRecovery = false;

        [Header("Cost")]
        [SerializeField] private float _staminaCost = 15f;
        [SerializeField] private float _cooldownDuration = 1f;

        [Header("Movement During Skill")]
        [SerializeField] private SkillMovementProfile _movementProfile;

        [Header("Effects (executed on hit/activation)")]
        [SerializeField] private List<SkillEffectSO> _effects = new List<SkillEffectSO>();

        [Header("Conditions (must all be met to use)")]
        [SerializeField] private List<SkillConditionSO> _conditions = new List<SkillConditionSO>();

        [Header("Weapon Restrictions")]
        [SerializeField] private List<WeaponType> _allowedWeapons = new List<WeaponType>();
        [SerializeField] private bool _anyWeaponAllowed = true;

        [Header("Utility AI Scoring Hints")]
        [SerializeField] private SkillUtilityProfile _utilityProfile;

        [Header("Animation")]
        [SerializeField] private SkillAnimationProfile _animationProfile;

        // --- Public Properties ---
        public string SkillName => _skillName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public SkillCategory Category => _category;
        public SkillTargetingProfile TargetingProfile => _targetingProfile;
        public float WindupDuration => _windupDuration;
        public float ActiveDuration => _activeDuration;
        public float RecoveryDuration => _recoveryDuration;
        public float TotalDuration => _windupDuration + _activeDuration + _recoveryDuration;
        public bool InterruptibleDuringWindup => _interruptibleDuringWindup;
        public bool InterruptibleDuringRecovery => _interruptibleDuringRecovery;
        public float StaminaCost => _staminaCost;
        public float CooldownDuration => _cooldownDuration;
        public SkillMovementProfile MovementProfile => _movementProfile;
        public IReadOnlyList<SkillEffectSO> Effects => _effects;
        public IReadOnlyList<SkillConditionSO> Conditions => _conditions;
        public SkillUtilityProfile UtilityProfile => _utilityProfile;
        public SkillAnimationProfile AnimationProfile => _animationProfile;

        /// <summary>
        /// Check if this skill can be executed given the current combat context.
        /// </summary>
        public bool CanExecute(CombatContext context)
        {
            // Stamina check
            if (context.CurrentStamina < _staminaCost) return false;

            // Stance check
            if (context.CurrentStance == CombatStance.Exhausted) return false;
            if (context.CurrentStance == CombatStance.Stunned) return false;

            // Weapon restriction check
            if (!_anyWeaponAllowed && _allowedWeapons.Count > 0)
            {
                if (!_allowedWeapons.Contains(context.EquippedWeapon))
                    return false;
            }

            // Custom conditions
            foreach (var condition in _conditions)
            {
                if (condition != null && !condition.IsMet(context))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get all agents hit by this skill from a given position/direction.
        /// </summary>
        public List<CombatAgent> GetTargetsInArea(Vector3 origin, Vector3 direction, CombatAgent caster)
        {
            var hits = new List<CombatAgent>();
            float range = _targetingProfile.MaxRange;
            float angle = _targetingProfile.ArcAngle;

            foreach (var agent in CombatAgentRegistry.GetAgentsInRange(origin, range))
            {
                if (agent == caster) continue;

                // Faction filter
                bool isEnemy = caster.IsHostileTo(agent);
                bool isAlly = caster.IsAlliedWith(agent);
                if (_targetingProfile.TargetsEnemies && !isEnemy) continue;
                if (_targetingProfile.TargetsAllies && !isAlly) continue;

                // Shape filter
                Vector3 toTarget = agent.transform.position - origin;
                toTarget.y = 0;

                switch (_targetingProfile.Shape)
                {
                    case AttackShape.Circle:
                        hits.Add(agent); // Already filtered by range
                        break;

                    case AttackShape.Cone:
                    case AttackShape.Arc:
                        float targetAngle = Vector3.Angle(direction, toTarget.normalized);
                        if (targetAngle <= angle * 0.5f)
                            hits.Add(agent);
                        break;

                    case AttackShape.Line:
                        // Check if target is within a narrow corridor along direction
                        float dot = Vector3.Dot(toTarget.normalized, direction.normalized);
                        float perpDist = Vector3.Cross(direction.normalized, toTarget).magnitude;
                        if (dot > 0 && perpDist < _targetingProfile.Width * 0.5f)
                            hits.Add(agent);
                        break;

                    case AttackShape.Point:
                        // Single target only
                        if (toTarget.magnitude <= range)
                            hits.Add(agent);
                        break;

                    case AttackShape.Rectangle:
                        float fwdDist = Vector3.Dot(toTarget, direction.normalized);
                        float sideDist = Mathf.Abs(Vector3.Dot(toTarget, Vector3.Cross(Vector3.up, direction).normalized));
                        if (fwdDist > 0 && fwdDist <= range && sideDist <= _targetingProfile.Width * 0.5f)
                            hits.Add(agent);
                        break;
                }
            }

            return hits;
        }
    }

    // --- Supporting Data Structures ---

    /// <summary>
    /// Defines how a skill selects and validates targets.
    /// </summary>
    [System.Serializable]
    public class SkillTargetingProfile
    {
        [Tooltip("Attack shape geometry")]
        public AttackShape Shape = AttackShape.Cone;

        [Tooltip("Maximum range from caster")]
        public float MaxRange = 2f;

        [Tooltip("Minimum range (for ranged only)")]
        public float MinRange = 0f;

        [Tooltip("Width for line/rectangle shapes")]
        public float Width = 1f;

        [Tooltip("Arc angle for cone/arc shapes (full angle)")]
        public float ArcAngle = 90f;

        [Tooltip("Can target enemies")]
        public bool TargetsEnemies = true;

        [Tooltip("Can target allies")]
        public bool TargetsAllies = false;

        [Tooltip("Is self-targeted (buffs, stances)")]
        public bool IsSelfTargeted = false;

        [Tooltip("Required relative position of target")]
        public RelativeDirection? RequiredDirection = null;
    }

    /// <summary>
    /// Defines movement applied to the caster during skill execution.
    /// </summary>
    [System.Serializable]
    public class SkillMovementProfile
    {
        [Tooltip("Does this skill move the caster?")]
        public bool HasMovement = false;

        [Tooltip("Movement distance during active phase")]
        public float MovementDistance = 0f;

        [Tooltip("Direction relative to facing (1=forward, -1=backward)")]
        public Vector3 MovementDirection = Vector3.forward;

        [Tooltip("Movement speed multiplier")]
        public float SpeedMultiplier = 1f;

        [Tooltip("Does movement lock facing?")]
        public bool LocksFacing = true;

        [Tooltip("Can the skill's movement push through enemies?")]
        public bool PushesThroughEnemies = false;
    }

    /// <summary>
    /// Hints for the utility AI to score this skill appropriately.
    /// Higher values = skill prefers that situation.
    /// </summary>
    [System.Serializable]
    public class SkillUtilityProfile
    {
        [Header("Situational Preferences [0-1]")]
        [Range(0, 1)] public float PreferWhenSurrounded = 0f;
        [Range(0, 1)] public float PreferInCorridor = 0f;
        [Range(0, 1)] public float PreferInOpenSpace = 0f;
        [Range(0, 1)] public float PreferWhenFlanking = 0f;
        [Range(0, 1)] public float PreferWhenBehindTarget = 0f;
        [Range(0, 1)] public float PreferAgainstDistractedTarget = 0f;
        [Range(0, 1)] public float PreferNearWall = 0f;
        [Range(0, 1)] public float PreferNearCliff = 0f;
        [Range(0, 1)] public float PreferLowEnemyStamina = 0f;
        [Range(0, 1)] public float PreferHighOwnStamina = 0f;
        [Range(0, 1)] public float PreferAllyInDanger = 0f;
        [Range(0, 1)] public float PreferChokepoint = 0f;

        [Header("Base Priority")]
        [Range(0, 2)] public float BasePriority = 1f;

        [Header("Risk/Reward")]
        [Range(0, 1)] public float CommitmentLevel = 0.3f;
        [Range(0, 1)] public float RiskLevel = 0.3f;
    }

    /// <summary>
    /// Animation-related data for skill execution.
    /// Decoupled from gameplay logic for flexibility.
    /// </summary>
    [System.Serializable]
    public class SkillAnimationProfile
    {
        public string AnimationTrigger = "Attack";
        public string WindupAnimation = "";
        public string ActiveAnimation = "";
        public string RecoveryAnimation = "";
        public bool UseRootMotion = false;
        public float AnimationSpeed = 1f;
    }
}

