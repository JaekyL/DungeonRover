// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Skill Executor
// =============================================================================
// Handles the execution pipeline of a combat skill: windup → active → recovery.
// Manages timing, movement, hit detection, and interruptibility.
// =============================================================================

using Combat.Core;
using Combat.Skills.Effects;
using UnityEngine;

namespace Combat.Skills
{
    /// <summary>
    /// Executes combat skills through their phase pipeline.
    /// One executor per CombatAgent. Handles timing, movement, and hit resolution.
    /// </summary>
    public class SkillExecutor
    {
        private readonly CombatAgent _owner;
        private CombatSkillSO _currentSkill;
        private CombatAgent _currentTarget;
        private Vector3 _direction;
        private SkillPhase _currentPhase = SkillPhase.None;
        private float _phaseTimer;
        private float _totalPhaseTime;
        private bool _hitResolved;

        // Cooldown tracking
        private readonly System.Collections.Generic.Dictionary<CombatSkillSO, float> _cooldowns =
            new System.Collections.Generic.Dictionary<CombatSkillSO, float>();

        public bool IsExecuting => _currentPhase != SkillPhase.None && _currentPhase != SkillPhase.Complete;
        public SkillPhase CurrentPhase => _currentPhase;
        public CombatSkillSO CurrentSkill => _currentSkill;
        public float PhaseProgress => _totalPhaseTime > 0 ? _phaseTimer / _totalPhaseTime : 0f;

        public SkillExecutor(CombatAgent owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Begin executing a skill. Returns false if skill cannot be executed.
        /// </summary>
        public bool Execute(CombatSkillSO skill, CombatAgent target, Vector3 direction)
        {
            if (IsExecuting) return false;
            if (skill == null) return false;
            if (!skill.CanExecute(_owner.Context)) return false;
            if (IsOnCooldown(skill)) return false;

            // Consume stamina
            if (!_owner.ConsumeStamina(skill.StaminaCost)) return false;

            _currentSkill = skill;
            _currentTarget = target;
            _direction = direction.normalized;
            _hitResolved = false;

            // Begin windup phase
            TransitionToPhase(SkillPhase.Windup);

            _owner.SetCombatState(CombatState.Executing);

            CombatEvents.FireSkillStarted(new SkillEventArgs
            {
                Caster = _owner,
                Skill = skill,
                Phase = SkillPhase.Windup,
                Direction = _direction
            });

            return true;
        }

        /// <summary>Update skill execution. Call from CombatAgent.Update().</summary>
        public void Update()
        {
            // Update cooldowns
            UpdateCooldowns();

            if (!IsExecuting) return;

            _phaseTimer += Time.deltaTime;

            // Apply movement during execution
            if (_currentSkill.MovementProfile != null && _currentSkill.MovementProfile.HasMovement)
            {
                ApplySkillMovement();
            }

            // Check phase transitions
            if (_phaseTimer >= _totalPhaseTime)
            {
                AdvancePhase();
            }

            // Resolve hits during active phase
            if (_currentPhase == SkillPhase.Active && !_hitResolved)
            {
                ResolveHits();
                _hitResolved = true;
            }
        }

        /// <summary>Interrupt the current skill execution.</summary>
        public void InterruptCurrent()
        {
            if (!IsExecuting) return;

            bool canInterrupt = (_currentPhase == SkillPhase.Windup && _currentSkill.InterruptibleDuringWindup) ||
                               (_currentPhase == SkillPhase.Recovery && _currentSkill.InterruptibleDuringRecovery);

            if (!canInterrupt && _currentPhase == SkillPhase.Active) return; // Can't interrupt active

            CombatEvents.FireSkillInterrupted(new SkillEventArgs
            {
                Caster = _owner,
                Skill = _currentSkill,
                Phase = _currentPhase,
                Direction = _direction
            });

            _owner.Memory.RecordSkillUsed(_currentSkill.SkillName, wasInterrupted: true);
            CompleteExecution();
        }

        /// <summary>Check if a skill is on cooldown.</summary>
        public bool IsOnCooldown(CombatSkillSO skill)
        {
            return _cooldowns.ContainsKey(skill) && _cooldowns[skill] > 0;
        }

        /// <summary>Get remaining cooldown time for a skill.</summary>
        public float GetCooldownRemaining(CombatSkillSO skill)
        {
            return _cooldowns.ContainsKey(skill) ? Mathf.Max(0, _cooldowns[skill]) : 0f;
        }

        // --- Private Methods ---

        private void TransitionToPhase(SkillPhase phase)
        {
            _currentPhase = phase;
            _phaseTimer = 0f;

            switch (phase)
            {
                case SkillPhase.Windup:
                    _totalPhaseTime = _currentSkill.WindupDuration;
                    break;
                case SkillPhase.Active:
                    _totalPhaseTime = _currentSkill.ActiveDuration;
                    break;
                case SkillPhase.Recovery:
                    _totalPhaseTime = _currentSkill.RecoveryDuration;
                    break;
                default:
                    _totalPhaseTime = 0f;
                    break;
            }

            CombatEvents.FireSkillPhaseChanged(new SkillEventArgs
            {
                Caster = _owner,
                Skill = _currentSkill,
                Phase = phase,
                Direction = _direction
            });
        }

        private void AdvancePhase()
        {
            switch (_currentPhase)
            {
                case SkillPhase.Windup:
                    TransitionToPhase(SkillPhase.Active);
                    break;
                case SkillPhase.Active:
                    TransitionToPhase(SkillPhase.Recovery);
                    break;
                case SkillPhase.Recovery:
                    OnSkillCompleted();
                    break;
            }
        }

        private void ResolveHits()
        {
            var targets = _currentSkill.GetTargetsInArea(
                _owner.transform.position, _direction, _owner);

            foreach (var target in targets)
            {
                // Calculate hit direction relative to defender
                RelativeDirection hitDir = DirectionUtility.GetRelativeDirection(
                    target.Facing != null ? target.Facing.Direction : target.transform.forward,
                    (_owner.transform.position - target.transform.position).normalized
                );

                // Apply all effects
                foreach (var effect in _currentSkill.Effects)
                {
                    if (effect != null)
                    {
                        var effectContext = new SkillEffectContext
                        {
                            Caster = _owner,
                            Target = target,
                            Skill = _currentSkill,
                            HitDirection = hitDir,
                            SkillDirection = _direction,
                            HitPoint = target.transform.position
                        };
                        effect.Apply(effectContext);
                    }
                }

                CombatEvents.FireSkillHit(new SkillHitEventArgs
                {
                    Attacker = _owner,
                    Defender = target,
                    Skill = _currentSkill,
                    HitDirection = hitDir,
                    HitPoint = target.transform.position
                });
            }
        }

        private void ApplySkillMovement()
        {
            var profile = _currentSkill.MovementProfile;
            if (_currentPhase != SkillPhase.Active) return;

            // Calculate movement direction relative to facing
            Vector3 moveDir = _owner.transform.TransformDirection(profile.MovementDirection).normalized;
            float speed = profile.MovementDistance / _currentSkill.ActiveDuration * profile.SpeedMultiplier;
            _owner.transform.position += moveDir * speed * Time.deltaTime;

            // Lock facing if specified
            if (profile.LocksFacing && _owner.Facing != null)
            {
                _owner.Facing.SetDirection(_direction);
            }
        }

        private void OnSkillCompleted()
        {
            CombatEvents.FireSkillCompleted(new SkillEventArgs
            {
                Caster = _owner,
                Skill = _currentSkill,
                Phase = SkillPhase.Complete,
                Direction = _direction
            });

            _owner.Memory.RecordSkillUsed(_currentSkill.SkillName, wasInterrupted: false);

            // Set cooldown
            _cooldowns[_currentSkill] = _currentSkill.CooldownDuration;

            CompleteExecution();
        }

        private void CompleteExecution()
        {
            _currentSkill = null;
            _currentTarget = null;
            _currentPhase = SkillPhase.None;
            _phaseTimer = 0f;
            _owner.SetCombatState(CombatState.InCombat);
        }

        private void UpdateCooldowns()
        {
            var keys = new System.Collections.Generic.List<CombatSkillSO>(_cooldowns.Keys);
            foreach (var key in keys)
            {
                _cooldowns[key] -= Time.deltaTime;
                if (_cooldowns[key] <= 0)
                    _cooldowns.Remove(key);
            }
        }
    }
}

