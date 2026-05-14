// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Example Skill Definitions (Created via ScriptableObject factory)
// =============================================================================
// Factory class to create pre-configured example skills programmatically.
// In production, these would be created as assets in the editor.
// =============================================================================

using Combat.Core;
using UnityEngine;

namespace Combat.Skills.Definitions
{
    /// <summary>
    /// Factory for creating example combat skills programmatically.
    /// Use for testing and demonstration. In production, create as SO assets.
    /// </summary>
    public static class SkillFactory
    {
        // =====================================================================
        // ATTACK GEOMETRY SKILLS
        // =====================================================================

        /// <summary>Narrow forward thrust. Best in corridors.</summary>
        public static CombatSkillSO CreateThrust()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Thrust";
            SetSkillFields(skill, "Thrust", SkillCategory.AttackGeometry,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Line,
                    MaxRange = 2.5f,
                    Width = 0.5f,
                    TargetsEnemies = true
                },
                windup: 0.2f, active: 0.15f, recovery: 0.3f,
                stamina: 12f, cooldown: 0.8f,
                utility: new SkillUtilityProfile
                {
                    PreferInCorridor = 0.9f,
                    BasePriority = 1.0f,
                    CommitmentLevel = 0.2f,
                    RiskLevel = 0.2f
                });
            return skill;
        }

        /// <summary>Wide horizontal sweep. Excellent when surrounded.</summary>
        public static CombatSkillSO CreateWideSweep()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Wide Sweep";
            SetSkillFields(skill, "Wide Sweep", SkillCategory.AttackGeometry,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Cone,
                    MaxRange = 2f,
                    ArcAngle = 180f,
                    TargetsEnemies = true
                },
                windup: 0.4f, active: 0.3f, recovery: 0.5f,
                stamina: 20f, cooldown: 1.5f,
                utility: new SkillUtilityProfile
                {
                    PreferWhenSurrounded = 0.95f,
                    PreferInOpenSpace = 0.6f,
                    BasePriority = 1.1f,
                    CommitmentLevel = 0.4f,
                    RiskLevel = 0.3f
                });
            return skill;
        }

        /// <summary>Powerful overhead smash. High damage, slow.</summary>
        public static CombatSkillSO CreateOverheadSmash()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Overhead Smash";
            SetSkillFields(skill, "Overhead Smash", SkillCategory.AttackGeometry,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Arc,
                    MaxRange = 1.8f,
                    ArcAngle = 60f,
                    TargetsEnemies = true
                },
                windup: 0.6f, active: 0.2f, recovery: 0.7f,
                stamina: 25f, cooldown: 2f,
                utility: new SkillUtilityProfile
                {
                    PreferLowEnemyStamina = 0.8f,
                    PreferHighOwnStamina = 0.7f,
                    BasePriority = 1.2f,
                    CommitmentLevel = 0.7f,
                    RiskLevel = 0.5f
                });
            return skill;
        }

        /// <summary>360-degree spin attack. Escapes encirclement.</summary>
        public static CombatSkillSO CreateSpinAttack()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Spin Attack";
            SetSkillFields(skill, "Spin Attack", SkillCategory.AttackGeometry,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Circle,
                    MaxRange = 1.8f,
                    TargetsEnemies = true
                },
                windup: 0.3f, active: 0.4f, recovery: 0.6f,
                stamina: 30f, cooldown: 3f,
                utility: new SkillUtilityProfile
                {
                    PreferWhenSurrounded = 1.0f,
                    BasePriority = 0.9f,
                    CommitmentLevel = 0.6f,
                    RiskLevel = 0.4f
                });
            return skill;
        }

        /// <summary>Step forward and strike. Closes distance.</summary>
        public static CombatSkillSO CreateAdvancingStrike()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Advancing Strike";
            SetSkillFields(skill, "Advancing Strike", SkillCategory.AttackGeometry,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Cone,
                    MaxRange = 2.5f,
                    ArcAngle = 45f,
                    TargetsEnemies = true
                },
                windup: 0.2f, active: 0.25f, recovery: 0.35f,
                stamina: 15f, cooldown: 1f,
                utility: new SkillUtilityProfile { BasePriority = 1.0f, CommitmentLevel = 0.3f },
                movement: new SkillMovementProfile
                {
                    HasMovement = true,
                    MovementDistance = 2f,
                    MovementDirection = Vector3.forward,
                    LocksFacing = true
                });
            return skill;
        }

        // =====================================================================
        // MOVEMENT-BASED SKILLS
        // =====================================================================

        /// <summary>Long-distance dash attack. Closes large gaps.</summary>
        public static CombatSkillSO CreateLunge()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Lunge";
            SetSkillFields(skill, "Lunge", SkillCategory.MovementAttack,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Line,
                    MaxRange = 4f,
                    Width = 0.8f,
                    TargetsEnemies = true
                },
                windup: 0.15f, active: 0.3f, recovery: 0.5f,
                stamina: 20f, cooldown: 2f,
                utility: new SkillUtilityProfile { BasePriority = 1.1f, CommitmentLevel = 0.5f, RiskLevel = 0.4f },
                movement: new SkillMovementProfile
                {
                    HasMovement = true,
                    MovementDistance = 4f,
                    MovementDirection = Vector3.forward,
                    LocksFacing = true,
                    SpeedMultiplier = 2f
                });
            return skill;
        }

        /// <summary>Leap attack from above. Ignores front line.</summary>
        public static CombatSkillSO CreateLeapAttack()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Leap Attack";
            SetSkillFields(skill, "Leap Attack", SkillCategory.MovementAttack,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Circle,
                    MaxRange = 1.5f,
                    TargetsEnemies = true
                },
                windup: 0.4f, active: 0.3f, recovery: 0.6f,
                stamina: 30f, cooldown: 4f,
                utility: new SkillUtilityProfile
                {
                    PreferInOpenSpace = 0.7f,
                    BasePriority = 1.0f,
                    CommitmentLevel = 0.7f,
                    RiskLevel = 0.6f
                },
                movement: new SkillMovementProfile
                {
                    HasMovement = true,
                    MovementDistance = 5f,
                    MovementDirection = Vector3.forward,
                    PushesThroughEnemies = true
                });
            return skill;
        }

        /// <summary>Slash while retreating. Maintains pressure while disengaging.</summary>
        public static CombatSkillSO CreateRetreatSlash()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Retreat Slash";
            SetSkillFields(skill, "Retreat Slash", SkillCategory.MovementAttack,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Cone,
                    MaxRange = 2f,
                    ArcAngle = 120f,
                    TargetsEnemies = true
                },
                windup: 0.2f, active: 0.2f, recovery: 0.3f,
                stamina: 18f, cooldown: 1.5f,
                utility: new SkillUtilityProfile { BasePriority = 0.9f, CommitmentLevel = 0.2f, RiskLevel = 0.1f },
                movement: new SkillMovementProfile
                {
                    HasMovement = true,
                    MovementDistance = 2.5f,
                    MovementDirection = Vector3.back,
                    LocksFacing = true
                });
            return skill;
        }

        /// <summary>Sidestep + counter. Punishes predictable attacks.</summary>
        public static CombatSkillSO CreateSidestepCounter()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Sidestep Counter";
            SetSkillFields(skill, "Sidestep Counter", SkillCategory.MovementAttack,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 2f,
                    TargetsEnemies = true
                },
                windup: 0.1f, active: 0.2f, recovery: 0.3f,
                stamina: 15f, cooldown: 2f,
                utility: new SkillUtilityProfile
                {
                    PreferAgainstDistractedTarget = 0.8f,
                    BasePriority = 1.0f,
                    CommitmentLevel = 0.3f
                },
                movement: new SkillMovementProfile
                {
                    HasMovement = true,
                    MovementDistance = 1.5f,
                    MovementDirection = Vector3.right,
                    LocksFacing = true
                });
            return skill;
        }

        /// <summary>Reckless forward charge. High commit, high reward.</summary>
        public static CombatSkillSO CreateChargeAttack()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Charge Attack";
            SetSkillFields(skill, "Charge Attack", SkillCategory.MovementAttack,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Rectangle,
                    MaxRange = 6f,
                    Width = 1.5f,
                    TargetsEnemies = true
                },
                windup: 0.5f, active: 0.4f, recovery: 0.8f,
                stamina: 35f, cooldown: 5f,
                utility: new SkillUtilityProfile
                {
                    PreferInOpenSpace = 0.8f,
                    PreferHighOwnStamina = 0.9f,
                    BasePriority = 1.2f,
                    CommitmentLevel = 0.9f,
                    RiskLevel = 0.7f
                },
                movement: new SkillMovementProfile
                {
                    HasMovement = true,
                    MovementDistance = 6f,
                    MovementDirection = Vector3.forward,
                    SpeedMultiplier = 2.5f,
                    PushesThroughEnemies = true,
                    LocksFacing = true
                });
            return skill;
        }

        // =====================================================================
        // POSITIONAL SKILLS
        // =====================================================================

        /// <summary>Attack from behind. Requires positioning.</summary>
        public static CombatSkillSO CreateBackstab()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Backstab";
            SetSkillFields(skill, "Backstab", SkillCategory.Positional,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 1.5f,
                    TargetsEnemies = true,
                    RequiredDirection = RelativeDirection.Rear
                },
                windup: 0.15f, active: 0.1f, recovery: 0.25f,
                stamina: 10f, cooldown: 1f,
                utility: new SkillUtilityProfile
                {
                    PreferWhenBehindTarget = 1.0f,
                    PreferWhenFlanking = 0.7f,
                    PreferAgainstDistractedTarget = 0.9f,
                    BasePriority = 1.3f,
                    CommitmentLevel = 0.2f,
                    RiskLevel = 0.2f
                });
            return skill;
        }

        /// <summary>Strike from the side. Moderate positional bonus.</summary>
        public static CombatSkillSO CreateFlankStrike()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Flank Strike";
            SetSkillFields(skill, "Flank Strike", SkillCategory.Positional,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 2f,
                    TargetsEnemies = true
                },
                windup: 0.2f, active: 0.15f, recovery: 0.3f,
                stamina: 12f, cooldown: 1f,
                utility: new SkillUtilityProfile
                {
                    PreferWhenFlanking = 0.9f,
                    BasePriority = 1.1f,
                    CommitmentLevel = 0.3f
                });
            return skill;
        }

        /// <summary>Slam target into nearby wall. Requires wall proximity.</summary>
        public static CombatSkillSO CreateWallSlam()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Wall Slam";
            SetSkillFields(skill, "Wall Slam", SkillCategory.Positional,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 1.5f,
                    TargetsEnemies = true
                },
                windup: 0.3f, active: 0.2f, recovery: 0.4f,
                stamina: 20f, cooldown: 3f,
                utility: new SkillUtilityProfile
                {
                    PreferNearWall = 1.0f,
                    BasePriority = 1.2f,
                    CommitmentLevel = 0.5f,
                    RiskLevel = 0.3f
                });
            return skill;
        }

        /// <summary>Push enemy off a cliff. Instant kill potential.</summary>
        public static CombatSkillSO CreateCliffPush()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Cliff Push";
            SetSkillFields(skill, "Cliff Push", SkillCategory.Positional,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 1.5f,
                    TargetsEnemies = true
                },
                windup: 0.2f, active: 0.15f, recovery: 0.3f,
                stamina: 15f, cooldown: 2f,
                utility: new SkillUtilityProfile
                {
                    PreferNearCliff = 1.0f,
                    BasePriority = 1.5f,
                    CommitmentLevel = 0.4f
                });
            return skill;
        }

        /// <summary>Charge through formation, splitting groups.</summary>
        public static CombatSkillSO CreateFormationSplitter()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Formation Splitter";
            SetSkillFields(skill, "Formation Splitter", SkillCategory.Positional,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Rectangle,
                    MaxRange = 4f,
                    Width = 2f,
                    TargetsEnemies = true
                },
                windup: 0.4f, active: 0.35f, recovery: 0.5f,
                stamina: 30f, cooldown: 5f,
                utility: new SkillUtilityProfile
                {
                    PreferWhenSurrounded = 0.6f,
                    PreferInOpenSpace = 0.7f,
                    BasePriority = 1.1f,
                    CommitmentLevel = 0.7f,
                    RiskLevel = 0.5f
                },
                movement: new SkillMovementProfile
                {
                    HasMovement = true,
                    MovementDistance = 4f,
                    MovementDirection = Vector3.forward,
                    PushesThroughEnemies = true
                });
            return skill;
        }

        // =====================================================================
        // TEMPO SKILLS
        // =====================================================================

        /// <summary>Fast strike that interrupts enemy windups.</summary>
        public static CombatSkillSO CreateInterrupt()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Interrupt";
            SetSkillFields(skill, "Interrupt", SkillCategory.Tempo,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 2f,
                    TargetsEnemies = true
                },
                windup: 0.05f, active: 0.1f, recovery: 0.3f,
                stamina: 10f, cooldown: 2f,
                utility: new SkillUtilityProfile
                {
                    PreferAgainstDistractedTarget = 1.0f, // "distracted" = winding up
                    BasePriority = 1.3f,
                    CommitmentLevel = 0.1f,
                    RiskLevel = 0.1f
                });
            return skill;
        }

        /// <summary>Counter-attack after blocking. Timing-based.</summary>
        public static CombatSkillSO CreateRiposte()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Riposte";
            SetSkillFields(skill, "Riposte", SkillCategory.Tempo,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 2f,
                    TargetsEnemies = true
                },
                windup: 0.1f, active: 0.15f, recovery: 0.25f,
                stamina: 12f, cooldown: 1.5f,
                utility: new SkillUtilityProfile
                {
                    BasePriority = 1.2f,
                    CommitmentLevel = 0.2f,
                    RiskLevel = 0.2f
                });
            return skill;
        }

        /// <summary>Breaks enemy guard, leaving them vulnerable.</summary>
        public static CombatSkillSO CreateGuardBreak()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Guard Break";
            SetSkillFields(skill, "Guard Break", SkillCategory.Tempo,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 1.8f,
                    TargetsEnemies = true
                },
                windup: 0.4f, active: 0.2f, recovery: 0.4f,
                stamina: 20f, cooldown: 3f,
                utility: new SkillUtilityProfile
                {
                    PreferHighOwnStamina = 0.7f,
                    BasePriority = 1.1f,
                    CommitmentLevel = 0.5f
                });
            return skill;
        }

        /// <summary>Slow powerful strike. Punishes immobile targets.</summary>
        public static CombatSkillSO CreateDelayedStrike()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Delayed Strike";
            SetSkillFields(skill, "Delayed Strike", SkillCategory.Tempo,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Arc,
                    MaxRange = 2f,
                    ArcAngle = 90f,
                    TargetsEnemies = true
                },
                windup: 0.8f, active: 0.2f, recovery: 0.5f,
                stamina: 22f, cooldown: 2.5f,
                utility: new SkillUtilityProfile
                {
                    PreferLowEnemyStamina = 0.9f,
                    BasePriority = 1.0f,
                    CommitmentLevel = 0.6f,
                    RiskLevel = 0.5f
                });
            return skill;
        }

        /// <summary>Finisher on low-health targets.</summary>
        public static CombatSkillSO CreateExecute()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Execute";
            SetSkillFields(skill, "Execute", SkillCategory.Tempo,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 1.5f,
                    TargetsEnemies = true
                },
                windup: 0.5f, active: 0.2f, recovery: 0.6f,
                stamina: 25f, cooldown: 5f,
                utility: new SkillUtilityProfile
                {
                    PreferLowEnemyStamina = 1.0f,
                    BasePriority = 1.4f,
                    CommitmentLevel = 0.6f,
                    RiskLevel = 0.4f
                });
            return skill;
        }

        // =====================================================================
        // DEFENSIVE SKILLS
        // =====================================================================

        /// <summary>Raise shield wall. Blocks frontal attacks for allies.</summary>
        public static CombatSkillSO CreateShieldWall()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Shield Wall";
            SetSkillFields(skill, "Shield Wall", SkillCategory.Defensive,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 0f,
                    IsSelfTargeted = true
                },
                windup: 0.2f, active: 2f, recovery: 0.3f,
                stamina: 15f, cooldown: 3f,
                utility: new SkillUtilityProfile
                {
                    PreferAllyInDanger = 0.8f,
                    PreferChokepoint = 0.9f,
                    BasePriority = 0.9f,
                    CommitmentLevel = 0.5f,
                    RiskLevel = 0.1f
                });
            return skill;
        }

        /// <summary>Brace for impact. High resistance, cannot move.</summary>
        public static CombatSkillSO CreateBrace()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Brace";
            SetSkillFields(skill, "Brace", SkillCategory.Defensive,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 0f,
                    IsSelfTargeted = true
                },
                windup: 0.1f, active: 1.5f, recovery: 0.2f,
                stamina: 10f, cooldown: 2f,
                utility: new SkillUtilityProfile
                {
                    PreferChokepoint = 0.7f,
                    BasePriority = 0.8f,
                    CommitmentLevel = 0.4f,
                    RiskLevel = 0.05f
                });
            return skill;
        }

        /// <summary>Move to intercept an attack aimed at an ally.</summary>
        public static CombatSkillSO CreateIntercept()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Intercept";
            SetSkillFields(skill, "Intercept", SkillCategory.Defensive,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 4f,
                    TargetsAllies = true,
                    TargetsEnemies = false
                },
                windup: 0.1f, active: 0.3f, recovery: 0.3f,
                stamina: 20f, cooldown: 4f,
                utility: new SkillUtilityProfile
                {
                    PreferAllyInDanger = 1.0f,
                    BasePriority = 1.2f,
                    CommitmentLevel = 0.5f,
                    RiskLevel = 0.3f
                },
                movement: new SkillMovementProfile
                {
                    HasMovement = true,
                    MovementDistance = 4f,
                    MovementDirection = Vector3.forward,
                    SpeedMultiplier = 2f
                });
            return skill;
        }

        /// <summary>Hold an area, preventing enemy passage.</summary>
        public static CombatSkillSO CreateZoneHold()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Zone Hold";
            SetSkillFields(skill, "Zone Hold", SkillCategory.Defensive,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Circle,
                    MaxRange = 2f,
                    IsSelfTargeted = true
                },
                windup: 0.2f, active: 3f, recovery: 0.3f,
                stamina: 25f, cooldown: 5f,
                utility: new SkillUtilityProfile
                {
                    PreferChokepoint = 1.0f,
                    PreferAllyInDanger = 0.7f,
                    BasePriority = 1.0f,
                    CommitmentLevel = 0.7f
                });
            return skill;
        }

        /// <summary>Defensive stance with reduced damage taken.</summary>
        public static CombatSkillSO CreateDefensiveStance()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Defensive Stance";
            SetSkillFields(skill, "Defensive Stance", SkillCategory.Defensive,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 0f,
                    IsSelfTargeted = true
                },
                windup: 0.1f, active: 0.1f, recovery: 0.1f,
                stamina: 5f, cooldown: 1f,
                utility: new SkillUtilityProfile
                {
                    BasePriority = 0.7f,
                    CommitmentLevel = 0.1f,
                    RiskLevel = 0.0f
                });
            return skill;
        }

        // =====================================================================
        // CROWD MANIPULATION SKILLS
        // =====================================================================

        /// <summary>Powerful knockback. Creates space.</summary>
        public static CombatSkillSO CreateKnockback()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Knockback";
            SetSkillFields(skill, "Knockback", SkillCategory.CrowdManipulation,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Cone,
                    MaxRange = 2f,
                    ArcAngle = 60f,
                    TargetsEnemies = true
                },
                windup: 0.3f, active: 0.2f, recovery: 0.4f,
                stamina: 18f, cooldown: 2f,
                utility: new SkillUtilityProfile
                {
                    PreferWhenSurrounded = 0.8f,
                    PreferNearCliff = 0.9f,
                    BasePriority = 1.0f,
                    CommitmentLevel = 0.3f
                });
            return skill;
        }

        /// <summary>Pull an enemy towards you. Break their formation.</summary>
        public static CombatSkillSO CreatePull()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Pull";
            SetSkillFields(skill, "Pull", SkillCategory.CrowdManipulation,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 4f,
                    TargetsEnemies = true
                },
                windup: 0.3f, active: 0.2f, recovery: 0.4f,
                stamina: 15f, cooldown: 3f,
                utility: new SkillUtilityProfile
                {
                    BasePriority = 0.9f,
                    CommitmentLevel = 0.3f
                });
            return skill;
        }

        /// <summary>Trip enemies in a low sweep.</summary>
        public static CombatSkillSO CreateTrip()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Trip";
            SetSkillFields(skill, "Trip", SkillCategory.CrowdManipulation,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Cone,
                    MaxRange = 2f,
                    ArcAngle = 120f,
                    TargetsEnemies = true
                },
                windup: 0.2f, active: 0.2f, recovery: 0.35f,
                stamina: 15f, cooldown: 2.5f,
                utility: new SkillUtilityProfile
                {
                    PreferWhenSurrounded = 0.7f,
                    BasePriority = 1.0f,
                    CommitmentLevel = 0.3f
                });
            return skill;
        }

        /// <summary>Sweep leg targeting lower body.</summary>
        public static CombatSkillSO CreateSweepLeg()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Sweep Leg";
            SetSkillFields(skill, "Sweep Leg", SkillCategory.CrowdManipulation,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Cone,
                    MaxRange = 1.8f,
                    ArcAngle = 150f,
                    TargetsEnemies = true
                },
                windup: 0.25f, active: 0.25f, recovery: 0.4f,
                stamina: 18f, cooldown: 2f,
                utility: new SkillUtilityProfile
                {
                    PreferWhenSurrounded = 0.8f,
                    BasePriority = 1.0f,
                    CommitmentLevel = 0.4f
                });
            return skill;
        }

        /// <summary>Terrifying strike that causes enemies to scatter.</summary>
        public static CombatSkillSO CreateFearStrike()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Fear Strike";
            SetSkillFields(skill, "Fear Strike", SkillCategory.CrowdManipulation,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Cone,
                    MaxRange = 3f,
                    ArcAngle = 90f,
                    TargetsEnemies = true
                },
                windup: 0.4f, active: 0.2f, recovery: 0.5f,
                stamina: 25f, cooldown: 5f,
                utility: new SkillUtilityProfile
                {
                    PreferWhenSurrounded = 0.9f,
                    PreferHighOwnStamina = 0.6f,
                    BasePriority = 1.0f,
                    CommitmentLevel = 0.5f,
                    RiskLevel = 0.2f
                });
            return skill;
        }

        // =====================================================================
        // COMMITMENT SKILLS
        // =====================================================================

        /// <summary>Charged heavy attack. Long windup, devastating damage.</summary>
        public static CombatSkillSO CreateChargedAttack()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Charged Attack";
            SetSkillFields(skill, "Charged Attack", SkillCategory.Commitment,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Arc,
                    MaxRange = 2.5f,
                    ArcAngle = 90f,
                    TargetsEnemies = true
                },
                windup: 1.2f, active: 0.2f, recovery: 0.8f,
                stamina: 35f, cooldown: 4f,
                utility: new SkillUtilityProfile
                {
                    PreferHighOwnStamina = 0.9f,
                    PreferLowEnemyStamina = 0.8f,
                    BasePriority = 1.3f,
                    CommitmentLevel = 0.9f,
                    RiskLevel = 0.7f
                });
            return skill;
        }

        /// <summary>Berserker rush. Multiple rapid strikes, exhausts self.</summary>
        public static CombatSkillSO CreateBerserkRush()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Berserk Rush";
            SetSkillFields(skill, "Berserk Rush", SkillCategory.Commitment,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Cone,
                    MaxRange = 2f,
                    ArcAngle = 60f,
                    TargetsEnemies = true
                },
                windup: 0.2f, active: 1.5f, recovery: 1.0f,
                stamina: 50f, cooldown: 8f,
                utility: new SkillUtilityProfile
                {
                    PreferHighOwnStamina = 1.0f,
                    BasePriority = 1.1f,
                    CommitmentLevel = 1.0f,
                    RiskLevel = 0.9f
                },
                movement: new SkillMovementProfile
                {
                    HasMovement = true,
                    MovementDistance = 3f,
                    MovementDirection = Vector3.forward,
                    LocksFacing = true
                });
            return skill;
        }

        /// <summary>Reckless wide cleave. High AoE at personal risk.</summary>
        public static CombatSkillSO CreateRecklessCleave()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Reckless Cleave";
            SetSkillFields(skill, "Reckless Cleave", SkillCategory.Commitment,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Cone,
                    MaxRange = 2.5f,
                    ArcAngle = 240f,
                    TargetsEnemies = true
                },
                windup: 0.5f, active: 0.4f, recovery: 0.9f,
                stamina: 40f, cooldown: 5f,
                utility: new SkillUtilityProfile
                {
                    PreferWhenSurrounded = 1.0f,
                    PreferHighOwnStamina = 0.8f,
                    BasePriority = 1.2f,
                    CommitmentLevel = 0.8f,
                    RiskLevel = 0.8f
                });
            return skill;
        }

        /// <summary>Lock into a duel with one target. Both become isolated.</summary>
        public static CombatSkillSO CreateDuelLock()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Duel Lock";
            SetSkillFields(skill, "Duel Lock", SkillCategory.Commitment,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Point,
                    MaxRange = 2f,
                    TargetsEnemies = true
                },
                windup: 0.3f, active: 0.1f, recovery: 0.2f,
                stamina: 20f, cooldown: 10f,
                utility: new SkillUtilityProfile
                {
                    BasePriority = 0.9f,
                    CommitmentLevel = 0.8f,
                    RiskLevel = 0.5f
                });
            return skill;
        }

        /// <summary>Chain of attacks building momentum. Each hit increases speed.</summary>
        public static CombatSkillSO CreateMomentumCombo()
        {
            var skill = ScriptableObject.CreateInstance<CombatSkillSO>();
            skill.name = "Momentum Combo";
            SetSkillFields(skill, "Momentum Combo", SkillCategory.Commitment,
                new SkillTargetingProfile
                {
                    Shape = AttackShape.Cone,
                    MaxRange = 2f,
                    ArcAngle = 90f,
                    TargetsEnemies = true
                },
                windup: 0.15f, active: 0.8f, recovery: 0.6f,
                stamina: 30f, cooldown: 3f,
                utility: new SkillUtilityProfile
                {
                    PreferHighOwnStamina = 0.7f,
                    BasePriority = 1.0f,
                    CommitmentLevel = 0.6f,
                    RiskLevel = 0.4f
                });
            return skill;
        }

        // =====================================================================
        // HELPER
        // =====================================================================

        private static void SetSkillFields(CombatSkillSO skill, string name, SkillCategory category,
            SkillTargetingProfile targeting, float windup, float active, float recovery,
            float stamina, float cooldown, SkillUtilityProfile utility,
            SkillMovementProfile movement = null)
        {
            // Use reflection to set private serialized fields
            var type = typeof(CombatSkillSO);
            SetField(type, skill, "_skillName", name);
            SetField(type, skill, "_category", category);
            SetField(type, skill, "_targetingProfile", targeting);
            SetField(type, skill, "_windupDuration", windup);
            SetField(type, skill, "_activeDuration", active);
            SetField(type, skill, "_recoveryDuration", recovery);
            SetField(type, skill, "_staminaCost", stamina);
            SetField(type, skill, "_cooldownDuration", cooldown);
            SetField(type, skill, "_utilityProfile", utility);
            if (movement != null)
                SetField(type, skill, "_movementProfile", movement);
            else
                SetField(type, skill, "_movementProfile", new SkillMovementProfile());
        }

        private static void SetField(System.Type type, object obj, string fieldName, object value)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }
}

