using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using DungeonGeneration.Narrative.Interpretation;
using DungeonGeneration.Narrative.WorldState;
using UnityEngine;

namespace DungeonGeneration.Narrative.Validation
{
    /// <summary>
    /// Interface for readability rules that validate environmental storytelling coherence.
    /// </summary>
    public interface IReadabilityRule
    {
        string RuleName { get; }
        ReadabilityIssue Validate(int roomId, List<StoryMarker> markers,
            List<DecorationInstance> decorations, RoomNarrativeState roomState,
            RoomInstance room, GenerationContext context);
    }

    /// <summary>
    /// Represents a detected readability or coherence issue.
    /// </summary>
    public class ReadabilityIssue
    {
        public string RuleName;
        public ReadabilitySeverity Severity;
        public int RoomId;
        public string Description;
        public string SuggestedFix;
        public bool WasAutoFixed;
    }

    public enum ReadabilitySeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Layer 8: Validation & Readability Stage.
    /// Prevents incoherent storytelling, detects contradictions, validates readability,
    /// and ensures critical paths remain accessible.
    /// Priority 950: runs near end of pipeline before final optimization.
    /// </summary>
    public class NarrativeValidationStage : IGenerationStage
    {
        public string StageName => "Narrative Validation & Readability";
        public int Priority => 855;

        private readonly List<IReadabilityRule> _rules;

        public NarrativeValidationStage()
        {
            _rules = new List<IReadabilityRule>
            {
                new MarkerDensityRule(),
                new ContradictoryStatesRule(),
                new CriticalPathBlockedRule(),
                new EmptyStorytellingRule(),
                new OvercrowdedRoomRule(),
                new OrphanedEvidenceRule()
            };
        }

        public void AddRule(IReadabilityRule rule) => _rules.Add(rule);

        public void Execute(GenerationContext context)
        {
            var worldState = context.GetCustomData<NarrativeWorldState>("narrative_world_state");
            if (worldState == null) return;

            var allIssues = new List<ReadabilityIssue>();
            var roomScores = new Dictionary<int, float>();

            if (context.SpatialMap == null) return;

            foreach (var room in context.SpatialMap.Rooms)
            {
                var roomState = worldState.GetRoomState(room.Id);
                var roomMarkers = context.StoryMarkers.Where(m => m.RoomId == room.Id).ToList();
                var roomDecorations = context.Decorations.Where(d => d.RoomId == room.Id).ToList();

                // Run all rules
                foreach (var rule in _rules)
                {
                    var issue = rule.Validate(room.Id, roomMarkers, roomDecorations, roomState, room, context);
                    if (issue != null)
                        allIssues.Add(issue);
                }

                // Calculate readability score
                float score = CalculateReadabilityScore(room, roomMarkers, roomDecorations, roomState);
                roomScores[room.Id] = score;
            }

            // Auto-fix critical issues
            int autoFixed = 0;
            foreach (var issue in allIssues.Where(i => i.Severity >= ReadabilitySeverity.Error))
            {
                if (TryAutoFix(issue, context, worldState))
                {
                    issue.WasAutoFixed = true;
                    autoFixed++;
                }
            }

            // Store results
            context.SetCustomData("narrative_issues", allIssues);
            context.SetCustomData("readability_scores", roomScores);

            int errors = allIssues.Count(i => i.Severity >= ReadabilitySeverity.Error);
            int warnings = allIssues.Count(i => i.Severity == ReadabilitySeverity.Warning);
            float avgScore = roomScores.Values.Count > 0 ? roomScores.Values.Average() : 0f;

            UnityEngine.Debug.Log($"[Narrative] Validation: {errors} errors, {warnings} warnings, " +
                                  $"{autoFixed} auto-fixed. Average readability: {avgScore:F2}");
        }

        private float CalculateReadabilityScore(RoomInstance room, List<StoryMarker> markers,
            List<DecorationInstance> decorations, RoomNarrativeState roomState)
        {
            float score = 1f;

            // Too many markers = confusing
            int totalElements = markers.Count + decorations.Count;
            if (totalElements > room.FloorTiles.Count * 0.5f)
                score -= 0.3f;

            // Too few markers in rooms with history = missed opportunity
            if (roomState.History.Count > 3 && markers.Count < 2)
                score -= 0.2f;

            // Contradictory states reduce readability
            bool hasConflict = roomState.IsWarzone;
            bool hasPeace = roomState.IsSafeZone;
            if (hasConflict && hasPeace) score -= 0.4f;

            // Multiple distinct storytelling layers are good (up to a point)
            var uniqueTypes = markers.Select(m => m.Type).Distinct().Count();
            if (uniqueTypes >= 2 && uniqueTypes <= 4) score += 0.1f;
            if (uniqueTypes > 6) score -= 0.2f;

            return Mathf.Clamp01(score);
        }

        private bool TryAutoFix(ReadabilityIssue issue, GenerationContext context, NarrativeWorldState worldState)
        {
            switch (issue.RuleName)
            {
                case "MarkerDensity":
                    // Remove excess markers from overcrowded rooms
                    var excess = context.StoryMarkers
                        .Where(m => m.RoomId == issue.RoomId)
                        .OrderBy(m => m.Intensity)
                        .ToList();
                    int toRemove = excess.Count / 3;
                    for (int i = 0; i < toRemove && i < excess.Count; i++)
                        context.StoryMarkers.Remove(excess[i]);
                    return toRemove > 0;

                case "CriticalPathBlocked":
                    // Unblock critical path tiles
                    if (context.SpatialMap != null && issue.RoomId < context.SpatialMap.Rooms.Count)
                    {
                        var room = context.SpatialMap.Rooms[issue.RoomId];
                        foreach (var entry in room.EntryPoints)
                        {
                            var tile = context.SpatialMap.GetTile(entry);
                            if (tile != null && tile.Type == TileType.Rubble)
                            {
                                tile.Type = TileType.Floor;
                                tile.Tags.Remove("rubble");
                                return true;
                            }
                        }
                    }
                    break;

                case "ContradictoryStates":
                    // Resolve contradiction by favoring the more dramatic state
                    var rs = worldState.GetRoomState(issue.RoomId);
                    if (rs.IsWarzone && rs.IsSafeZone)
                        rs.IsSafeZone = false;
                    return true;
            }

            return false;
        }
    }

    // --- Rule Implementations ---

    public class MarkerDensityRule : IReadabilityRule
    {
        public string RuleName => "MarkerDensity";

        public ReadabilityIssue Validate(int roomId, List<StoryMarker> markers,
            List<DecorationInstance> decorations, RoomNarrativeState roomState,
            RoomInstance room, GenerationContext context)
        {
            int total = markers.Count + decorations.Count;
            float density = room.FloorTiles.Count > 0 ? (float)total / room.FloorTiles.Count : 0;

            if (density > 0.6f)
            {
                return new ReadabilityIssue
                {
                    RuleName = RuleName,
                    Severity = ReadabilitySeverity.Error,
                    RoomId = roomId,
                    Description = $"Room {roomId} has excessive marker density ({density:F2}). " +
                                  $"{total} elements in {room.FloorTiles.Count} tiles.",
                    SuggestedFix = "Remove low-importance markers to reduce clutter"
                };
            }
            return null;
        }
    }

    public class ContradictoryStatesRule : IReadabilityRule
    {
        public string RuleName => "ContradictoryStates";

        public ReadabilityIssue Validate(int roomId, List<StoryMarker> markers,
            List<DecorationInstance> decorations, RoomNarrativeState roomState,
            RoomInstance room, GenerationContext context)
        {
            if (roomState.IsWarzone && roomState.IsSafeZone)
            {
                return new ReadabilityIssue
                {
                    RuleName = RuleName,
                    Severity = ReadabilitySeverity.Error,
                    RoomId = roomId,
                    Description = $"Room {roomId} is marked as both warzone and safe zone.",
                    SuggestedFix = "Remove safe zone flag from rooms with active conflict"
                };
            }

            if (roomState.IsCollapsed && !string.IsNullOrEmpty(roomState.CurrentOwner))
            {
                return new ReadabilityIssue
                {
                    RuleName = RuleName,
                    Severity = ReadabilitySeverity.Warning,
                    RoomId = roomId,
                    Description = $"Room {roomId} is collapsed but still owned by {roomState.CurrentOwner}.",
                    SuggestedFix = "Clear ownership from collapsed rooms"
                };
            }

            return null;
        }
    }

    public class CriticalPathBlockedRule : IReadabilityRule
    {
        public string RuleName => "CriticalPathBlocked";

        public ReadabilityIssue Validate(int roomId, List<StoryMarker> markers,
            List<DecorationInstance> decorations, RoomNarrativeState roomState,
            RoomInstance room, GenerationContext context)
        {
            if (context.Graph == null) return null;

            var node = context.Graph.GetNode(room.GraphNodeId);
            if (node == null || !node.IsCriticalPath) return null;

            // Check if all entries are blocked
            bool allBlocked = room.EntryPoints.Count > 0 && room.EntryPoints.All(entry =>
            {
                var tile = context.SpatialMap?.GetTile(entry);
                return tile != null && (tile.Type == TileType.Rubble || tile.Type == TileType.Pit);
            });

            if (allBlocked)
            {
                return new ReadabilityIssue
                {
                    RuleName = RuleName,
                    Severity = ReadabilitySeverity.Critical,
                    RoomId = roomId,
                    Description = $"Room {roomId} is on critical path but all entries are blocked!",
                    SuggestedFix = "Clear at least one entry point of rubble/pit"
                };
            }

            return null;
        }
    }

    public class EmptyStorytellingRule : IReadabilityRule
    {
        public string RuleName => "EmptyStorytelling";

        public ReadabilityIssue Validate(int roomId, List<StoryMarker> markers,
            List<DecorationInstance> decorations, RoomNarrativeState roomState,
            RoomInstance room, GenerationContext context)
        {
            // Rooms with significant history but no storytelling markers
            if (roomState.History.Count > 5 && markers.Count == 0)
            {
                return new ReadabilityIssue
                {
                    RuleName = RuleName,
                    Severity = ReadabilitySeverity.Warning,
                    RoomId = roomId,
                    Description = $"Room {roomId} has {roomState.History.Count} historical events but no story markers.",
                    SuggestedFix = "Add basic storytelling markers for rooms with significant history"
                };
            }
            return null;
        }
    }

    public class OvercrowdedRoomRule : IReadabilityRule
    {
        public string RuleName => "OvercrowdedRoom";

        public ReadabilityIssue Validate(int roomId, List<StoryMarker> markers,
            List<DecorationInstance> decorations, RoomNarrativeState roomState,
            RoomInstance room, GenerationContext context)
        {
            int skeletons = markers.Count(m => m.Type == StoryMarkerType.Skeleton);
            if (skeletons > 6)
            {
                return new ReadabilityIssue
                {
                    RuleName = RuleName,
                    Severity = ReadabilitySeverity.Warning,
                    RoomId = roomId,
                    Description = $"Room {roomId} has {skeletons} skeleton markers. Too many reduces impact.",
                    SuggestedFix = "Cap skeleton count at 4-5 per room for dramatic impact"
                };
            }
            return null;
        }
    }

    public class OrphanedEvidenceRule : IReadabilityRule
    {
        public string RuleName => "OrphanedEvidence";

        public ReadabilityIssue Validate(int roomId, List<StoryMarker> markers,
            List<DecorationInstance> decorations, RoomNarrativeState roomState,
            RoomInstance room, GenerationContext context)
        {
            // Ritual markings in non-ritual rooms
            bool hasRitualMarkers = markers.Any(m => m.Type == StoryMarkerType.RitualMarking);
            if (hasRitualMarkers && !roomState.IsRitualSite && !roomState.SemanticTags.Contains("ritual_site"))
            {
                return new ReadabilityIssue
                {
                    RuleName = RuleName,
                    Severity = ReadabilitySeverity.Warning,
                    RoomId = roomId,
                    Description = $"Room {roomId} has ritual markers but is not a ritual site.",
                    SuggestedFix = "Either mark room as ritual site or remove orphaned ritual markers"
                };
            }
            return null;
        }
    }
}

