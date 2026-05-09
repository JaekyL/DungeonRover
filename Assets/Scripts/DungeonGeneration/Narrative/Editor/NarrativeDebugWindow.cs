#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Narrative.Interpretation;
using DungeonGeneration.Narrative.Validation;
using DungeonGeneration.Narrative.WorldState;
using UnityEditor;
using UnityEngine;

namespace DungeonGeneration.Narrative.Editor
{
    /// <summary>
    /// Editor window for inspecting and debugging the narrative simulation system.
    /// Provides tabs for faction overview, territory map, timeline, room histories,
    /// readability scores, and interpretation results.
    /// </summary>
    public class NarrativeDebugWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private int _selectedTab;
        private int _selectedRoom = -1;
        private int _timelineStep = 0;
        private bool _showOnlyIssues = false;
        private string _factionFilter = "";

        private readonly string[] _tabs =
        {
            "Overview", "Factions", "Territory", "Timeline",
            "Room History", "Readability", "Interpretation"
        };

        [MenuItem("Window/Dungeon Generation/Narrative Debug")]
        public static void ShowWindow()
        {
            GetWindow<NarrativeDebugWindow>("Narrative Debug");
        }

        private GenerationContext GetContext()
        {
            var gen = FindObjectOfType<DungeonGenerator>();
            return gen?.LastContext;
        }

        private NarrativeWorldState GetWorldState()
        {
            return GetContext()?.GetCustomData<NarrativeWorldState>("narrative_world_state");
        }

        private void OnGUI()
        {
            var ctx = GetContext();
            var worldState = GetWorldState();

            if (ctx == null)
            {
                EditorGUILayout.HelpBox("No dungeon generated. Use the DungeonGenerator component.", MessageType.Info);
                return;
            }

            if (worldState == null)
            {
                EditorGUILayout.HelpBox("No narrative data. Attach a NarrativeConfig to the generator and regenerate.", MessageType.Warning);
                return;
            }

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            switch (_selectedTab)
            {
                case 0: DrawOverview(ctx, worldState); break;
                case 1: DrawFactions(worldState); break;
                case 2: DrawTerritory(worldState, ctx); break;
                case 3: DrawTimeline(worldState); break;
                case 4: DrawRoomHistory(worldState, ctx); break;
                case 5: DrawReadability(ctx); break;
                case 6: DrawInterpretation(ctx); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawOverview(GenerationContext ctx, NarrativeWorldState worldState)
        {
            EditorGUILayout.LabelField("Narrative Simulation Overview", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField($"Simulation Steps: {worldState.CurrentStep}");
            EditorGUILayout.LabelField($"Timeline Events: {worldState.Timeline.Entries.Count}");
            EditorGUILayout.LabelField($"Total Factions: {worldState.FactionStates.Count}");

            int active = worldState.FactionStates.Values.Count(f => f.IsActive);
            int eliminated = worldState.FactionStates.Values.Count(f => f.IsEliminated);
            EditorGUILayout.LabelField($"  Active: {active}  |  Eliminated: {eliminated}");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Room States", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Tracked Rooms: {worldState.RoomStates.Count}");
            EditorGUILayout.LabelField($"Abandoned: {worldState.GetAbandonedRooms().Count}");
            EditorGUILayout.LabelField($"Contested: {worldState.GetContestedRooms().Count}");
            EditorGUILayout.LabelField($"Corrupted: {worldState.GetCorruptedRooms().Count}");
            EditorGUILayout.LabelField($"Damaged: {worldState.GetDamagedRooms().Count}");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            foreach (var kvp in worldState.Resources)
            {
                var barColor = kvp.Value < 20f ? Color.red : kvp.Value < 50f ? Color.yellow : Color.green;
                EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value:F1}");
                var rect = GUILayoutUtility.GetRect(200, 8);
                EditorGUI.DrawRect(rect, Color.gray);
                rect.width *= Mathf.Clamp01(kvp.Value / 100f);
                EditorGUI.DrawRect(rect, barColor);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Storytelling", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Story Markers: {ctx.StoryMarkers.Count}");
            EditorGUILayout.LabelField($"Decorations: {ctx.Decorations.Count}");

            var scores = ctx.GetCustomData<Dictionary<int, float>>("readability_scores");
            if (scores != null && scores.Count > 0)
            {
                float avg = scores.Values.Average();
                EditorGUILayout.LabelField($"Average Readability: {avg:F2}");
            }
        }

        private void DrawFactions(NarrativeWorldState worldState)
        {
            EditorGUILayout.LabelField("Faction States", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            foreach (var kvp in worldState.FactionStates)
            {
                var state = kvp.Value;
                EditorGUILayout.BeginVertical("box");

                string statusIcon = state.IsEliminated ? "☠" : state.IsActive ? "✓" : "?";
                EditorGUILayout.LabelField($"{statusIcon} {state.FactionId}", EditorStyles.boldLabel);

                EditorGUILayout.LabelField($"  Morale: {state.Morale:F2}");
                var moraleRect = GUILayoutUtility.GetRect(200, 6);
                EditorGUI.DrawRect(moraleRect, Color.gray);
                moraleRect.width *= Mathf.Clamp01(state.Morale);
                EditorGUI.DrawRect(moraleRect, Color.green);

                EditorGUILayout.LabelField($"  Desperation: {state.Desperation:F2}");
                var despRect = GUILayoutUtility.GetRect(200, 6);
                EditorGUI.DrawRect(despRect, Color.gray);
                despRect.width *= Mathf.Clamp01(state.Desperation);
                EditorGUI.DrawRect(despRect, new Color(1f, 0.3f, 0f));

                EditorGUILayout.LabelField($"  Strength: {state.Strength:F2}");
                EditorGUILayout.LabelField($"  Territory: {state.TerritorySize} rooms [{string.Join(", ", state.ControlledRoomIds)}]");
                EditorGUILayout.LabelField($"  Members: {state.MemberCount}");

                if (state.IsEliminated)
                    EditorGUILayout.LabelField($"  Eliminated at step: {state.StepEliminated}");

                if (state.ActiveGoals.Count > 0)
                    EditorGUILayout.LabelField($"  Goals: {string.Join(", ", state.ActiveGoals)}");

                if (state.Dispositions.Count > 0)
                {
                    EditorGUILayout.LabelField("  Dispositions:", EditorStyles.miniLabel);
                    foreach (var disp in state.Dispositions)
                    {
                        string emoji = disp.Value > 0.3f ? "💚" : disp.Value < -0.3f ? "💀" : "😐";
                        EditorGUILayout.LabelField($"    {emoji} → {disp.Key}: {disp.Value:F2}");
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
        }

        private void DrawTerritory(NarrativeWorldState worldState, GenerationContext ctx)
        {
            EditorGUILayout.LabelField("Territory Overview", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var counts = worldState.Territories.GetTerritoryCount();
            foreach (var kvp in counts)
            {
                EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value} rooms");
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Room Ownership", EditorStyles.boldLabel);

            if (ctx.SpatialMap != null)
            {
                for (int i = 0; i < ctx.SpatialMap.Rooms.Count; i++)
                {
                    var roomState = worldState.GetRoomState(i);
                    string owner = roomState.CurrentOwner ?? "(unclaimed)";
                    string flags = "";
                    if (roomState.IsBarricaded) flags += " [BAR]";
                    if (roomState.IsCollapsed) flags += " [COL]";
                    if (roomState.IsWarzone) flags += " [WAR]";
                    if (roomState.IsAbandoned) flags += " [ABN]";
                    if (roomState.IsRitualSite) flags += " [RIT]";
                    if (roomState.IsFlooded) flags += " [FLD]";

                    EditorGUILayout.LabelField($"  Room {i}: {owner}{flags} | Label: {roomState.RoomSemanticLabel}");
                }
            }
        }

        private void DrawTimeline(NarrativeWorldState worldState)
        {
            EditorGUILayout.LabelField("World Timeline", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            int maxStep = worldState.Timeline.Entries.Count > 0
                ? worldState.Timeline.Entries.Max(e => e.Step)
                : 0;

            _timelineStep = EditorGUILayout.IntSlider("Step Filter", _timelineStep, 0, maxStep);
            _factionFilter = EditorGUILayout.TextField("Faction Filter", _factionFilter);

            EditorGUILayout.Space(5);

            var entries = worldState.Timeline.Entries.AsEnumerable();

            if (_timelineStep > 0)
                entries = entries.Where(e => e.Step == _timelineStep);

            if (!string.IsNullOrEmpty(_factionFilter))
                entries = entries.Where(e => e.Actor.Contains(_factionFilter));

            var sorted = entries.OrderBy(e => e.Step).ThenBy(e => e.Actor).ToList();

            EditorGUILayout.LabelField($"Showing {sorted.Count} / {worldState.Timeline.Entries.Count} events");
            EditorGUILayout.Space(3);

            int currentStep = -1;
            foreach (var entry in sorted.Take(200))
            {
                if (entry.Step != currentStep)
                {
                    currentStep = entry.Step;
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField($"— Step {currentStep} —", EditorStyles.boldLabel);
                }

                Color impactColor = entry.Impact > 0.7f ? Color.red :
                    entry.Impact > 0.4f ? Color.yellow : Color.white;
                var style = new GUIStyle(EditorStyles.label) { normal = { textColor = impactColor } };

                string roomInfo = entry.AffectedRoomId >= 0 ? $" [Room {entry.AffectedRoomId}]" : "";
                EditorGUILayout.LabelField(
                    $"  [{entry.Actor}]{roomInfo} {entry.EventType}: {entry.Description}", style);
            }

            if (sorted.Count > 200)
                EditorGUILayout.LabelField($"... and {sorted.Count - 200} more events");
        }

        private void DrawRoomHistory(NarrativeWorldState worldState, GenerationContext ctx)
        {
            EditorGUILayout.LabelField("Room History Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            int roomCount = ctx.SpatialMap?.Rooms.Count ?? 0;
            if (roomCount == 0) return;

            _selectedRoom = EditorGUILayout.IntSlider("Room", _selectedRoom, 0, roomCount - 1);

            if (_selectedRoom < 0) return;

            var roomState = worldState.GetRoomState(_selectedRoom);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Room {_selectedRoom}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Semantic Label: {roomState.RoomSemanticLabel}");
            EditorGUILayout.LabelField($"Current Owner: {roomState.CurrentOwner ?? "none"}");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("State Values", EditorStyles.boldLabel);
            DrawBar("Danger", roomState.DangerLevel, Color.red);
            DrawBar("Corruption", roomState.Corruption, new Color(0.5f, 0f, 0.8f));
            DrawBar("Structural Damage", roomState.StructuralDamage, Color.yellow);
            DrawBar("Decay", roomState.Decay, new Color(0.6f, 0.5f, 0.3f));
            DrawBar("Water Level", roomState.WaterLevel, Color.blue);
            DrawBar("Conflict Intensity", roomState.ConflictIntensity, new Color(1f, 0.3f, 0f));
            DrawBar("Traversal Safety", roomState.TraversalSafety, Color.green);

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Flags", EditorStyles.boldLabel);
            string flags = "";
            if (roomState.IsBarricaded) flags += "Barricaded ";
            if (roomState.IsCollapsed) flags += "Collapsed ";
            if (roomState.IsSealed) flags += "Sealed ";
            if (roomState.IsFlooded) flags += "Flooded ";
            if (roomState.IsRitualSite) flags += "RitualSite ";
            if (roomState.IsAbandoned) flags += "Abandoned ";
            if (roomState.IsWarzone) flags += "Warzone ";
            if (roomState.IsSafeZone) flags += "SafeZone ";
            EditorGUILayout.LabelField($"  {(string.IsNullOrEmpty(flags) ? "none" : flags)}");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Semantic Tags", EditorStyles.boldLabel);
            foreach (var tag in roomState.SemanticTags)
                EditorGUILayout.LabelField($"  • {tag}");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField($"Ownership History ({roomState.OwnershipHistory.Count})", EditorStyles.boldLabel);
            foreach (var record in roomState.OwnershipHistory)
            {
                EditorGUILayout.LabelField(
                    $"  Step {record.FromStep}: {record.PreviousOwner ?? "unclaimed"} → {record.FactionId}");
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField($"Event History ({roomState.History.Count})", EditorStyles.boldLabel);
            foreach (var entry in roomState.History)
            {
                EditorGUILayout.LabelField($"  Step {entry.Step} [{entry.Actor}]: {entry.Description}");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawReadability(GenerationContext ctx)
        {
            EditorGUILayout.LabelField("Readability Scores & Validation", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var scores = ctx.GetCustomData<Dictionary<int, float>>("readability_scores");
            var issues = ctx.GetCustomData<List<ReadabilityIssue>>("narrative_issues");

            if (issues != null)
            {
                _showOnlyIssues = EditorGUILayout.Toggle("Show Only Issues", _showOnlyIssues);
                EditorGUILayout.Space(3);

                int critCount = issues.Count(i => i.Severity == ReadabilitySeverity.Critical);
                int errCount = issues.Count(i => i.Severity == ReadabilitySeverity.Error);
                int warnCount = issues.Count(i => i.Severity == ReadabilitySeverity.Warning);
                int fixedCount = issues.Count(i => i.WasAutoFixed);

                EditorGUILayout.LabelField($"Critical: {critCount} | Errors: {errCount} | Warnings: {warnCount} | Auto-fixed: {fixedCount}");
                EditorGUILayout.Space(5);

                foreach (var issue in issues.OrderByDescending(i => i.Severity))
                {
                    Color col = issue.Severity == ReadabilitySeverity.Critical ? Color.red :
                        issue.Severity == ReadabilitySeverity.Error ? new Color(1f, 0.5f, 0f) :
                        issue.Severity == ReadabilitySeverity.Warning ? Color.yellow : Color.white;

                    var style = new GUIStyle(EditorStyles.helpBox);
                    EditorGUILayout.BeginVertical(style);

                    var labelStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = col }, fontStyle = FontStyle.Bold };
                    EditorGUILayout.LabelField($"[{issue.Severity}] {issue.RuleName} — Room {issue.RoomId}" +
                                              (issue.WasAutoFixed ? " ✓ FIXED" : ""), labelStyle);
                    EditorGUILayout.LabelField(issue.Description, EditorStyles.wordWrappedLabel);
                    if (!string.IsNullOrEmpty(issue.SuggestedFix))
                        EditorGUILayout.LabelField($"Fix: {issue.SuggestedFix}", EditorStyles.miniLabel);

                    EditorGUILayout.EndVertical();
                }
            }

            if (scores != null && !_showOnlyIssues)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Per-Room Readability", EditorStyles.boldLabel);

                foreach (var kvp in scores.OrderBy(s => s.Value))
                {
                    Color barColor = kvp.Value > 0.7f ? Color.green :
                        kvp.Value > 0.4f ? Color.yellow : Color.red;

                    EditorGUILayout.LabelField($"  Room {kvp.Key}: {kvp.Value:F2}");
                    var rect = GUILayoutUtility.GetRect(200, 6);
                    EditorGUI.DrawRect(rect, Color.gray);
                    rect.width *= kvp.Value;
                    EditorGUI.DrawRect(rect, barColor);
                }
            }
        }

        private void DrawInterpretation(GenerationContext ctx)
        {
            EditorGUILayout.LabelField("Spatial Interpretation Results", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var results = ctx.GetCustomData<Dictionary<int, InterpretationResult>>("interpretation_results");
            if (results == null)
            {
                EditorGUILayout.LabelField("No interpretation data available.");
                return;
            }

            foreach (var kvp in results.OrderBy(r => r.Key))
            {
                var r = kvp.Value;
                if (r.Markers.Count == 0 && r.Modifications.Count == 0 && r.Atmospheres.Count == 0)
                    continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Room {kvp.Key}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  Markers: {r.Markers.Count} | Modifications: {r.Modifications.Count} | Atmospheres: {r.Atmospheres.Count}");

                foreach (var atmo in r.Atmospheres)
                {
                    EditorGUILayout.LabelField($"  🌫 {atmo.AtmosphereType} ({atmo.Intensity:F2}): {atmo.Description}");
                }

                foreach (var mod in r.Modifications.Take(5))
                {
                    EditorGUILayout.LabelField($"  🔧 {mod.Type}: {mod.Description}");
                }

                if (r.Modifications.Count > 5)
                    EditorGUILayout.LabelField($"    ... and {r.Modifications.Count - 5} more");

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawBar(string label, float value, Color color)
        {
            EditorGUILayout.LabelField($"  {label}: {value:F2}");
            var rect = GUILayoutUtility.GetRect(200, 6);
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
            rect.width *= Mathf.Clamp01(value);
            EditorGUI.DrawRect(rect, color);
        }
    }
}
#endif

