// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Formation System
// =============================================================================
// Autonomous formation-aware combat positioning. Agents maintain coherent
// formations while fighting, adapting to terrain and combat pressure.
// =============================================================================

using System.Collections.Generic;
using Combat.Core;
using UnityEngine;

namespace Combat.Formation
{
    /// <summary>
    /// Formation controller manages a group of combat agents in formation.
    /// Integrates with traversal movement for seamless formation → combat transitions.
    /// </summary>
    public class FormationController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private FormationType _formationType = FormationType.Line;
        [SerializeField] private float _spacing = 2f;
        [SerializeField] private float _cohesion = 0.7f;

        [Header("State")]
        [SerializeField] private float _integrity = 1f;

        private readonly List<FormationSlot> _slots = new List<FormationSlot>();
        private Vector3 _formationCenter;
        private Vector3 _formationFacing;
        private bool _inCombat;

        public float Integrity => _integrity;
        public FormationType CurrentFormation => _formationType;
        public IReadOnlyList<FormationSlot> Slots => _slots;
        public Vector3 Center => _formationCenter;

        /// <summary>Add an agent to the formation.</summary>
        public void AddAgent(CombatAgent agent, FormationRole role)
        {
            var slot = new FormationSlot
            {
                Agent = agent,
                Role = role,
                LocalOffset = CalculateSlotOffset(_slots.Count, role),
                IsOccupied = true
            };
            _slots.Add(slot);
            agent.SetFormationRole(role);
        }

        /// <summary>Remove an agent from the formation.</summary>
        public void RemoveAgent(CombatAgent agent)
        {
            _slots.RemoveAll(s => s.Agent == agent);
            RecalculateSlots();
        }

        private void Update()
        {
            if (_slots.Count == 0) return;

            UpdateFormationCenter();
            UpdateSlotPositions();
            CalculateIntegrity();
        }

        private void UpdateFormationCenter()
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var slot in _slots)
            {
                if (slot.Agent != null && slot.Agent.IsAlive)
                {
                    sum += slot.Agent.transform.position;
                    count++;
                }
            }
            if (count > 0)
                _formationCenter = sum / count;
        }

        private void UpdateSlotPositions()
        {
            // Update world positions from formation layout
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Agent == null || !slot.Agent.IsAlive) continue;

                // Calculate desired world position based on formation type
                Vector3 desiredPos = _formationCenter +
                    Quaternion.LookRotation(_formationFacing.sqrMagnitude > 0.01f ? _formationFacing : Vector3.forward) *
                    slot.LocalOffset;

                slot.DesiredWorldPosition = desiredPos;
                slot.DistanceFromSlot = Vector3.Distance(slot.Agent.transform.position, desiredPos);
            }
        }

        private void CalculateIntegrity()
        {
            if (_slots.Count == 0) { _integrity = 0; return; }

            float totalDeviation = 0;
            int count = 0;
            foreach (var slot in _slots)
            {
                if (slot.Agent == null || !slot.Agent.IsAlive) continue;
                totalDeviation += Mathf.Clamp01(slot.DistanceFromSlot / (_spacing * 2f));
                count++;
            }

            _integrity = count > 0 ? 1f - (totalDeviation / count) : 0f;
        }

        /// <summary>Get the desired position for a specific agent.</summary>
        public Vector3 GetDesiredPosition(CombatAgent agent)
        {
            foreach (var slot in _slots)
            {
                if (slot.Agent == agent)
                    return slot.DesiredWorldPosition;
            }
            return agent.transform.position;
        }

        /// <summary>Set the formation's facing direction.</summary>
        public void SetFacing(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
                _formationFacing = direction.normalized;
        }

        /// <summary>Switch to a different formation type.</summary>
        public void SetFormationType(FormationType type)
        {
            _formationType = type;
            RecalculateSlots();
        }

        /// <summary>Enter combat mode - adapt formation for fighting.</summary>
        public void EnterCombat(Vector3 threatDirection)
        {
            _inCombat = true;
            SetFacing(threatDirection);

            // Compress formation in corridors
            // Widen formation in open spaces
        }

        private void RecalculateSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].LocalOffset = CalculateSlotOffset(i, _slots[i].Role);
            }
        }

        private Vector3 CalculateSlotOffset(int index, FormationRole role)
        {
            switch (_formationType)
            {
                case FormationType.Line:
                    return CalculateLineOffset(index, role);
                case FormationType.Wedge:
                    return CalculateWedgeOffset(index, role);
                case FormationType.Circle:
                    return CalculateCircleOffset(index);
                case FormationType.Column:
                    return CalculateColumnOffset(index, role);
                case FormationType.Scatter:
                    return CalculateScatterOffset(index);
                default:
                    return Vector3.zero;
            }
        }

        private Vector3 CalculateLineOffset(int index, FormationRole role)
        {
            float row = role == FormationRole.Frontline ? 0 :
                        role == FormationRole.Backline ? -_spacing : -_spacing * 0.5f;
            float col = (index - _slots.Count * 0.5f) * _spacing;
            return new Vector3(col, 0, row);
        }

        private Vector3 CalculateWedgeOffset(int index, FormationRole role)
        {
            if (index == 0) return new Vector3(0, 0, _spacing); // Point
            float side = (index % 2 == 0) ? 1f : -1f;
            int depth = (index + 1) / 2;
            return new Vector3(side * depth * _spacing * 0.7f, 0, -depth * _spacing * 0.5f);
        }

        private Vector3 CalculateCircleOffset(int index)
        {
            float angle = (index / (float)_slots.Count) * 360f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle) * _spacing, 0, Mathf.Cos(angle) * _spacing);
        }

        private Vector3 CalculateColumnOffset(int index, FormationRole role)
        {
            float col = (index % 2 == 0) ? -_spacing * 0.5f : _spacing * 0.5f;
            float row = -(index / 2) * _spacing;
            return new Vector3(col, 0, row);
        }

        private Vector3 CalculateScatterOffset(int index)
        {
            // Pseudo-random but deterministic scatter
            float angle = index * 137.5f * Mathf.Deg2Rad; // Golden angle
            float dist = _spacing * (0.5f + index * 0.3f);
            return new Vector3(Mathf.Sin(angle) * dist, 0, Mathf.Cos(angle) * dist);
        }
    }

    /// <summary>
    /// A slot in a formation assigned to a specific agent.
    /// </summary>
    [System.Serializable]
    public class FormationSlot
    {
        public CombatAgent Agent;
        public FormationRole Role;
        public Vector3 LocalOffset;
        public Vector3 DesiredWorldPosition;
        public float DistanceFromSlot;
        public bool IsOccupied;
    }

    /// <summary>Formation layout types.</summary>
    public enum FormationType
    {
        Line,       // Horizontal line facing enemy
        Wedge,      // V-shape, good for charges
        Circle,     // Defensive circle
        Column,     // Two-wide column for corridors
        Scatter     // Loose formation, good vs AoE
    }
}

