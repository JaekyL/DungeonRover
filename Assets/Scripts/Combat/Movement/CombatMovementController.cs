// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Combat Movement System
// =============================================================================
// Handles combat-specific movement: repositioning, flanking, retreating,
// interception, and engagement approach. Integrates with traversal pathfinding.
// =============================================================================

using Combat.Core;
using Combat.Facing;
using Combat.Formation;
using UnityEngine;

namespace Combat.Movement
{
    /// <summary>
    /// Combat movement controller. Manages AI movement during combat:
    /// repositioning, flanking, retreating, interception, and approach.
    /// 
    /// Overrides traversal movement when in combat and integrates
    /// with formation positioning.
    /// </summary>
    [RequireComponent(typeof(CombatAgent))]
    public class CombatMovementController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _repositionInterval = 0.5f;
        [SerializeField] private float _arrivalDistance = 0.3f;
        [SerializeField] private float _combatMoveSpeed = 3.5f;

        [Header("State (Read Only)")]
        [SerializeField] private CombatMovementIntent _currentIntent = CombatMovementIntent.Hold;
        [SerializeField] private Vector3 _moveTarget;

        private CombatAgent _agent;
        private float _lastRepositionTime;

        public CombatMovementIntent CurrentIntent => _currentIntent;
        public Vector3 MoveTarget => _moveTarget;

        private void Awake()
        {
            _agent = GetComponent<CombatAgent>();
        }

        private void Update()
        {
            if (!_agent.IsAlive || _agent.State == CombatState.Idle) return;
            if (_agent.State == CombatState.Executing) return; // Skill handles movement

            // Determine movement intent
            if (Time.time - _lastRepositionTime >= _repositionInterval)
            {
                DetermineMovementIntent();
                _lastRepositionTime = Time.time;
            }

            // Execute movement
            ExecuteMovement();
        }

        private void DetermineMovementIntent()
        {
            var context = _agent.Context;
            if (context == null) return;

            var brain = _agent.Brain;
            var decision = brain != null ? brain.CurrentDecision : default;

            // Priority 1: If we have a skill decision but are out of range, approach
            if (decision.HasDecision && decision.Target != null && decision.Target != _agent)
            {
                float dist = Vector3.Distance(transform.position, decision.Target.transform.position);
                float requiredRange = decision.ChosenSkill.TargetingProfile.MaxRange;
                if (dist > requiredRange)
                {
                    _currentIntent = CombatMovementIntent.Approach;
                    _moveTarget = decision.Target.transform.position;
                    return;
                }
            }

            // Priority 2: If being flanked and have no defense, reposition
            if (context.IsBeingFlanked && context.EngagementCount >= 2)
            {
                _currentIntent = CombatMovementIntent.Reposition;
                _moveTarget = FindSaferPosition(context);
                return;
            }

            // Priority 3: If doctrine says flank and opportunity exists
            if (brain?.Doctrine != null && brain.Doctrine.FlankingDesire > 0.5f
                && context.PrimaryTarget != null
                && !FlankEvaluator.HasFlankingAngle(_agent, context.PrimaryTarget))
            {
                _currentIntent = CombatMovementIntent.Flank;
                _moveTarget = FlankEvaluator.GetBestFlankPosition(
                    _agent, context.PrimaryTarget, _agent.WeaponReach);
                return;
            }

            // Priority 4: Maintain preferred distance
            if (context.PrimaryTarget != null)
            {
                float dist = Vector3.Distance(transform.position, context.PrimaryTarget.transform.position);
                float preferred = _agent.Weapon != null ? _agent.Weapon.PreferredDistance : _agent.WeaponReach * 0.8f;

                if (dist < preferred * 0.6f)
                {
                    _currentIntent = CombatMovementIntent.CreateSpace;
                    Vector3 away = (transform.position - context.PrimaryTarget.transform.position).normalized;
                    _moveTarget = transform.position + away * (preferred - dist);
                    return;
                }

                if (dist > preferred * 1.5f)
                {
                    _currentIntent = CombatMovementIntent.Approach;
                    _moveTarget = context.PrimaryTarget.transform.position;
                    return;
                }
            }

            // Priority 5: Formation discipline
            if (context.DistanceToFormationPosition > 2f && context.FormationIntegrity < 0.7f)
            {
                _currentIntent = CombatMovementIntent.ReturnToFormation;
                // FormationController provides target position
                return;
            }

            // Default: Hold position but face enemy
            _currentIntent = CombatMovementIntent.Hold;
            if (context.PrimaryTarget != null && _agent.Facing != null)
            {
                _agent.Facing.FaceTowards(context.PrimaryTarget.transform.position);
            }
        }

        private void ExecuteMovement()
        {
            if (_currentIntent == CombatMovementIntent.Hold) return;

            Vector3 direction = _moveTarget - transform.position;
            direction.y = 0;

            if (direction.magnitude < _arrivalDistance)
            {
                _currentIntent = CombatMovementIntent.Hold;
                return;
            }

            Vector3 moveDir = direction.normalized;
            float speed = _combatMoveSpeed;

            // Exhaustion slowdown
            if (_agent.Stamina != null)
                speed = _agent.Stamina.GetEffectiveMoveSpeed(speed);

            transform.position += moveDir * speed * Time.deltaTime;

            // Face movement direction (unless intent is retreat)
            if (_agent.Facing != null)
            {
                if (_currentIntent == CombatMovementIntent.Retreat ||
                    _currentIntent == CombatMovementIntent.CreateSpace)
                {
                    // Face enemy while backing away
                    if (_agent.Context?.PrimaryTarget != null)
                        _agent.Facing.FaceTowards(_agent.Context.PrimaryTarget.transform.position);
                }
                else
                {
                    _agent.Facing.SetDesiredDirection(moveDir);
                }
            }
        }

        private Vector3 FindSaferPosition(CombatContext context)
        {
            // Find a position with fewer enemies behind us
            // Prefer positions near allies or walls (back protection)
            Vector3 best = transform.position;

            if (context.NearbyAllies.Count > 0)
            {
                // Move toward nearest ally for mutual support
                best = context.NearbyAllies[0].transform.position;
                Vector3 dir = (best - transform.position).normalized;
                return transform.position + dir * 2f;
            }

            // Default: back toward nearest wall
            if (context.DistanceToNearestWall < 5f && context.NearestWallNormal.sqrMagnitude > 0)
            {
                return transform.position - context.NearestWallNormal * 2f;
            }

            return transform.position;
        }

        /// <summary>Force a specific movement intent (used by skill effects).</summary>
        public void ForceIntent(CombatMovementIntent intent, Vector3 target)
        {
            _currentIntent = intent;
            _moveTarget = target;
        }
    }

    /// <summary>
    /// What the combat movement system is trying to accomplish.
    /// </summary>
    public enum CombatMovementIntent
    {
        Hold,              // Stay in position
        Approach,          // Close distance to target
        Retreat,           // Move away from danger
        Flank,             // Get behind/beside target
        CreateSpace,       // Back away from too-close enemy
        Reposition,        // Find tactically better position
        Intercept,         // Move to protect ally
        ReturnToFormation, // Get back to formation slot
        CircleStrafe       // Circle around target
    }
}

