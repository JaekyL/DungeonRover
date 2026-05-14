// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Combat Memory
// =============================================================================
// Tracks combat history for an agent: who hit them, what skills were used,
// damage dealt/taken, and behavioral patterns of enemies.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace Combat.Core
{
    /// <summary>
    /// Combat memory system for a single agent. Tracks combat history
    /// to inform tactical AI decisions (e.g., "this enemy always attacks from behind").
    /// Serializable for save/load support.
    /// </summary>
    [System.Serializable]
    public class CombatMemory
    {
        [System.Serializable]
        public struct DamageRecord
        {
            public int AttackerInstanceId;
            public float Damage;
            public float Timestamp;
            public RelativeDirection Direction;
        }

        [System.Serializable]
        public struct SkillRecord
        {
            public int CasterInstanceId;
            public string SkillName;
            public float Timestamp;
            public bool WasInterrupted;
        }

        // --- Storage ---
        private readonly List<DamageRecord> _damagesTaken = new List<DamageRecord>();
        private readonly List<DamageRecord> _damagesDealt = new List<DamageRecord>();
        private readonly List<SkillRecord> _skillsUsed = new List<SkillRecord>();
        private readonly Dictionary<int, float> _threatMemory = new Dictionary<int, float>();
        private readonly Dictionary<int, int> _hitCounts = new Dictionary<int, int>();

        private const int MAX_RECORDS = 50;
        private float _combatStartTime;
        private float _totalDamageTaken;
        private float _totalDamageDealt;

        // --- Properties ---
        public float TotalDamageTaken => _totalDamageTaken;
        public float TotalDamageDealt => _totalDamageDealt;
        public float CombatStartTime => _combatStartTime;
        public IReadOnlyList<DamageRecord> DamagesTaken => _damagesTaken;

        // --- API ---

        public void StartCombat()
        {
            _combatStartTime = Time.time;
        }

        public void RecordDamageTaken(CombatAgent attacker, float damage, float time)
        {
            if (attacker == null) return;
            var record = new DamageRecord
            {
                AttackerInstanceId = attacker.GetInstanceID(),
                Damage = damage,
                Timestamp = time,
                Direction = RelativeDirection.Front
            };
            _damagesTaken.Add(record);
            _totalDamageTaken += damage;

            // Track threat from attacker
            int id = attacker.GetInstanceID();
            if (!_threatMemory.ContainsKey(id)) _threatMemory[id] = 0;
            _threatMemory[id] += damage;

            // Track hit count
            if (!_hitCounts.ContainsKey(id)) _hitCounts[id] = 0;
            _hitCounts[id]++;

            TrimIfNeeded(_damagesTaken);
        }

        public void RecordDamageDealt(CombatAgent target, float damage, float time)
        {
            if (target == null) return;
            _damagesDealt.Add(new DamageRecord
            {
                AttackerInstanceId = target.GetInstanceID(),
                Damage = damage,
                Timestamp = time
            });
            _totalDamageDealt += damage;
            TrimIfNeeded(_damagesDealt);
        }

        public void RecordSkillUsed(string skillName, bool wasInterrupted)
        {
            _skillsUsed.Add(new SkillRecord
            {
                SkillName = skillName,
                Timestamp = Time.time,
                WasInterrupted = wasInterrupted
            });
            TrimIfNeeded(_skillsUsed);
        }

        /// <summary>Get accumulated threat from a specific agent.</summary>
        public float GetThreatFrom(CombatAgent agent)
        {
            if (agent == null) return 0;
            _threatMemory.TryGetValue(agent.GetInstanceID(), out float threat);
            return threat;
        }

        /// <summary>Get number of times hit by a specific agent.</summary>
        public int GetHitCountFrom(CombatAgent agent)
        {
            if (agent == null) return 0;
            _hitCounts.TryGetValue(agent.GetInstanceID(), out int count);
            return count;
        }

        /// <summary>Get recent damage taken in the last N seconds.</summary>
        public float GetRecentDamageTaken(float windowSeconds)
        {
            float threshold = Time.time - windowSeconds;
            float total = 0;
            for (int i = _damagesTaken.Count - 1; i >= 0; i--)
            {
                if (_damagesTaken[i].Timestamp < threshold) break;
                total += _damagesTaken[i].Damage;
            }
            return total;
        }

        /// <summary>Clear all memory (e.g., after combat ends).</summary>
        public void Clear()
        {
            _damagesTaken.Clear();
            _damagesDealt.Clear();
            _skillsUsed.Clear();
            _threatMemory.Clear();
            _hitCounts.Clear();
            _totalDamageTaken = 0;
            _totalDamageDealt = 0;
        }

        private void TrimIfNeeded<T>(List<T> list)
        {
            if (list.Count > MAX_RECORDS)
                list.RemoveRange(0, list.Count - MAX_RECORDS);
        }
    }
}

