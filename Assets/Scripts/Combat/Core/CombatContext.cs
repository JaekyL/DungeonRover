// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Combat Context
// =============================================================================
// Provides snapshot data about the current tactical situation for an agent.
// Used by utility AI, skill selection, and behavioral rules for decision-making.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace Combat.Core
{
    /// <summary>
    /// Immutable snapshot of the tactical situation around a combat agent.
    /// Rebuilt each decision cycle. Designed for ECS-compatible data layout.
    /// </summary>
    [System.Serializable]
    public class CombatContext
    {
        // --- Self ---
        public CombatAgent Self;
        public Vector3 Position;
        public Vector3 FacingDirection;
        public CombatStance CurrentStance;
        public CombatState CurrentState;
        public float CurrentStamina;
        public float MaxStamina;
        public float StaminaRatio => MaxStamina > 0 ? CurrentStamina / MaxStamina : 0f;
        public float CurrentHealth;
        public float MaxHealth;
        public float HealthRatio => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
        public WeaponType EquippedWeapon;
        public float WeaponReach;

        // --- Engagement ---
        public List<CombatAgent> EngagedEnemies = new List<CombatAgent>();
        public CombatAgent PrimaryTarget;
        public int EngagementCount => EngagedEnemies.Count;
        public bool IsEngaged => EngagementCount > 0;
        public bool IsSurrounded => EngagementCount >= 3;

        // --- Spatial Awareness ---
        public List<CombatAgent> NearbyAllies = new List<CombatAgent>();
        public List<CombatAgent> NearbyEnemies = new List<CombatAgent>();
        public int AllyCount => NearbyAllies.Count;
        public int EnemyCount => NearbyEnemies.Count;
        public float NearestEnemyDistance = float.MaxValue;
        public float NearestAllyDistance = float.MaxValue;
        public float LocalEnemyDensity; // enemies within weapon reach
        public float LocalAllyDensity;

        // --- Terrain ---
        public TerrainFeature NearestTerrainFeature = TerrainFeature.None;
        public float DistanceToNearestWall = float.MaxValue;
        public float DistanceToNearestCliff = float.MaxValue;
        public bool IsInCorridor;
        public bool IsInDoorway;
        public bool IsInChokepoint;
        public bool IsInOpenSpace;
        public Vector3 NearestWallNormal;

        // --- Positioning ---
        public bool HasFlankingOpportunity;
        public bool HasBackstabOpportunity;
        public bool IsBeingFlanked;
        public bool IsExposed; // no allies nearby
        public float FormationIntegrity; // 0=broken, 1=perfect
        public FormationRole CurrentFormationRole;

        // --- Threat ---
        public float IncomingThreatLevel; // aggregate threat facing self
        public float OutgoingThreatLevel; // how threatening self is
        public CombatAgent HighestThreatEnemy;
        public CombatAgent MostVulnerableAlly;

        // --- Timing ---
        public float TimeSinceCombatStart;
        public float TimeSinceLastAction;
        public float TimeSinceLastHit; // time since self was last hit

        // --- Escape & Movement ---
        public bool HasEscapeRoute;
        public Vector3 BestEscapeDirection;
        public float DistanceToFormationPosition;

        /// <summary>
        /// Get the relative direction from self to a target.
        /// </summary>
        public RelativeDirection GetRelativeDirectionTo(CombatAgent target)
        {
            if (target == null) return RelativeDirection.Front;
            Vector3 toTarget = (target.transform.position - Position).normalized;
            return DirectionUtility.GetRelativeDirection(FacingDirection, toTarget);
        }

        /// <summary>
        /// Get the relative direction FROM which an attacker approaches.
        /// </summary>
        public RelativeDirection GetAttackingDirectionFrom(CombatAgent attacker)
        {
            if (attacker == null) return RelativeDirection.Front;
            Vector3 fromAttacker = (attacker.transform.position - Position).normalized;
            return DirectionUtility.GetRelativeDirection(FacingDirection, fromAttacker);
        }
    }

    /// <summary>
    /// Utility class for directional calculations.
    /// </summary>
    public static class DirectionUtility
    {
        /// <summary>
        /// Determine relative direction based on facing and incoming direction.
        /// </summary>
        public static RelativeDirection GetRelativeDirection(Vector3 facing, Vector3 toTarget)
        {
            float angle = Vector3.SignedAngle(facing, toTarget, Vector3.up);

            if (angle >= -22.5f && angle < 22.5f) return RelativeDirection.Front;
            if (angle >= 22.5f && angle < 67.5f) return RelativeDirection.FrontRight;
            if (angle >= 67.5f && angle < 112.5f) return RelativeDirection.Right;
            if (angle >= 112.5f && angle < 157.5f) return RelativeDirection.RearRight;
            if (angle >= -67.5f && angle < -22.5f) return RelativeDirection.FrontLeft;
            if (angle >= -112.5f && angle < -67.5f) return RelativeDirection.Left;
            if (angle >= -157.5f && angle < -112.5f) return RelativeDirection.RearLeft;
            return RelativeDirection.Rear;
        }

        /// <summary>
        /// Check if a direction is considered "from behind" (flanking/backstab eligible).
        /// </summary>
        public static bool IsFromBehind(RelativeDirection dir)
        {
            return dir == RelativeDirection.Rear ||
                   dir == RelativeDirection.RearLeft ||
                   dir == RelativeDirection.RearRight;
        }

        /// <summary>
        /// Check if a direction is to the side (flank eligible).
        /// </summary>
        public static bool IsFromSide(RelativeDirection dir)
        {
            return dir == RelativeDirection.Left ||
                   dir == RelativeDirection.Right ||
                   dir == RelativeDirection.FrontLeft ||
                   dir == RelativeDirection.FrontRight;
        }
    }
}

