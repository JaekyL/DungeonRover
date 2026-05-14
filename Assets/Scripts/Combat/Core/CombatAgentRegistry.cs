// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Combat Agent Registry
// =============================================================================
// Global registry of active combat agents for spatial queries and targeting.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace Combat.Core
{
    /// <summary>
    /// Static registry of all active combat agents in the scene.
    /// Enables efficient spatial queries without requiring Physics overlaps.
    /// Thread-safe for future Job System integration.
    /// </summary>
    public static class CombatAgentRegistry
    {
        private static readonly List<CombatAgent> _agents = new List<CombatAgent>();

        public static IReadOnlyList<CombatAgent> AllAgents => _agents;

        public static void Register(CombatAgent agent)
        {
            if (!_agents.Contains(agent))
                _agents.Add(agent);
        }

        public static void Unregister(CombatAgent agent)
        {
            _agents.Remove(agent);
        }

        /// <summary>Get all agents within range of a position.</summary>
        public static List<CombatAgent> GetAgentsInRange(Vector3 position, float range)
        {
            var result = new List<CombatAgent>();
            float sqrRange = range * range;
            foreach (var agent in _agents)
            {
                if (!agent.IsAlive) continue;
                if ((agent.transform.position - position).sqrMagnitude <= sqrRange)
                    result.Add(agent);
            }
            return result;
        }

        /// <summary>Get all enemies of a faction within range.</summary>
        public static List<CombatAgent> GetEnemiesInRange(Vector3 position, float range, Faction myFaction)
        {
            var result = new List<CombatAgent>();
            float sqrRange = range * range;
            foreach (var agent in _agents)
            {
                if (!agent.IsAlive) continue;
                if (agent.Faction == myFaction || agent.Faction == Faction.Neutral) continue;
                if ((agent.transform.position - position).sqrMagnitude <= sqrRange)
                    result.Add(agent);
            }
            return result;
        }

        /// <summary>Get all allies of a faction within range.</summary>
        public static List<CombatAgent> GetAlliesInRange(Vector3 position, float range, Faction myFaction)
        {
            var result = new List<CombatAgent>();
            float sqrRange = range * range;
            foreach (var agent in _agents)
            {
                if (!agent.IsAlive) continue;
                if (agent.Faction != myFaction) continue;
                if ((agent.transform.position - position).sqrMagnitude <= sqrRange)
                    result.Add(agent);
            }
            return result;
        }

        /// <summary>Get the nearest enemy to a position.</summary>
        public static CombatAgent GetNearestEnemy(Vector3 position, Faction myFaction)
        {
            CombatAgent nearest = null;
            float bestDist = float.MaxValue;
            foreach (var agent in _agents)
            {
                if (!agent.IsAlive || agent.Faction == myFaction || agent.Faction == Faction.Neutral) continue;
                float dist = (agent.transform.position - position).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = agent;
                }
            }
            return nearest;
        }

        /// <summary>Clear registry (call on scene unload).</summary>
        public static void Clear() => _agents.Clear();
    }
}

