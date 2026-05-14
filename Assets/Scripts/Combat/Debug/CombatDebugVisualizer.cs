// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Combat Debug Visualization
// =============================================================================
// Gizmo-based runtime visualization for all combat systems.
// Essential for understanding AI decisions, engagement zones, and hit volumes.
// =============================================================================

using Combat.Core;
using Combat.Engagement;
using Combat.Formation;
using Combat.Movement;
using Combat.Skills;
using Combat.Spatial;
using UnityEngine;

namespace Combat.Debug
{
    /// <summary>
    /// Comprehensive combat debug visualizer. Draws gizmos for:
    /// - Engagement zones and connections
    /// - Facing arcs and attack areas
    /// - Utility scores
    /// - Target selection
    /// - Formation overlays
    /// - Weapon reach
    /// - Movement intentions
    /// - Current skill execution
    /// </summary>
    [RequireComponent(typeof(CombatAgent))]
    public class CombatDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization Toggles")]
        [SerializeField] private bool _showEngagement = true;
        [SerializeField] private bool _showFacingArc = true;
        [SerializeField] private bool _showWeaponReach = true;
        [SerializeField] private bool _showUtilityScore = true;
        [SerializeField] private bool _showTargetSelection = true;
        [SerializeField] private bool _showMovementIntent = true;
        [SerializeField] private bool _showSkillExecution = true;
        [SerializeField] private bool _showThreat = true;
        [SerializeField] private bool _showFormation = true;

        [Header("Colors")]
        [SerializeField] private Color _engagementColor = new Color(1f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color _facingArcColor = new Color(0.3f, 0.3f, 1f, 0.3f);
        [SerializeField] private Color _weaponReachColor = new Color(1f, 1f, 0f, 0.2f);
        [SerializeField] private Color _targetColor = Color.red;
        [SerializeField] private Color _movementColor = Color.cyan;
        [SerializeField] private Color _skillColor = new Color(1f, 0.5f, 0f, 0.5f);
        [SerializeField] private Color _formationColor = new Color(0f, 1f, 0f, 0.3f);

        private CombatAgent _agent;

        private void Awake()
        {
            _agent = GetComponent<CombatAgent>();
        }

        private void OnDrawGizmos()
        {
            if (_agent == null) _agent = GetComponent<CombatAgent>();
            if (_agent == null || !_agent.IsAlive) return;

            Vector3 pos = transform.position + Vector3.up * 0.1f;

            if (_showWeaponReach)
                DrawWeaponReach(pos);

            if (_showEngagement)
                DrawEngagement(pos);

            if (_showFacingArc)
                DrawFacingArc(pos);

            if (_showTargetSelection)
                DrawTargetSelection(pos);

            if (_showMovementIntent)
                DrawMovementIntent(pos);

            if (_showSkillExecution)
                DrawSkillExecution(pos);
        }

        private void OnDrawGizmosSelected()
        {
            if (_agent == null) return;

            Vector3 pos = transform.position + Vector3.up * 0.1f;

            if (_showThreat)
                DrawThreat(pos);

            if (_showFormation)
                DrawFormation(pos);
        }

        private void DrawWeaponReach(Vector3 pos)
        {
            Gizmos.color = _weaponReachColor;
            Gizmos.DrawWireSphere(pos, _agent.WeaponReach);
        }

        private void DrawEngagement(Vector3 pos)
        {
            if (_agent.Engagement == null) return;
            Gizmos.color = _engagementColor;

            // Draw engagement range
            DrawCircle(pos, _agent.Engagement.EngagementRange, 20);

            // Draw lines to engaged enemies
            foreach (var enemy in _agent.Engagement.EngagedEnemies)
            {
                if (enemy == null) continue;
                Gizmos.DrawLine(pos, enemy.transform.position + Vector3.up * 0.1f);
            }
        }

        private void DrawFacingArc(Vector3 pos)
        {
            if (_agent.Facing == null) return;
            Gizmos.color = _facingArcColor;

            float arcAngle = _agent.Facing.FacingArcAngle;
            Vector3 dir = _agent.Facing.Direction;
            float reach = _agent.WeaponReach;

            // Draw facing direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(pos, dir * reach);

            // Draw arc edges
            Gizmos.color = _facingArcColor;
            Quaternion leftRot = Quaternion.Euler(0, -arcAngle * 0.5f, 0);
            Quaternion rightRot = Quaternion.Euler(0, arcAngle * 0.5f, 0);
            Gizmos.DrawRay(pos, leftRot * dir * reach);
            Gizmos.DrawRay(pos, rightRot * dir * reach);

            // Fill arc
            DrawArc(pos, dir, reach, arcAngle, 12);
        }

        private void DrawTargetSelection(Vector3 pos)
        {
            if (_agent.Engagement == null || _agent.Engagement.PrimaryTarget == null) return;
            Gizmos.color = _targetColor;
            Vector3 targetPos = _agent.Engagement.PrimaryTarget.transform.position + Vector3.up * 0.1f;
            Gizmos.DrawLine(pos + Vector3.up, targetPos + Vector3.up);
            Gizmos.DrawWireSphere(targetPos + Vector3.up * 0.5f, 0.3f);
        }

        private void DrawMovementIntent(Vector3 pos)
        {
            var movement = GetComponent<CombatMovementController>();
            if (movement == null || movement.CurrentIntent == CombatMovementIntent.Hold) return;

            Gizmos.color = _movementColor;
            Gizmos.DrawLine(pos, movement.MoveTarget);
            Gizmos.DrawWireSphere(movement.MoveTarget, 0.2f);

            // Label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 2.5f, movement.CurrentIntent.ToString());
            #endif
        }

        private void DrawSkillExecution(Vector3 pos)
        {
            if (_agent.SkillExecutor == null || !_agent.SkillExecutor.IsExecuting) return;

            Gizmos.color = _skillColor;
            var skill = _agent.SkillExecutor.CurrentSkill;
            if (skill == null) return;

            // Draw attack area based on shape
            Vector3 dir = _agent.Facing != null ? _agent.Facing.Direction : transform.forward;
            float range = skill.TargetingProfile.MaxRange;

            switch (skill.TargetingProfile.Shape)
            {
                case AttackShape.Circle:
                    Gizmos.DrawWireSphere(pos, range);
                    break;
                case AttackShape.Cone:
                    DrawArc(pos, dir, range, skill.TargetingProfile.ArcAngle, 16);
                    break;
                case AttackShape.Line:
                    Gizmos.DrawRay(pos, dir * range);
                    break;
            }

            // Phase indicator
            float progress = _agent.SkillExecutor.PhaseProgress;
            Color phaseColor = _agent.SkillExecutor.CurrentPhase switch
            {
                SkillPhase.Windup => Color.yellow,
                SkillPhase.Active => Color.red,
                SkillPhase.Recovery => Color.gray,
                _ => Color.white
            };
            Gizmos.color = phaseColor;
            Gizmos.DrawWireSphere(pos + Vector3.up * 2f, 0.15f + progress * 0.15f);
        }

        private void DrawThreat(Vector3 pos)
        {
            if (_agent.Threat == null) return;
            foreach (var kvp in _agent.Threat.ThreatTable)
            {
                if (kvp.Key == null) continue;
                float intensity = Mathf.Clamp01(kvp.Value / 50f);
                Gizmos.color = Color.Lerp(Color.yellow, Color.red, intensity);
                Gizmos.DrawLine(pos + Vector3.up * 0.5f,
                    kvp.Key.transform.position + Vector3.up * 0.5f);
            }
        }

        private void DrawFormation(Vector3 pos)
        {
            var formation = GetComponentInParent<FormationController>();
            if (formation == null) return;

            Gizmos.color = _formationColor;
            foreach (var slot in formation.Slots)
            {
                if (slot.Agent == null) continue;
                Gizmos.DrawWireSphere(slot.DesiredWorldPosition, 0.3f);
                Gizmos.DrawLine(slot.Agent.transform.position, slot.DesiredWorldPosition);
            }
        }

        // --- Helper Methods ---

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float step = 360f / segments;
            Vector3 prev = center + new Vector3(radius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * step * Mathf.Deg2Rad;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        private void DrawArc(Vector3 center, Vector3 direction, float radius, float angle, int segments)
        {
            float halfAngle = angle * 0.5f;
            float step = angle / segments;
            Vector3 prev = center + Quaternion.Euler(0, -halfAngle, 0) * direction * radius;
            Gizmos.DrawLine(center, prev);

            for (int i = 1; i <= segments; i++)
            {
                float a = -halfAngle + i * step;
                Vector3 next = center + Quaternion.Euler(0, a, 0) * direction * radius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
            Gizmos.DrawLine(prev, center);
        }

        // --- GUI Overlay ---
        private void OnGUI()
        {
            if (!_showUtilityScore || _agent == null || !_agent.IsAlive) return;

            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f)
                : Vector3.zero;

            if (screenPos.z < 0) return;

            float x = screenPos.x - 60;
            float y = Screen.height - screenPos.y;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = Color.white }
            };

            // Agent name + state
            GUI.Label(new Rect(x, y, 120, 16),
                $"{_agent.AgentName} [{_agent.State}]", style);

            // Current intention
            if (_agent.Brain != null)
            {
                GUI.Label(new Rect(x, y + 14, 120, 16),
                    _agent.Brain.CurrentIntention, style);
            }

            // Health/Stamina bars
            Rect hpRect = new Rect(x + 10, y + 28, 100, 6);
            GUI.DrawTexture(hpRect, Texture2D.whiteTexture);
            GUI.color = Color.green;
            GUI.DrawTexture(new Rect(hpRect.x, hpRect.y, hpRect.width * _agent.HealthRatio, hpRect.height), Texture2D.whiteTexture);
            GUI.color = Color.yellow;
            Rect stRect = new Rect(x + 10, y + 36, 100, 4);
            GUI.DrawTexture(new Rect(stRect.x, stRect.y, stRect.width * _agent.StaminaRatio, stRect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}

