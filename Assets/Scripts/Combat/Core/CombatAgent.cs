// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Combat Agent
// =============================================================================
// The central MonoBehaviour component that gives a GameObject combat capabilities.
// Integrates with the existing TraversalAIController and coordinates subsystems.
// =============================================================================

using System.Collections.Generic;
using Combat.Engagement;
using Combat.Facing;
using Combat.Formation;
using Combat.Skills;
using Combat.Stamina;
using Combat.Threat;
using Combat.Weapons;
using UnityEngine;

namespace Combat.Core
{
    /// <summary>
    /// Core combat component. Attach to any AI agent that should participate in combat.
    /// Orchestrates combat subsystems and provides the unified combat interface.
    /// 
    /// Integration: Works alongside TraversalAIController. When combat is active,
    /// combat movement overrides traversal movement but uses the same spatial systems.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatAgent : MonoBehaviour
    {
        // --- Configuration ---
        [Header("Identity")]
        [SerializeField] private Faction _faction = Faction.PlayerParty;
        [SerializeField] private string _agentName = "Agent";

        [Header("Stats")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth = 100f;
        [SerializeField] private float _maxStamina = 100f;
        [SerializeField] private float _currentStamina = 100f;
        [SerializeField] private float _staminaRegenRate = 15f;
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _combatMoveSpeed = 3f;

        [Header("Equipment")]
        [SerializeField] private WeaponProfileSO _equippedWeapon;
        [SerializeField] private List<CombatSkillSO> _equippedSkills = new List<CombatSkillSO>();

        [Header("Combat State (Read Only)")]
        [SerializeField] private CombatState _combatState = CombatState.Idle;
        [SerializeField] private CombatStance _stance = CombatStance.Neutral;
        [SerializeField] private FormationRole _formationRole = FormationRole.Free;

        // --- Sub-components (automatically resolved) ---
        private FacingComponent _facing;
        private EngagementTracker _engagement;
        private StaminaComponent _stamina;
        private ThreatTracker _threat;
        private CombatMemory _memory;
        private CombatBrain _brain;

        // --- Runtime ---
        private CombatContext _cachedContext;
        private Skills.SkillExecutor _skillExecutor;
        private float _lastContextUpdateTime;
        private float _stunEndTime;
        private static readonly float CONTEXT_UPDATE_INTERVAL = 0.1f;

        // --- Public Properties ---
        public string AgentName => _agentName;
        public Faction Faction => _faction;
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public float HealthRatio => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
        public float MaxStamina => _maxStamina;
        public float CurrentStamina => _currentStamina;
        public float StaminaRatio => _maxStamina > 0 ? _currentStamina / _maxStamina : 0f;
        public float MoveSpeed => _combatState == CombatState.Idle ? _moveSpeed : _combatMoveSpeed;
        public CombatState State => _combatState;
        public CombatStance Stance => _stance;
        public FormationRole Role => _formationRole;
        public WeaponProfileSO Weapon => _equippedWeapon;
        public IReadOnlyList<CombatSkillSO> EquippedSkills => _equippedSkills;
        public FacingComponent Facing => _facing;
        public EngagementTracker Engagement => _engagement;
        public StaminaComponent Stamina => _stamina;
        public ThreatTracker Threat => _threat;
        public CombatMemory Memory => _memory;
        public CombatBrain Brain => _brain;
        public CombatContext Context => _cachedContext;
        public Skills.SkillExecutor SkillExecutor => _skillExecutor;
        public bool IsAlive => _currentHealth > 0;
        public bool CanAct => _combatState != CombatState.Stunned &&
                              _combatState != CombatState.Recovering &&
                              IsAlive;
        public float WeaponReach => _equippedWeapon != null ? _equippedWeapon.Reach : 1.5f;

        // --- Lifecycle ---

        private void Awake()
        {
            // Resolve or add sub-components
            _facing = GetOrAddComponent<FacingComponent>();
            _engagement = GetOrAddComponent<EngagementTracker>();
            _stamina = GetOrAddComponent<StaminaComponent>();
            _threat = GetOrAddComponent<ThreatTracker>();
            _memory = new CombatMemory();
            _brain = GetOrAddComponent<CombatBrain>();
            _skillExecutor = new Skills.SkillExecutor(this);
            _cachedContext = new CombatContext();
        }

        private void OnEnable()
        {
            CombatAgentRegistry.Register(this);
        }

        private void OnDisable()
        {
            CombatAgentRegistry.Unregister(this);
        }

        private void Update()
        {
            if (!IsAlive) return;

            // Update stun timer
            if (_combatState == CombatState.Stunned && Time.time >= _stunEndTime)
            {
                SetCombatState(CombatState.InCombat);
            }

            // Regenerate stamina
            if (_combatState != CombatState.Executing)
            {
                RegenerateStamina();
            }

            // Update context periodically
            if (Time.time - _lastContextUpdateTime >= CONTEXT_UPDATE_INTERVAL)
            {
                UpdateCombatContext();
                _lastContextUpdateTime = Time.time;
            }

            // Update skill execution
            _skillExecutor.Update();
        }

        // --- Public API ---

        /// <summary>Set combat state with event notification.</summary>
        public void SetCombatState(CombatState newState)
        {
            if (_combatState == newState) return;
            var prev = _combatState;
            _combatState = newState;
            CombatEvents.FireCombatStateChanged(new CombatStateEventArgs
            {
                Agent = this,
                PreviousState = prev,
                NewState = newState
            });
        }

        /// <summary>Change combat stance.</summary>
        public void SetStance(CombatStance newStance)
        {
            if (_stance == newStance) return;
            var prev = _stance;
            _stance = newStance;
            CombatEvents.FireStanceChanged(new StanceEventArgs
            {
                Agent = this,
                PreviousStance = prev,
                NewStance = newStance
            });
        }

        /// <summary>Set formation role.</summary>
        public void SetFormationRole(FormationRole role)
        {
            _formationRole = role;
        }

        /// <summary>Apply damage to this agent.</summary>
        public void TakeDamage(float damage, CombatAgent attacker, RelativeDirection direction)
        {
            // Directional defense modifiers
            float modifier = GetDirectionalDefenseModifier(direction);
            float finalDamage = damage * modifier;

            _currentHealth = Mathf.Max(0, _currentHealth - finalDamage);

            // Record in memory
            _memory.RecordDamageTaken(attacker, finalDamage, Time.time);

            // Fire event
            CombatEvents.FireDamageDealt(new DamageEventArgs
            {
                Attacker = attacker,
                Defender = this,
                RawDamage = damage,
                FinalDamage = finalDamage,
                Direction = direction,
                WasBlocked = modifier < 1f,
                WasCritical = DirectionUtility.IsFromBehind(direction)
            });

            // Defeat check
            if (_currentHealth <= 0)
            {
                SetCombatState(CombatState.Idle);
                CombatEvents.FireAgentDefeated(this);
            }
        }

        /// <summary>Apply stun effect.</summary>
        public void ApplyStun(float duration)
        {
            _stunEndTime = Time.time + duration;
            SetCombatState(CombatState.Stunned);
            _skillExecutor.InterruptCurrent();
            CombatEvents.FireAgentStunned(this);
        }

        /// <summary>Apply displacement force to this agent.</summary>
        public void ApplyDisplacement(Vector3 force, CombatAgent source)
        {
            // Simple displacement - move over time would be better with a coroutine
            transform.position += force;
            CombatEvents.FireDisplacement(new DisplacementEventArgs
            {
                Target = this,
                Source = source,
                Force = force,
                Distance = force.magnitude
            });
        }

        /// <summary>Consume stamina for an action. Returns false if insufficient.</summary>
        public bool ConsumeStamina(float amount)
        {
            if (_currentStamina < amount) return false;
            _currentStamina -= amount;
            if (_currentStamina <= 0)
            {
                _currentStamina = 0;
                SetStance(CombatStance.Exhausted);
                CombatEvents.FireStaminaDepleted(new StaminaEventArgs
                {
                    Agent = this,
                    CurrentStamina = _currentStamina,
                    MaxStamina = _maxStamina
                });
            }
            return true;
        }

        /// <summary>Check if this agent is hostile to another.</summary>
        public bool IsHostileTo(CombatAgent other)
        {
            if (other == null || other == this) return false;
            return _faction != other.Faction && _faction != Faction.Neutral && other.Faction != Faction.Neutral;
        }

        /// <summary>Check if this agent is allied with another.</summary>
        public bool IsAlliedWith(CombatAgent other)
        {
            if (other == null || other == this) return false;
            return _faction == other.Faction;
        }

        /// <summary>Equip a skill at runtime.</summary>
        public void EquipSkill(CombatSkillSO skill)
        {
            if (!_equippedSkills.Contains(skill))
                _equippedSkills.Add(skill);
        }

        /// <summary>Unequip a skill at runtime.</summary>
        public void UnequipSkill(CombatSkillSO skill)
        {
            _equippedSkills.Remove(skill);
        }

        // --- Private Methods ---

        private void RegenerateStamina()
        {
            float regenMultiplier = _stance == CombatStance.Defensive ? 1.5f :
                                    _stance == CombatStance.Aggressive ? 0.7f : 1f;
            _currentStamina = Mathf.Min(_maxStamina,
                _currentStamina + _staminaRegenRate * regenMultiplier * Time.deltaTime);

            // Exit exhausted stance when recovered
            if (_stance == CombatStance.Exhausted && _currentStamina > _maxStamina * 0.3f)
            {
                SetStance(CombatStance.Neutral);
                CombatEvents.FireStaminaRecovered(new StaminaEventArgs
                {
                    Agent = this,
                    CurrentStamina = _currentStamina,
                    MaxStamina = _maxStamina
                });
            }
        }

        private float GetDirectionalDefenseModifier(RelativeDirection direction)
        {
            // Attacks from behind deal more damage, frontal can be blocked
            switch (direction)
            {
                case RelativeDirection.Rear:
                case RelativeDirection.RearLeft:
                case RelativeDirection.RearRight:
                    return 1.5f; // backstab bonus for attacker
                case RelativeDirection.Front:
                    return _stance == CombatStance.Defensive ? 0.5f :
                           _stance == CombatStance.Braced ? 0.3f : 1.0f;
                default:
                    return 1.0f;
            }
        }

        private void UpdateCombatContext()
        {
            _cachedContext.Self = this;
            _cachedContext.Position = transform.position;
            _cachedContext.FacingDirection = _facing != null ? _facing.Direction : transform.forward;
            _cachedContext.CurrentStance = _stance;
            _cachedContext.CurrentState = _combatState;
            _cachedContext.CurrentStamina = _currentStamina;
            _cachedContext.MaxStamina = _maxStamina;
            _cachedContext.CurrentHealth = _currentHealth;
            _cachedContext.MaxHealth = _maxHealth;
            _cachedContext.EquippedWeapon = _equippedWeapon != null ? _equippedWeapon.WeaponType : WeaponType.Unarmed;
            _cachedContext.WeaponReach = WeaponReach;

            // Populate from engagement tracker
            if (_engagement != null)
            {
                _cachedContext.EngagedEnemies.Clear();
                _cachedContext.EngagedEnemies.AddRange(_engagement.EngagedEnemies);
                _cachedContext.PrimaryTarget = _engagement.PrimaryTarget;
            }

            // Populate nearby agents from registry
            PopulateNearbyAgents();
        }

        private void PopulateNearbyAgents()
        {
            float awarenessRadius = 15f;
            _cachedContext.NearbyAllies.Clear();
            _cachedContext.NearbyEnemies.Clear();
            _cachedContext.NearestEnemyDistance = float.MaxValue;
            _cachedContext.NearestAllyDistance = float.MaxValue;

            foreach (var agent in CombatAgentRegistry.AllAgents)
            {
                if (agent == this || !agent.IsAlive) continue;
                float dist = Vector3.Distance(transform.position, agent.transform.position);
                if (dist > awarenessRadius) continue;

                if (IsAlliedWith(agent))
                {
                    _cachedContext.NearbyAllies.Add(agent);
                    if (dist < _cachedContext.NearestAllyDistance)
                        _cachedContext.NearestAllyDistance = dist;
                }
                else if (IsHostileTo(agent))
                {
                    _cachedContext.NearbyEnemies.Add(agent);
                    if (dist < _cachedContext.NearestEnemyDistance)
                        _cachedContext.NearestEnemyDistance = dist;
                }
            }

            // Density metrics
            _cachedContext.LocalEnemyDensity = 0;
            _cachedContext.LocalAllyDensity = 0;
            foreach (var e in _cachedContext.NearbyEnemies)
            {
                if (Vector3.Distance(transform.position, e.transform.position) <= WeaponReach * 2f)
                    _cachedContext.LocalEnemyDensity++;
            }
            foreach (var a in _cachedContext.NearbyAllies)
            {
                if (Vector3.Distance(transform.position, a.transform.position) <= WeaponReach * 2f)
                    _cachedContext.LocalAllyDensity++;
            }
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            var comp = GetComponent<T>();
            if (comp == null) comp = gameObject.AddComponent<T>();
            return comp;
        }

        // --- Debug ---
        private void OnDrawGizmosSelected()
        {
            // Weapon reach
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, WeaponReach);

            // Facing direction
            if (_facing != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position + Vector3.up, _facing.Direction * 2f);
            }
        }
    }
}

