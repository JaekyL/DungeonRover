// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Combat Utility Scorer
// =============================================================================
// Evaluates the utility of each skill in the current tactical situation.
// Uses the skill's UtilityProfile + contextual modifiers to determine best action.
// =============================================================================

using Combat.Core;
using Combat.Skills;
using UnityEngine;

namespace Combat.AI.Utility
{
    /// <summary>
    /// Scores combat skills based on tactical context.
    /// The core of autonomous combat decision-making.
    /// 
    /// Scoring Philosophy:
    /// - Positional skills score higher when position is favorable
    /// - AoE skills score higher when enemies are clustered
    /// - Defensive skills score higher when threatened
    /// - Commitment skills score higher when stamina is abundant
    /// - Spatial manipulation scores higher near terrain features
    /// </summary>
    public class CombatUtilityScorer
    {
        /// <summary>
        /// Score a skill for a specific target in the current context.
        /// Returns [0..∞) where higher = more desirable.
        /// </summary>
        public float ScoreSkill(CombatSkillSO skill, CombatContext context, CombatAgent target,
            CombatDoctrineSO doctrine)
        {
            if (skill == null || context == null) return 0f;

            var profile = skill.UtilityProfile;
            if (profile == null) return skill.StaminaCost > 0 ? 0.5f : 0f;

            float score = profile.BasePriority;

            // --- Situational Modifiers ---

            // Surrounded bonus
            if (context.IsSurrounded)
                score += profile.PreferWhenSurrounded * 1.5f;
            else if (context.EngagementCount >= 2)
                score += profile.PreferWhenSurrounded * 0.5f;

            // Corridor bonus
            if (context.IsInCorridor)
                score += profile.PreferInCorridor * 1.2f;

            // Open space bonus
            if (context.IsInOpenSpace)
                score += profile.PreferInOpenSpace * 0.8f;

            // Chokepoint bonus
            if (context.IsInChokepoint)
                score += profile.PreferChokepoint * 1.3f;

            // Flanking/Behind target
            if (target != null && target != context.Self)
            {
                var relDir = GetAttackDirection(context, target);
                if (DirectionUtility.IsFromBehind(relDir))
                    score += profile.PreferWhenBehindTarget * 1.5f;
                else if (DirectionUtility.IsFromSide(relDir))
                    score += profile.PreferWhenFlanking * 1.0f;

                // Target distracted (executing a skill)
                if (target.SkillExecutor != null && target.SkillExecutor.IsExecuting)
                    score += profile.PreferAgainstDistractedTarget * 1.2f;

                // Target low stamina
                if (target.StaminaRatio < 0.3f)
                    score += profile.PreferLowEnemyStamina * 1.0f;
            }

            // Wall proximity
            if (context.DistanceToNearestWall <= 2f)
                score += profile.PreferNearWall * 1.0f;

            // Cliff proximity
            if (context.DistanceToNearestCliff <= 3f)
                score += profile.PreferNearCliff * 1.2f;

            // Own stamina
            score += profile.PreferHighOwnStamina * context.StaminaRatio;

            // Ally in danger
            if (context.MostVulnerableAlly != null && context.MostVulnerableAlly.HealthRatio < 0.3f)
                score += profile.PreferAllyInDanger * 1.0f;

            // --- Commitment Penalty ---
            // Higher commitment = riskier. Penalize if low stamina or surrounded (unless skill is good for that)
            float commitmentPenalty = profile.CommitmentLevel * (1f - context.StaminaRatio) * 0.5f;
            score -= commitmentPenalty;

            // Risk penalty - scale with health
            float riskPenalty = profile.RiskLevel * (1f - context.HealthRatio) * 0.8f;
            score -= riskPenalty;

            // --- Doctrine Modifiers ---
            if (doctrine != null)
            {
                score *= doctrine.GetCategoryMultiplier(skill.Category);
                score += doctrine.AggressionBias * (profile.RiskLevel > 0.5f ? 0.3f : 0f);
                score += doctrine.DefensiveBias * (skill.Category == SkillCategory.Defensive ? 0.4f : 0f);
            }

            // --- Distance Penalty ---
            if (target != null && target != context.Self)
            {
                float dist = Vector3.Distance(context.Position, target.transform.position);
                float maxRange = skill.TargetingProfile.MaxRange;
                if (dist > maxRange)
                {
                    // Penalize the farther away we are (approach cost)
                    float distPenalty = (dist - maxRange) * 0.1f;
                    score -= distPenalty;
                }
            }

            return Mathf.Max(0f, score);
        }

        private RelativeDirection GetAttackDirection(CombatContext context, CombatAgent target)
        {
            // How are we positioned relative to the TARGET's facing?
            Vector3 targetFacing = target.Facing != null ? target.Facing.Direction : target.transform.forward;
            Vector3 toUs = (context.Position - target.transform.position).normalized;
            return DirectionUtility.GetRelativeDirection(targetFacing, toUs);
        }
    }
}

