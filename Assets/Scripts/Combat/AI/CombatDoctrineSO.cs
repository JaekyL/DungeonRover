// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Combat Doctrine ScriptableObject
// =============================================================================
// Configurable combat personality/strategy. Determines overall combat behavior
// preferences, aggression levels, and skill category biases.
// =============================================================================

using Combat.Core;
using UnityEngine;

namespace Combat.AI
{
    /// <summary>
    /// Defines a combat doctrine (personality/strategy) for an AI combatant.
    /// Equivalent to Pillars of Eternity's configurable AI behavior slots.
    /// 
    /// Examples:
    /// - "Berserker": High aggression, prefers commitment skills, ignores defense
    /// - "Guardian": High defense, prefers shield/intercept, holds formation
    /// - "Flanker": Prefers positional skills, seeks flanking opportunities
    /// - "Controller": Prefers crowd manipulation, holds chokepoints
    /// </summary>
    [CreateAssetMenu(fileName = "NewDoctrine", menuName = "Combat/Doctrine")]
    public class CombatDoctrineSO : ScriptableObject
    {
        [Header("Identity")]
        public string DoctrineName = "Default";
        [TextArea] public string Description;

        [Header("Decision Thresholds")]
        [Tooltip("Minimum utility score to take action (lower = more active)")]
        [Range(0.05f, 0.5f)] public float ActionThreshold = 0.15f;

        [Tooltip("How much better a new target must be to switch (hysteresis)")]
        [Range(1.0f, 2.0f)] public float TargetSwitchThreshold = 1.3f;

        [Header("General Biases")]
        [Range(-1, 1)] public float AggressionBias = 0f;
        [Range(-1, 1)] public float DefensiveBias = 0f;
        [Range(-1, 1)] public float MobilityBias = 0f;
        [Range(0, 1)] public float RiskTolerance = 0.5f;

        [Header("Skill Category Multipliers")]
        [Range(0, 2)] public float AttackGeometryMultiplier = 1f;
        [Range(0, 2)] public float MovementAttackMultiplier = 1f;
        [Range(0, 2)] public float PositionalMultiplier = 1f;
        [Range(0, 2)] public float TempoMultiplier = 1f;
        [Range(0, 2)] public float DefensiveMultiplier = 1f;
        [Range(0, 2)] public float CrowdManipulationMultiplier = 1f;
        [Range(0, 2)] public float CommitmentMultiplier = 1f;

        [Header("Positioning Preferences")]
        [Tooltip("Preferred engagement range (weapon-reach multiplier)")]
        [Range(0.5f, 2f)] public float PreferredRangeMultiplier = 1f;

        [Tooltip("How much the agent values staying in formation")]
        [Range(0, 1)] public float FormationDiscipline = 0.5f;

        [Tooltip("How eagerly the agent pursues flanking opportunities")]
        [Range(0, 1)] public float FlankingDesire = 0.3f;

        [Header("Retreat Behavior")]
        [Tooltip("Health ratio at which retreat is considered")]
        [Range(0, 0.8f)] public float RetreatHealthThreshold = 0.2f;

        [Tooltip("Stamina ratio at which agent becomes more cautious")]
        [Range(0, 0.5f)] public float CautionStaminaThreshold = 0.25f;

        /// <summary>Get the utility multiplier for a skill category.</summary>
        public float GetCategoryMultiplier(SkillCategory category)
        {
            switch (category)
            {
                case SkillCategory.AttackGeometry: return AttackGeometryMultiplier;
                case SkillCategory.MovementAttack: return MovementAttackMultiplier;
                case SkillCategory.Positional: return PositionalMultiplier;
                case SkillCategory.Tempo: return TempoMultiplier;
                case SkillCategory.Defensive: return DefensiveMultiplier;
                case SkillCategory.CrowdManipulation: return CrowdManipulationMultiplier;
                case SkillCategory.Commitment: return CommitmentMultiplier;
                default: return 1f;
            }
        }
    }

    /// <summary>
    /// Factory for creating pre-built doctrine presets.
    /// </summary>
    public static class CombatDoctrinePresets
    {
        public static CombatDoctrineSO CreateBerserker()
        {
            var d = ScriptableObject.CreateInstance<CombatDoctrineSO>();
            d.DoctrineName = "Berserker";
            d.Description = "Reckless aggression. Maximizes damage output at personal risk.";
            d.AggressionBias = 0.8f;
            d.DefensiveBias = -0.5f;
            d.RiskTolerance = 0.9f;
            d.CommitmentMultiplier = 1.8f;
            d.DefensiveMultiplier = 0.3f;
            d.AttackGeometryMultiplier = 1.5f;
            d.FormationDiscipline = 0.1f;
            d.FlankingDesire = 0.2f;
            d.RetreatHealthThreshold = 0.05f;
            d.ActionThreshold = 0.1f;
            return d;
        }

        public static CombatDoctrineSO CreateGuardian()
        {
            var d = ScriptableObject.CreateInstance<CombatDoctrineSO>();
            d.DoctrineName = "Guardian";
            d.Description = "Defensive bulwark. Holds position and protects allies.";
            d.AggressionBias = -0.3f;
            d.DefensiveBias = 0.8f;
            d.RiskTolerance = 0.3f;
            d.DefensiveMultiplier = 1.8f;
            d.CommitmentMultiplier = 0.5f;
            d.CrowdManipulationMultiplier = 1.3f;
            d.FormationDiscipline = 0.9f;
            d.FlankingDesire = 0.0f;
            d.RetreatHealthThreshold = 0.1f;
            d.PreferredRangeMultiplier = 0.8f;
            return d;
        }

        public static CombatDoctrineSO CreateFlanker()
        {
            var d = ScriptableObject.CreateInstance<CombatDoctrineSO>();
            d.DoctrineName = "Flanker";
            d.Description = "Mobile striker. Seeks positional advantage.";
            d.AggressionBias = 0.3f;
            d.MobilityBias = 0.8f;
            d.RiskTolerance = 0.5f;
            d.PositionalMultiplier = 1.8f;
            d.MovementAttackMultiplier = 1.5f;
            d.DefensiveMultiplier = 0.5f;
            d.FormationDiscipline = 0.2f;
            d.FlankingDesire = 0.9f;
            d.PreferredRangeMultiplier = 1.2f;
            return d;
        }

        public static CombatDoctrineSO CreateController()
        {
            var d = ScriptableObject.CreateInstance<CombatDoctrineSO>();
            d.DoctrineName = "Controller";
            d.Description = "Crowd control specialist. Manipulates space and movement.";
            d.AggressionBias = 0.1f;
            d.DefensiveBias = 0.2f;
            d.RiskTolerance = 0.4f;
            d.CrowdManipulationMultiplier = 1.8f;
            d.TempoMultiplier = 1.4f;
            d.CommitmentMultiplier = 0.7f;
            d.FormationDiscipline = 0.6f;
            d.FlankingDesire = 0.3f;
            return d;
        }

        public static CombatDoctrineSO CreateDuelist()
        {
            var d = ScriptableObject.CreateInstance<CombatDoctrineSO>();
            d.DoctrineName = "Duelist";
            d.Description = "Precision fighter. Excels in 1v1 with tempo and positioning.";
            d.AggressionBias = 0.4f;
            d.RiskTolerance = 0.5f;
            d.TempoMultiplier = 1.7f;
            d.PositionalMultiplier = 1.4f;
            d.CrowdManipulationMultiplier = 0.5f;
            d.FormationDiscipline = 0.3f;
            d.FlankingDesire = 0.6f;
            d.PreferredRangeMultiplier = 1.0f;
            return d;
        }
    }
}

