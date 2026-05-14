// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Terrain Interaction System
// =============================================================================
// Combat interactions with dungeon geometry: wall slams, cliff pushes,
// corridor bonuses, doorway holds, and environmental hazards.
// =============================================================================

using Combat.Core;
using Combat.Spatial;
using UnityEngine;

namespace Combat.Terrain
{
    /// <summary>
    /// Evaluates terrain-based combat bonuses and interactions.
    /// Used by utility scoring and skill effects.
    /// </summary>
    public static class TerrainCombatEvaluator
    {
        /// <summary>
        /// Get terrain-based damage bonus for a skill hitting a target at a location.
        /// </summary>
        public static float GetTerrainDamageBonus(CombatAgent target, Vector3 pushDirection)
        {
            float bonus = 0f;

            if (SpatialCombatSystem.Instance == null) return bonus;
            var data = SpatialCombatSystem.Instance.GetSpatialData(target);

            // Wall slam bonus - if push direction leads into a wall
            if (data.NearestWallDistance < 1.5f)
            {
                // Check if push direction is toward the wall
                float dot = Vector3.Dot(pushDirection.normalized, -data.NearestWallNormal);
                if (dot > 0.5f)
                    bonus += 0.5f; // 50% bonus damage for wall slam
            }

            return bonus;
        }

        /// <summary>
        /// Check if pushing a target in a direction would result in a cliff fall.
        /// </summary>
        public static bool WouldFallOffCliff(CombatAgent target, Vector3 pushDirection, float pushDistance)
        {
            if (SpatialCombatSystem.Instance == null) return false;
            var data = SpatialCombatSystem.Instance.GetSpatialData(target);

            // If cliff is nearby and push direction is toward it
            if (data.NearestCliffDistance < pushDistance + 1f)
            {
                // Simple check - would need actual cliff direction for accuracy
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get terrain tactical value for holding a position.
        /// Used by utility AI to evaluate defensive positioning.
        /// </summary>
        public static float GetPositionDefensiveValue(Vector3 position)
        {
            if (SpatialCombatSystem.Instance == null) return 0.5f;

            float value = 0.5f;
            var feature = SpatialCombatSystem.Instance.GetTerrainFeatureAt(position);

            switch (feature)
            {
                case TerrainFeature.Doorway: value = 1.0f; break;   // Best choke
                case TerrainFeature.Corridor: value = 0.8f; break;  // Good choke
                case TerrainFeature.Wall: value = 0.6f; break;      // Back protection
                case TerrainFeature.OpenSpace: value = 0.3f; break; // Exposed
                case TerrainFeature.Chokepoint: value = 0.9f; break;
            }

            return value;
        }

        /// <summary>
        /// Get terrain-based skill appropriateness boost.
        /// A corridor makes thrust attacks better, open space makes sweeps better, etc.
        /// </summary>
        public static float GetTerrainSkillBonus(TerrainFeature terrain, SkillCategory category, AttackShape shape)
        {
            switch (terrain)
            {
                case TerrainFeature.Corridor:
                    if (shape == AttackShape.Line) return 0.5f;      // Thrusts excel
                    if (shape == AttackShape.Cone && category == SkillCategory.AttackGeometry) return -0.2f; // Sweeps worse
                    break;

                case TerrainFeature.OpenSpace:
                    if (shape == AttackShape.Circle) return 0.3f;    // Spin attacks good
                    if (shape == AttackShape.Cone) return 0.2f;      // Sweeps good
                    break;

                case TerrainFeature.Doorway:
                    if (category == SkillCategory.Defensive) return 0.5f;  // Defense excellent
                    if (shape == AttackShape.Line) return 0.3f;            // Thrust good
                    break;

                case TerrainFeature.Wall:
                    if (category == SkillCategory.Positional) return 0.3f; // Wall slam opportunity
                    break;

                case TerrainFeature.Cliff:
                    if (category == SkillCategory.CrowdManipulation) return 0.5f; // Push off!
                    break;
            }

            return 0f;
        }
    }
}

