// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Engagement System
// =============================================================================
// Tracks which agents are "engaged" in melee combat with each other.
// Engagement affects pathfinding, disengagement penalties, and frontline behavior.
// =============================================================================

using System.Collections.Generic;
using Combat.Core;
using UnityEngine;

namespace Combat.Engagement
{
    /// <summary>
    /// Tracks engagement state for a combat agent.
    /// Engagement = being within melee range and actively in combat with a target.
    /// </summary>
    [RequireComponent(typeof(CombatAgent))]
    public class EngagementTracker : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _engagementRange = 3f;
        [SerializeField] private float _disengageRange = 4.5f;
        [SerializeField] private float _engagementCheckInterval = 0.2f;

        [Header("State (Read Only)")]
        [SerializeField] private int _engagedCount;

        private CombatAgent _owner;
        private readonly List<CombatAgent> _engagedEnemies = new List<CombatAgent>();
        private CombatAgent _primaryTarget;
        private float _lastCheckTime;

        public IReadOnlyList<CombatAgent> EngagedEnemies => _engagedEnemies;
        public CombatAgent PrimaryTarget => _primaryTarget;
        public int EngagedCount => _engagedEnemies.Count;
        public bool IsEngaged => _engagedEnemies.Count > 0;
        public float EngagementRange => _engagementRange;

        private void Awake()
        {
            _owner = GetComponent<CombatAgent>();
        }

        private void Update()
        {
            if (Time.time - _lastCheckTime < _engagementCheckInterval) return;
            _lastCheckTime = Time.time;
            UpdateEngagements();
            _engagedCount = _engagedEnemies.Count;
        }

        private void UpdateEngagements()
        {
            // Check for new engagements
            foreach (var agent in CombatAgentRegistry.AllAgents)
            {
                if (agent == _owner || !agent.IsAlive) continue;
                if (!_owner.IsHostileTo(agent)) continue;

                float dist = Vector3.Distance(transform.position, agent.transform.position);

                if (dist <= _engagementRange && !_engagedEnemies.Contains(agent))
                {
                    // New engagement
                    _engagedEnemies.Add(agent);
                    _owner.SetCombatState(CombatState.InCombat);
                    CombatEvents.FireEngagementStarted(new EngagementEventArgs
                    {
                        Initiator = _owner,
                        Target = agent,
                        Distance = dist
                    });
                }
                else if (dist > _disengageRange && _engagedEnemies.Contains(agent))
                {
                    // Disengagement (by distance)
                    _engagedEnemies.Remove(agent);
                    CombatEvents.FireEngagementBroken(new EngagementEventArgs
                    {
                        Initiator = _owner,
                        Target = agent,
                        Distance = dist
                    });
                }
            }

            // Remove dead enemies
            _engagedEnemies.RemoveAll(e => e == null || !e.IsAlive);

            // Update primary target (closest engaged)
            UpdatePrimaryTarget();

            // Exit combat if no engagements and no nearby enemies
            if (_engagedEnemies.Count == 0 && _owner.State == CombatState.InCombat)
            {
                bool anyNearby = false;
                foreach (var agent in CombatAgentRegistry.AllAgents)
                {
                    if (agent == _owner || !agent.IsAlive || !_owner.IsHostileTo(agent)) continue;
                    if (Vector3.Distance(transform.position, agent.transform.position) < _disengageRange * 2)
                    {
                        anyNearby = true;
                        break;
                    }
                }
                if (!anyNearby)
                    _owner.SetCombatState(CombatState.Idle);
            }
        }

        private void UpdatePrimaryTarget()
        {
            if (_engagedEnemies.Count == 0)
            {
                _primaryTarget = null;
                return;
            }

            // Primary target = closest engaged enemy
            float bestDist = float.MaxValue;
            CombatAgent best = null;
            foreach (var enemy in _engagedEnemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = enemy;
                }
            }
            _primaryTarget = best;
        }

        /// <summary>
        /// Attempt to disengage from an enemy. May trigger opportunity attack.
        /// </summary>
        public void AttemptDisengage(CombatAgent from, Vector3 direction, bool careful = false)
        {
            if (!_engagedEnemies.Contains(from)) return;

            bool careless = !careful;
            CombatEvents.FireDisengageAttempt(new DisengageEventArgs
            {
                Agent = _owner,
                EngagedWith = from,
                DisengageDirection = direction,
                WasCareless = careless
            });

            _engagedEnemies.Remove(from);
        }

        /// <summary>Force engage a target (e.g., taunt/pull).</summary>
        public void ForceEngage(CombatAgent target)
        {
            if (!_engagedEnemies.Contains(target))
                _engagedEnemies.Add(target);
            _owner.SetCombatState(CombatState.InCombat);
        }
    }

    /// <summary>
    /// Static engagement zone queries.
    /// </summary>
    public static class EngagementZone
    {
        /// <summary>Check if position is within any enemy's engagement zone.</summary>
        public static bool IsPositionEngaged(Vector3 position, Faction myFaction)
        {
            foreach (var agent in CombatAgentRegistry.AllAgents)
            {
                if (!agent.IsAlive || agent.Faction == myFaction) continue;
                if (agent.Engagement == null) continue;
                float dist = Vector3.Distance(position, agent.transform.position);
                if (dist <= agent.Engagement.EngagementRange)
                    return true;
            }
            return false;
        }

        /// <summary>Get the number of enemies engaging a position.</summary>
        public static int GetEngagementPressure(Vector3 position, Faction myFaction)
        {
            int count = 0;
            foreach (var agent in CombatAgentRegistry.AllAgents)
            {
                if (!agent.IsAlive || agent.Faction == myFaction) continue;
                if (agent.Engagement == null) continue;
                float dist = Vector3.Distance(position, agent.transform.position);
                if (dist <= agent.Engagement.EngagementRange)
                    count++;
            }
            return count;
        }
    }
}

