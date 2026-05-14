// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Facing & Direction System
// =============================================================================
// Manages agent facing direction, directional hit resolution, and flank detection.
// =============================================================================

using Combat.Core;
using UnityEngine;

namespace Combat.Facing
{
    /// <summary>
    /// Manages the facing direction of a combat agent.
    /// Critical for directional attacks, blocking, flanking, and backstabs.
    /// </summary>
    [RequireComponent(typeof(CombatAgent))]
    public class FacingComponent : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _turnSpeed = 360f;
        [SerializeField] private float _facingArcAngle = 120f; // frontal arc

        [Header("State (Read Only)")]
        [SerializeField] private Vector3 _currentDirection = Vector3.forward;

        private CombatAgent _owner;
        private Vector3 _desiredDirection;
        private bool _facingLocked;

        public Vector3 Direction => _currentDirection;
        public float FacingArcAngle => _facingArcAngle;
        public bool IsLocked => _facingLocked;

        private void Awake()
        {
            _owner = GetComponent<CombatAgent>();
            _currentDirection = transform.forward;
            _desiredDirection = _currentDirection;
        }

        private void Update()
        {
            if (_facingLocked) return;

            // Smoothly rotate towards desired direction
            if (_desiredDirection.sqrMagnitude > 0.01f)
            {
                _currentDirection = Vector3.RotateTowards(
                    _currentDirection, _desiredDirection,
                    _turnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f
                ).normalized;

                // Apply to transform rotation
                if (_currentDirection.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(_currentDirection, Vector3.up);
            }
        }

        /// <summary>Set desired facing direction (smooth turn).</summary>
        public void SetDesiredDirection(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
                _desiredDirection = direction.normalized;
        }

        /// <summary>Set facing instantly.</summary>
        public void SetDirection(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
            {
                _currentDirection = direction.normalized;
                _desiredDirection = _currentDirection;
                transform.rotation = Quaternion.LookRotation(_currentDirection, Vector3.up);
            }
        }

        /// <summary>Face towards a target position.</summary>
        public void FaceTowards(Vector3 targetPosition)
        {
            Vector3 dir = targetPosition - transform.position;
            dir.y = 0;
            SetDesiredDirection(dir);
        }

        /// <summary>Lock facing (during skill execution).</summary>
        public void LockFacing(bool locked)
        {
            _facingLocked = locked;
        }

        /// <summary>Check if a point is within the frontal arc.</summary>
        public bool IsInFrontalArc(Vector3 point)
        {
            Vector3 toPoint = (point - transform.position).normalized;
            toPoint.y = 0;
            float angle = Vector3.Angle(_currentDirection, toPoint);
            return angle <= _facingArcAngle * 0.5f;
        }

        /// <summary>Check if a point is behind this agent.</summary>
        public bool IsBehind(Vector3 point)
        {
            Vector3 toPoint = (point - transform.position).normalized;
            toPoint.y = 0;
            float dot = Vector3.Dot(_currentDirection, toPoint);
            return dot < -0.3f;
        }
    }

    /// <summary>
    /// Resolves hits based on direction for damage/block calculations.
    /// </summary>
    public static class DirectionalHitResolver
    {
        /// <summary>
        /// Calculate the defense modifier based on the attack direction relative to the defender.
        /// </summary>
        public static float GetDefenseModifier(CombatAgent defender, Vector3 attackOrigin)
        {
            if (defender.Facing == null) return 1f;

            Vector3 toAttacker = (attackOrigin - defender.transform.position).normalized;
            var direction = DirectionUtility.GetRelativeDirection(defender.Facing.Direction, toAttacker);

            // Frontal attacks can be blocked (especially in defensive stance)
            if (direction == RelativeDirection.Front)
            {
                if (defender.Stance == CombatStance.Defensive) return 0.4f;
                if (defender.Stance == CombatStance.Braced) return 0.2f;
                if (defender.Weapon != null && defender.Weapon.WeaponType == WeaponType.Shield) return 0.5f;
                return 0.8f;
            }

            // Side attacks partially defended
            if (DirectionUtility.IsFromSide(direction))
                return 1.0f;

            // Rear attacks = full damage + bonus
            if (DirectionUtility.IsFromBehind(direction))
                return 1.5f;

            return 1f;
        }
    }

    /// <summary>
    /// Evaluates flanking opportunities for utility scoring.
    /// </summary>
    public static class FlankEvaluator
    {
        /// <summary>
        /// Check if attacker has a flanking angle on the target.
        /// </summary>
        public static bool HasFlankingAngle(CombatAgent attacker, CombatAgent target)
        {
            if (target.Facing == null) return false;
            Vector3 toAttacker = (attacker.transform.position - target.transform.position).normalized;
            var dir = DirectionUtility.GetRelativeDirection(target.Facing.Direction, toAttacker);
            return DirectionUtility.IsFromSide(dir) || DirectionUtility.IsFromBehind(dir);
        }

        /// <summary>
        /// Check if attacker is directly behind the target.
        /// </summary>
        public static bool HasBackstabAngle(CombatAgent attacker, CombatAgent target)
        {
            if (target.Facing == null) return false;
            Vector3 toAttacker = (attacker.transform.position - target.transform.position).normalized;
            var dir = DirectionUtility.GetRelativeDirection(target.Facing.Direction, toAttacker);
            return DirectionUtility.IsFromBehind(dir);
        }

        /// <summary>
        /// Get the best flanking position for an attacker to reach.
        /// </summary>
        public static Vector3 GetBestFlankPosition(CombatAgent attacker, CombatAgent target, float desiredRange)
        {
            if (target.Facing == null) return target.transform.position;

            // Try to get behind/to the side
            Vector3 targetRight = Vector3.Cross(Vector3.up, target.Facing.Direction).normalized;

            // Choose the side closest to attacker's current position
            Vector3 leftPos = target.transform.position - targetRight * desiredRange;
            Vector3 rightPos = target.transform.position + targetRight * desiredRange;
            Vector3 rearPos = target.transform.position - target.Facing.Direction * desiredRange;

            float distLeft = Vector3.Distance(attacker.transform.position, leftPos);
            float distRight = Vector3.Distance(attacker.transform.position, rightPos);
            float distRear = Vector3.Distance(attacker.transform.position, rearPos);

            if (distRear <= distLeft && distRear <= distRight) return rearPos;
            if (distLeft <= distRight) return leftPos;
            return rightPos;
        }
    }
}

