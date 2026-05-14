// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Combat Brain
// =============================================================================
// The autonomous decision-making component. Evaluates the combat context,
// selects skills, and directs combat movement using utility AI.
// =============================================================================

using System.Collections.Generic;
using Combat.AI;
using Combat.AI.Utility;
using Combat.Skills;
using UnityEngine;

namespace Combat.Core
{
    /// <summary>
    /// Autonomous combat brain. Runs each decision cycle to select the best
    /// combat action based on tactical context, equipped skills, and behavioral rules.
    /// 
    /// Inspired by FFXII gambits + Pillars of Eternity AI behaviors.
    /// </summary>
    [RequireComponent(typeof(CombatAgent))]
    public class CombatBrain : MonoBehaviour
    {
        [Header("Decision Making")]
        [SerializeField] private float _decisionInterval = 0.5f;
        [SerializeField] private CombatDoctrineSO _doctrine;

        [Header("Debug")]
        [SerializeField] private string _currentIntention = "Idle";
        [SerializeField] private string _lastSkillConsidered = "None";
        [SerializeField] private float _lastBestScore;

        private CombatAgent _agent;
        private CombatUtilityScorer _utilityScorer;
        private float _lastDecisionTime;

        // Current decision output
        private CombatDecision _currentDecision;

        public CombatDecision CurrentDecision => _currentDecision;
        public string CurrentIntention => _currentIntention;
        public CombatDoctrineSO Doctrine => _doctrine;

        private void Awake()
        {
            _agent = GetComponent<CombatAgent>();
            _utilityScorer = new CombatUtilityScorer();
        }

        private void Update()
        {
            if (!_agent.IsAlive || !_agent.CanAct) return;
            if (_agent.State == CombatState.Idle) return;

            if (Time.time - _lastDecisionTime >= _decisionInterval)
            {
                MakeDecision();
                _lastDecisionTime = Time.time;
            }
        }

        /// <summary>
        /// Core decision loop. Evaluates all available skills and selects the best action.
        /// </summary>
        private void MakeDecision()
        {
            var context = _agent.Context;
            if (context == null) return;

            // Skip if currently executing a skill
            if (_agent.SkillExecutor.IsExecuting) return;

            // Evaluate all equipped skills
            float bestScore = 0f;
            CombatSkillSO bestSkill = null;
            CombatAgent bestTarget = null;

            foreach (var skill in _agent.EquippedSkills)
            {
                if (skill == null) continue;
                if (!skill.CanExecute(context)) continue;

                // Find best target for this skill
                var candidates = GetTargetCandidates(skill, context);
                foreach (var candidate in candidates)
                {
                    float score = _utilityScorer.ScoreSkill(skill, context, candidate, _doctrine);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSkill = skill;
                        bestTarget = candidate;
                    }
                }

                // Self-targeted skills
                if (skill.TargetingProfile.IsSelfTargeted)
                {
                    float score = _utilityScorer.ScoreSkill(skill, context, _agent, _doctrine);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSkill = skill;
                        bestTarget = _agent;
                    }
                }
            }

            _lastBestScore = bestScore;

            // Minimum score threshold to act
            float actionThreshold = _doctrine != null ? _doctrine.ActionThreshold : 0.15f;
            if (bestScore >= actionThreshold && bestSkill != null)
            {
                _currentDecision = new CombatDecision
                {
                    ChosenSkill = bestSkill,
                    Target = bestTarget,
                    Score = bestScore,
                    DecisionTime = Time.time
                };

                _currentIntention = $"{bestSkill.SkillName} → {bestTarget?.AgentName ?? "self"}";
                _lastSkillConsidered = bestSkill.SkillName;

                ExecuteDecision(_currentDecision);
            }
            else
            {
                _currentIntention = "Repositioning";
                _currentDecision = new CombatDecision { ChosenSkill = null };
            }
        }

        private void ExecuteDecision(CombatDecision decision)
        {
            if (decision.ChosenSkill == null) return;

            // Check range - if too far, signal combat movement
            if (decision.Target != null && decision.Target != _agent)
            {
                float dist = Vector3.Distance(transform.position, decision.Target.transform.position);
                float requiredRange = decision.ChosenSkill.TargetingProfile.MaxRange;

                if (dist > requiredRange)
                {
                    // Need to close distance first
                    _currentIntention = $"Approaching for {decision.ChosenSkill.SkillName}";
                    return;
                }
            }

            // Execute the skill
            Vector3 direction = decision.Target != null && decision.Target != _agent
                ? (decision.Target.transform.position - transform.position).normalized
                : transform.forward;

            _agent.SkillExecutor.Execute(decision.ChosenSkill, decision.Target, direction);
        }

        private List<CombatAgent> GetTargetCandidates(CombatSkillSO skill, CombatContext context)
        {
            var candidates = new List<CombatAgent>();
            float maxRange = skill.TargetingProfile.MaxRange + 3f; // extra range for approach consideration

            if (skill.TargetingProfile.TargetsEnemies)
            {
                foreach (var enemy in context.NearbyEnemies)
                {
                    if (Vector3.Distance(transform.position, enemy.transform.position) <= maxRange)
                        candidates.Add(enemy);
                }
            }

            if (skill.TargetingProfile.TargetsAllies)
            {
                foreach (var ally in context.NearbyAllies)
                {
                    if (Vector3.Distance(transform.position, ally.transform.position) <= maxRange)
                        candidates.Add(ally);
                }
            }

            return candidates;
        }

        /// <summary>Set combat doctrine at runtime.</summary>
        public void SetDoctrine(CombatDoctrineSO doctrine)
        {
            _doctrine = doctrine;
        }
    }

    /// <summary>
    /// Output of a combat brain decision cycle.
    /// </summary>
    [System.Serializable]
    public struct CombatDecision
    {
        public CombatSkillSO ChosenSkill;
        public CombatAgent Target;
        public float Score;
        public float DecisionTime;

        public bool HasDecision => ChosenSkill != null;
    }
}

