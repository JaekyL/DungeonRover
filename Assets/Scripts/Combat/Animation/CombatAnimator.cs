// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Animation Interface
// =============================================================================
// Clean interface between combat execution and animation.
// Decoupled so animation is never required for gameplay to function.
// =============================================================================

using Combat.Core;
using Combat.Skills;
using UnityEngine;

namespace Combat.Animation
{
    /// <summary>
    /// Interface for combat animation controllers.
    /// Implement this to connect any animation system to combat skills.
    /// </summary>
    public interface ICombatAnimator
    {
        void PlayWindup(string animationName, float duration);
        void PlayActive(string animationName, float duration);
        void PlayRecovery(string animationName, float duration);
        void PlayHitReaction(RelativeDirection hitFrom);
        void SetMovementSpeed(float speed);
        void SetCombatStance(CombatStance stance);
        bool IsRootMotionActive { get; }
    }

    /// <summary>
    /// Default Animator-based combat animation controller.
    /// Connects to Unity's Animator via triggers and parameters.
    /// </summary>
    [RequireComponent(typeof(CombatAgent))]
    public class CombatAnimatorController : MonoBehaviour, ICombatAnimator
    {
        [Header("References")]
        [SerializeField] private Animator _animator;

        [Header("Parameter Names")]
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _stanceParam = "Stance";
        [SerializeField] private string _attackTrigger = "Attack";
        [SerializeField] private string _hitTrigger = "Hit";
        [SerializeField] private string _phaseParam = "Phase";

        private CombatAgent _agent;
        public bool IsRootMotionActive => _animator != null && _animator.applyRootMotion;

        private void Awake()
        {
            _agent = GetComponent<CombatAgent>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            CombatEvents.OnSkillPhaseChanged += OnSkillPhase;
            CombatEvents.OnSkillHit += OnHit;
            CombatEvents.OnStanceChanged += OnStance;
        }

        private void OnDisable()
        {
            CombatEvents.OnSkillPhaseChanged -= OnSkillPhase;
            CombatEvents.OnSkillHit -= OnHit;
            CombatEvents.OnStanceChanged -= OnStance;
        }

        public void PlayWindup(string animationName, float duration)
        {
            if (_animator == null) return;
            _animator.SetInteger(_phaseParam, 1);
            if (!string.IsNullOrEmpty(animationName))
                _animator.SetTrigger(animationName);
            else
                _animator.SetTrigger(_attackTrigger);
        }

        public void PlayActive(string animationName, float duration)
        {
            if (_animator == null) return;
            _animator.SetInteger(_phaseParam, 2);
        }

        public void PlayRecovery(string animationName, float duration)
        {
            if (_animator == null) return;
            _animator.SetInteger(_phaseParam, 3);
        }

        public void PlayHitReaction(RelativeDirection hitFrom)
        {
            if (_animator == null) return;
            _animator.SetTrigger(_hitTrigger);
        }

        public void SetMovementSpeed(float speed)
        {
            if (_animator == null) return;
            _animator.SetFloat(_speedParam, speed);
        }

        public void SetCombatStance(CombatStance stance)
        {
            if (_animator == null) return;
            _animator.SetInteger(_stanceParam, (int)stance);
        }

        private void OnSkillPhase(SkillEventArgs args)
        {
            if (args.Caster != _agent) return;
            var anim = args.Skill.AnimationProfile;
            if (anim == null) return;

            switch (args.Phase)
            {
                case SkillPhase.Windup:
                    PlayWindup(anim.WindupAnimation, args.Skill.WindupDuration);
                    break;
                case SkillPhase.Active:
                    PlayActive(anim.ActiveAnimation, args.Skill.ActiveDuration);
                    break;
                case SkillPhase.Recovery:
                    PlayRecovery(anim.RecoveryAnimation, args.Skill.RecoveryDuration);
                    break;
            }
        }

        private void OnHit(SkillHitEventArgs args)
        {
            if (args.Defender != _agent) return;
            PlayHitReaction(args.HitDirection);
        }

        private void OnStance(StanceEventArgs args)
        {
            if (args.Agent != _agent) return;
            SetCombatStance(args.NewStance);
        }
    }
}

