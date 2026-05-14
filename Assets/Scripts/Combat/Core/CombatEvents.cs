// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Event Definitions
// =============================================================================
// Central event system for decoupled combat communication.
// Uses C# events + a static event bus for cross-system notifications.
// =============================================================================

using System;
using UnityEngine;

namespace Combat.Core
{
    /// <summary>
    /// Static event bus for combat system-wide events.
    /// Systems subscribe to relevant events without direct coupling.
    /// </summary>
    public static class CombatEvents
    {
        // --- Engagement Events ---
        public static event Action<EngagementEventArgs> OnEngagementStarted;
        public static event Action<EngagementEventArgs> OnEngagementBroken;
        public static event Action<DisengageEventArgs> OnDisengageAttempt;

        // --- Skill Events ---
        public static event Action<SkillEventArgs> OnSkillStarted;
        public static event Action<SkillEventArgs> OnSkillPhaseChanged;
        public static event Action<SkillEventArgs> OnSkillCompleted;
        public static event Action<SkillEventArgs> OnSkillInterrupted;
        public static event Action<SkillHitEventArgs> OnSkillHit;

        // --- Combat State Events ---
        public static event Action<CombatStateEventArgs> OnCombatStateChanged;
        public static event Action<StanceEventArgs> OnStanceChanged;

        // --- Damage & Effects ---
        public static event Action<DamageEventArgs> OnDamageDealt;
        public static event Action<DisplacementEventArgs> OnDisplacement;
        public static event Action<CombatAgent> OnAgentDefeated;
        public static event Action<CombatAgent> OnAgentStunned;

        // --- Formation Events ---
        public static event Action<FormationEventArgs> OnFormationBroken;
        public static event Action<FormationEventArgs> OnFormationFormed;

        // --- Threat Events ---
        public static event Action<ThreatEventArgs> OnThreatChanged;

        // --- Stamina Events ---
        public static event Action<StaminaEventArgs> OnStaminaDepleted;
        public static event Action<StaminaEventArgs> OnStaminaRecovered;

        // Fire methods (null-safe invocations)
        public static void FireEngagementStarted(EngagementEventArgs args) => OnEngagementStarted?.Invoke(args);
        public static void FireEngagementBroken(EngagementEventArgs args) => OnEngagementBroken?.Invoke(args);
        public static void FireDisengageAttempt(DisengageEventArgs args) => OnDisengageAttempt?.Invoke(args);
        public static void FireSkillStarted(SkillEventArgs args) => OnSkillStarted?.Invoke(args);
        public static void FireSkillPhaseChanged(SkillEventArgs args) => OnSkillPhaseChanged?.Invoke(args);
        public static void FireSkillCompleted(SkillEventArgs args) => OnSkillCompleted?.Invoke(args);
        public static void FireSkillInterrupted(SkillEventArgs args) => OnSkillInterrupted?.Invoke(args);
        public static void FireSkillHit(SkillHitEventArgs args) => OnSkillHit?.Invoke(args);
        public static void FireCombatStateChanged(CombatStateEventArgs args) => OnCombatStateChanged?.Invoke(args);
        public static void FireStanceChanged(StanceEventArgs args) => OnStanceChanged?.Invoke(args);
        public static void FireDamageDealt(DamageEventArgs args) => OnDamageDealt?.Invoke(args);
        public static void FireDisplacement(DisplacementEventArgs args) => OnDisplacement?.Invoke(args);
        public static void FireAgentDefeated(CombatAgent agent) => OnAgentDefeated?.Invoke(agent);
        public static void FireAgentStunned(CombatAgent agent) => OnAgentStunned?.Invoke(agent);
        public static void FireFormationBroken(FormationEventArgs args) => OnFormationBroken?.Invoke(args);
        public static void FireFormationFormed(FormationEventArgs args) => OnFormationFormed?.Invoke(args);
        public static void FireThreatChanged(ThreatEventArgs args) => OnThreatChanged?.Invoke(args);
        public static void FireStaminaDepleted(StaminaEventArgs args) => OnStaminaDepleted?.Invoke(args);
        public static void FireStaminaRecovered(StaminaEventArgs args) => OnStaminaRecovered?.Invoke(args);

        /// <summary>Clear all subscribers. Call on scene unload.</summary>
        public static void ClearAll()
        {
            OnEngagementStarted = null;
            OnEngagementBroken = null;
            OnDisengageAttempt = null;
            OnSkillStarted = null;
            OnSkillPhaseChanged = null;
            OnSkillCompleted = null;
            OnSkillInterrupted = null;
            OnSkillHit = null;
            OnCombatStateChanged = null;
            OnStanceChanged = null;
            OnDamageDealt = null;
            OnDisplacement = null;
            OnAgentDefeated = null;
            OnAgentStunned = null;
            OnFormationBroken = null;
            OnFormationFormed = null;
            OnThreatChanged = null;
            OnStaminaDepleted = null;
            OnStaminaRecovered = null;
        }
    }

    // --- Event Argument Structs ---

    public struct EngagementEventArgs
    {
        public CombatAgent Initiator;
        public CombatAgent Target;
        public float Distance;
    }

    public struct DisengageEventArgs
    {
        public CombatAgent Agent;
        public CombatAgent EngagedWith;
        public Vector3 DisengageDirection;
        public bool WasCareless; // triggers opportunity attack
    }

    public struct SkillEventArgs
    {
        public CombatAgent Caster;
        public Skills.CombatSkillSO Skill;
        public SkillPhase Phase;
        public Vector3 Direction;
    }

    public struct SkillHitEventArgs
    {
        public CombatAgent Attacker;
        public CombatAgent Defender;
        public Skills.CombatSkillSO Skill;
        public float Damage;
        public RelativeDirection HitDirection;
        public Vector3 HitPoint;
    }

    public struct CombatStateEventArgs
    {
        public CombatAgent Agent;
        public CombatState PreviousState;
        public CombatState NewState;
    }

    public struct StanceEventArgs
    {
        public CombatAgent Agent;
        public CombatStance PreviousStance;
        public CombatStance NewStance;
    }

    public struct DamageEventArgs
    {
        public CombatAgent Attacker;
        public CombatAgent Defender;
        public float RawDamage;
        public float FinalDamage;
        public RelativeDirection Direction;
        public bool WasBlocked;
        public bool WasCritical;
    }

    public struct DisplacementEventArgs
    {
        public CombatAgent Target;
        public CombatAgent Source;
        public Vector3 Force;
        public float Distance;
    }

    public struct FormationEventArgs
    {
        public Formation.FormationController Formation;
        public CombatAgent Agent;
    }

    public struct ThreatEventArgs
    {
        public CombatAgent Source;
        public CombatAgent Target;
        public float ThreatDelta;
        public float TotalThreat;
    }

    public struct StaminaEventArgs
    {
        public CombatAgent Agent;
        public float CurrentStamina;
        public float MaxStamina;
    }
}

