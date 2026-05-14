// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Core Enumerations
// =============================================================================
// Defines all shared enumerations used across the combat system.
// Separated for clarity, serialization, and future ECS migration.
// =============================================================================

namespace Combat.Core
{
    /// <summary>
    /// Combat stance affects available actions, utility scoring, and movement behavior.
    /// </summary>
    public enum CombatStance
    {
        /// <summary>Default balanced stance.</summary>
        Neutral,
        /// <summary>Bonus to attack, penalty to defense.</summary>
        Aggressive,
        /// <summary>Bonus to defense, penalty to attack speed.</summary>
        Defensive,
        /// <summary>Bonus to mobility, penalty to damage.</summary>
        Evasive,
        /// <summary>Committed to an action, limited mobility.</summary>
        Committed,
        /// <summary>Exhausted, reduced capabilities.</summary>
        Exhausted,
        /// <summary>Braced for impact, cannot move but high resistance.</summary>
        Braced,
        /// <summary>Berserk mode: massive damage, no defense.</summary>
        Berserk,
        Stunned
    }

    /// <summary>
    /// The agent's current combat state within the engagement lifecycle.
    /// </summary>
    public enum CombatState
    {
        /// <summary>Not in combat, normal traversal.</summary>
        Idle,
        /// <summary>Aware of threats, transitioning to combat.</summary>
        Alert,
        /// <summary>Actively engaged in combat.</summary>
        InCombat,
        /// <summary>Executing a skill (windups, active, recovery).</summary>
        Executing,
        /// <summary>Recovering from a heavy action.</summary>
        Recovering,
        /// <summary>Stunned or interrupted.</summary>
        Stunned,
        /// <summary>Disengaging from combat.</summary>
        Disengaging,
        /// <summary>Fleeing from threats.</summary>
        Fleeing
    }

    /// <summary>
    /// Faction/team affiliation for combat targeting.
    /// </summary>
    public enum Faction
    {
        Neutral,
        PlayerParty,
        EnemyA,
        EnemyB,
        EnemyC,
        Wildlife,
        Undead
    }

    /// <summary>
    /// Attack geometry shape for skill targeting.
    /// </summary>
    public enum AttackShape
    {
        /// <summary>Narrow line attack (thrust, stab).</summary>
        Line,
        /// <summary>Forward cone (wide sweep).</summary>
        Cone,
        /// <summary>Full circle around caster (spin attack).</summary>
        Circle,
        /// <summary>Forward arc (overhead smash).</summary>
        Arc,
        /// <summary>Single target point (backstab).</summary>
        Point,
        /// <summary>Rectangle in front (charge).</summary>
        Rectangle
    }

    /// <summary>
    /// Direction relative to target for positional bonuses.
    /// </summary>
    public enum RelativeDirection
    {
        Front,
        FrontLeft,
        FrontRight,
        Left,
        Right,
        RearLeft,
        RearRight,
        Rear
    }

    /// <summary>
    /// Weapon type affects combat behavior, spacing, and available skills.
    /// </summary>
    public enum WeaponType
    {
        Unarmed,
        Sword,
        Dagger,
        Spear,
        Polearm,
        Axe,
        Hammer,
        Shield,
        GreatSword,
        Staff
    }

    /// <summary>
    /// Skill execution phase for animation and interruptibility.
    /// </summary>
    public enum SkillPhase
    {
        /// <summary>Not executing.</summary>
        None,
        /// <summary>Telegraphing the attack (can be interrupted).</summary>
        Windup,
        /// <summary>Active frames dealing damage/effects.</summary>
        Active,
        /// <summary>Post-attack recovery (vulnerable).</summary>
        Recovery,
        /// <summary>Skill completed.</summary>
        Complete
    }

    /// <summary>
    /// Skill category for AI utility scoring and weapon restrictions.
    /// </summary>
    public enum SkillCategory
    {
        AttackGeometry,
        MovementAttack,
        Positional,
        Tempo,
        Defensive,
        CrowdManipulation,
        Commitment
    }

    /// <summary>
    /// Terrain feature type for terrain-interacting skills.
    /// </summary>
    public enum TerrainFeature
    {
        None,
        Wall,
        Cliff,
        Corridor,
        Doorway,
        Hazard,
        Trap,
        OpenSpace,
        Chokepoint
    }

    /// <summary>
    /// Formation role determines positioning behavior.
    /// </summary>
    public enum FormationRole
    {
        Frontline,
        Backline,
        Flank,
        Support,
        Vanguard,
        Rearguard,
        Free
    }

    /// <summary>
    /// Threat level assessment for the aggro system.
    /// </summary>
    public enum ThreatLevel
    {
        None,
        Low,
        Medium,
        High,
        Critical
    }
}

