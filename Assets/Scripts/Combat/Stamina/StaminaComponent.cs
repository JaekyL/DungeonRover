// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Stamina Component
// =============================================================================
// Manages stamina resource, regeneration, and exhaustion states.
// =============================================================================

using Combat.Core;
using UnityEngine;

namespace Combat.Stamina
{
    /// <summary>
    /// Stamina management component. Stamina gates combat actions and creates
    /// meaningful commitment/exhaustion dynamics.
    /// </summary>
    [RequireComponent(typeof(CombatAgent))]
    public class StaminaComponent : MonoBehaviour
    {
        // Stamina is managed directly by CombatAgent for simplicity.
        // This component provides additional stamina-related behaviors.

        private CombatAgent _owner;

        [Header("Exhaustion")]
        [SerializeField] private float _exhaustionSlowdown = 0.5f;
        [SerializeField] private float _exhaustionDamageBonus = 0.3f; // taken damage increase

        public bool IsExhausted => _owner != null && _owner.Stance == CombatStance.Exhausted;
        public float ExhaustionSlowdown => IsExhausted ? _exhaustionSlowdown : 0f;
        public float ExhaustionVulnerability => IsExhausted ? _exhaustionDamageBonus : 0f;

        private void Awake()
        {
            _owner = GetComponent<CombatAgent>();
        }

        /// <summary>Get the effective move speed accounting for exhaustion.</summary>
        public float GetEffectiveMoveSpeed(float baseSpeed)
        {
            if (IsExhausted)
                return baseSpeed * (1f - _exhaustionSlowdown);
            return baseSpeed;
        }
    }
}

