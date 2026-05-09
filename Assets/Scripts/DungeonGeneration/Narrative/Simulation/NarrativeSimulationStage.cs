using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Narrative.Authoring;
using DungeonGeneration.Narrative.WorldState;

namespace DungeonGeneration.Narrative.Simulation
{
    /// <summary>
    /// Layer 3: Constrained historical simulation stage.
    /// Runs authored faction agents through time steps, processes narrative events,
    /// enforces constraints and protected outcomes, and produces cascading consequences.
    /// Integrates with the existing dungeon pipeline at priority 305 (runs after or alongside
    /// the existing HistorySimulationStage at 300).
    /// </summary>
    public class NarrativeSimulationStage : IGenerationStage
    {
        public string StageName => "Narrative Historical Simulation";
        public int Priority => 305;

        public void Execute(GenerationContext context)
        {
            var config = context.GetCustomData<NarrativeConfig>("narrative_config");
            if (config == null) return;

            var worldState = context.GetCustomData<NarrativeWorldState>("narrative_world_state");
            if (worldState == null)
            {
                UnityEngine.Debug.LogWarning("[Narrative] No world state found. Ensure SemanticExtractionStage ran first.");
                return;
            }

            var rng = context.Random.Fork("narrative_simulation");

            // Create agents for each faction
            var agents = CreateAgents(config, worldState, context, rng);

            // Schedule mandatory events
            var eventSchedule = ScheduleEvents(config, rng);

            // Run simulation
            int totalSteps = config.simulationSteps;
            for (int step = 0; step < totalSteps; step++)
            {
                worldState.CurrentStep = step;

                // Process scheduled/mandatory events for this step
                ProcessScheduledEvents(step, eventSchedule, worldState, context, rng, agents);

                // Run all agents
                foreach (var agent in agents.OrderBy(a => a.Priority))
                {
                    agent.SimulateStep(step, worldState, context, rng.Fork($"{agent.AgentId}_{step}"));
                }

                // Apply environmental decay
                ApplyGlobalDecay(step, config, worldState);

                // Enforce constraints
                EnforceConstraints(step, config, worldState, context);

                // Check for cascading events
                ProcessCascadingEvents(step, config, worldState, context, rng);

                // Anti-chaos check
                if (worldState.FactionStates.Values.Count(f => f.IsActive) == 0)
                {
                    UnityEngine.Debug.Log($"[Narrative] All factions eliminated at step {step}. Ending simulation.");
                    break;
                }
            }

            // Enforce protected outcomes
            EnforceProtectedOutcomes(config, worldState, context);

            // Mark abandoned rooms with decay
            FinalizeWorldState(worldState, context);

            // Bridge to existing HistoryLog for compatibility
            BridgeToHistoryLog(worldState, context);

            UnityEngine.Debug.Log($"[Narrative] Simulation complete: {worldState.Timeline.Entries.Count} events, " +
                                  $"{worldState.FactionStates.Values.Count(f => f.IsActive)} surviving factions");
        }

        private List<INarrativeAgent> CreateAgents(NarrativeConfig config, NarrativeWorldState worldState,
            GenerationContext context, SeededRandom rng)
        {
            var agents = new List<INarrativeAgent>();
            int priority = 0;
            foreach (var faction in config.factions)
            {
                var agent = new FactionAgent(faction, priority++);
                agent.Initialize(worldState, context, rng.Fork(faction.factionId));
                agents.Add(agent);
            }
            return agents;
        }

        private Dictionary<int, List<NarrativeEventDefinition>> ScheduleEvents(NarrativeConfig config, SeededRandom rng)
        {
            var schedule = new Dictionary<int, List<NarrativeEventDefinition>>();

            foreach (var evt in config.timelineEvents)
            {
                int targetStep;
                switch (evt.timing)
                {
                    case EventTiming.FixedStep:
                        targetStep = evt.fixedStep;
                        break;
                    case EventTiming.Range:
                        targetStep = rng.Next(evt.earliestStep, evt.latestStep + 1);
                        break;
                    case EventTiming.AsSoonAsPossible:
                        targetStep = 1;
                        break;
                    default:
                        continue; // Triggered/Conditional events are processed separately
                }

                if (!schedule.ContainsKey(targetStep))
                    schedule[targetStep] = new List<NarrativeEventDefinition>();
                schedule[targetStep].Add(evt);
            }

            return schedule;
        }

        private void ProcessScheduledEvents(int step, Dictionary<int, List<NarrativeEventDefinition>> schedule,
            NarrativeWorldState worldState, GenerationContext context, SeededRandom rng,
            List<INarrativeAgent> agents)
        {
            if (!schedule.ContainsKey(step)) return;

            foreach (var evt in schedule[step])
            {
                if (!CheckPreconditions(evt, worldState)) continue;

                ExecuteNarrativeEvent(step, evt, worldState, context, rng);

                // Notify agents
                foreach (var effect in evt.effects)
                {
                    var relevantAgents = agents.Where(a => evt.involvedFactions
                        .Any(f => f.factionId == a.FactionId));
                    foreach (var agent in relevantAgents)
                    {
                        agent.OnEventTriggered(effect.type.ToString(), -1, worldState);
                    }
                }
            }
        }

        private bool CheckPreconditions(NarrativeEventDefinition evt, NarrativeWorldState worldState)
        {
            foreach (var precondition in evt.preconditions)
            {
                switch (precondition.type)
                {
                    case PreconditionType.FactionExists:
                        var faction = worldState.GetFactionState(precondition.targetId);
                        if (faction.IsEliminated) return false;
                        break;
                    case PreconditionType.ResourceBelow:
                        if (worldState.GetResource(precondition.targetId) >= precondition.threshold * 100f)
                            return false;
                        break;
                    case PreconditionType.EventOccurred:
                        if (!worldState.Timeline.Entries.Any(e => e.EventType == precondition.targetId))
                            return false;
                        break;
                    case PreconditionType.StepReached:
                        if (worldState.CurrentStep < int.Parse(precondition.expectedValue ?? "0"))
                            return false;
                        break;
                    case PreconditionType.CharacterAlive:
                        if (worldState.CharacterStates.ContainsKey(precondition.targetId) &&
                            !worldState.CharacterStates[precondition.targetId].IsAlive)
                            return false;
                        break;
                }
            }
            return true;
        }

        private void ExecuteNarrativeEvent(int step, NarrativeEventDefinition evt,
            NarrativeWorldState worldState, GenerationContext context, SeededRandom rng)
        {
            worldState.Timeline.Record(step, evt.eventId, evt.description,
                evt.involvedFactions.Count > 0 ? evt.involvedFactions[0].factionId : "world",
                -1, (float)evt.severity / 4f);

            foreach (var effect in evt.effects)
            {
                ApplyEventEffect(step, effect, worldState, context, rng);
            }

            // Handle cascading events
            foreach (var triggered in evt.triggeredEvents)
            {
                if (triggered != null && rng.NextBool(evt.cascadeProbability))
                {
                    ExecuteNarrativeEvent(step, triggered, worldState, context, rng);
                }
            }

            UnityEngine.Debug.Log($"[Narrative] Event '{evt.displayName}' executed at step {step}");
        }

        private void ApplyEventEffect(int step, EventEffect effect, NarrativeWorldState worldState,
            GenerationContext context, SeededRandom rng)
        {
            int targetRoomId = -1;
            if (int.TryParse(effect.targetId, out int parsedRoom))
                targetRoomId = parsedRoom;

            switch (effect.type)
            {
                case EventEffectType.DamageRoom:
                    if (targetRoomId >= 0)
                    {
                        var rs = worldState.GetRoomState(targetRoomId);
                        rs.StructuralDamage += System.Math.Abs(effect.magnitude);
                        rs.AddHistoryEntry(step, $"Room damaged by event", effect.targetId, effect.magnitude);
                    }
                    break;

                case EventEffectType.CorruptRoom:
                    if (targetRoomId >= 0)
                    {
                        var rs = worldState.GetRoomState(targetRoomId);
                        rs.Corruption += System.Math.Abs(effect.magnitude);
                        rs.SemanticTags.Add("corrupted");
                    }
                    break;

                case EventEffectType.FloodRoom:
                    if (targetRoomId >= 0)
                    {
                        var rs = worldState.GetRoomState(targetRoomId);
                        rs.IsFlooded = true;
                        rs.WaterLevel += System.Math.Abs(effect.magnitude);
                        rs.SemanticTags.Add("flooded");
                    }
                    break;

                case EventEffectType.CollapseArea:
                    if (targetRoomId >= 0)
                    {
                        var rs = worldState.GetRoomState(targetRoomId);
                        rs.IsCollapsed = true;
                        rs.StructuralDamage = 1f;
                        rs.TraversalSafety = 0f;
                        rs.SemanticTags.Add("collapsed");
                    }
                    break;

                case EventEffectType.DisplaceFaction:
                    if (!string.IsNullOrEmpty(effect.targetId))
                    {
                        var fs = worldState.GetFactionState(effect.targetId);
                        fs.Morale -= 0.3f;
                        fs.Desperation += 0.3f;
                    }
                    break;

                case EventEffectType.KillCharacter:
                    if (worldState.CharacterStates.ContainsKey(effect.targetId))
                    {
                        var cs = worldState.CharacterStates[effect.targetId];
                        cs.IsAlive = false;
                        cs.StepDied = step;
                        cs.CauseOfDeath = effect.value;
                    }
                    break;

                case EventEffectType.ChangeMorale:
                    if (!string.IsNullOrEmpty(effect.targetId))
                    {
                        var fs = worldState.GetFactionState(effect.targetId);
                        fs.Morale = System.Math.Max(0f, System.Math.Min(1f, fs.Morale + effect.magnitude));
                    }
                    break;

                case EventEffectType.CreateBarricade:
                    if (targetRoomId >= 0)
                    {
                        var rs = worldState.GetRoomState(targetRoomId);
                        rs.IsBarricaded = true;
                        rs.SemanticTags.Add("barricaded");
                    }
                    break;

                case EventEffectType.CreateRitualSite:
                    if (targetRoomId >= 0)
                    {
                        var rs = worldState.GetRoomState(targetRoomId);
                        rs.IsRitualSite = true;
                        rs.SemanticTags.Add("ritual_site");
                    }
                    break;

                case EventEffectType.SpreadCorruption:
                    SpreadToNeighbors(step, targetRoomId, "corruption", effect.magnitude, worldState, context, rng);
                    break;

                case EventEffectType.AbandonTerritory:
                    if (!string.IsNullOrEmpty(effect.targetId))
                    {
                        var fs = worldState.GetFactionState(effect.targetId);
                        foreach (var rid in fs.ControlledRoomIds.ToList())
                        {
                            var rs = worldState.GetRoomState(rid);
                            rs.SetOwner(null, step);
                            rs.IsAbandoned = true;
                            rs.SemanticTags.Add("abandoned");
                        }
                        fs.ControlledRoomIds.Clear();
                        fs.TerritorySize = 0;
                    }
                    break;

                case EventEffectType.AddEnvironmentalTag:
                    if (targetRoomId >= 0 && !string.IsNullOrEmpty(effect.value))
                    {
                        var rs = worldState.GetRoomState(targetRoomId);
                        rs.SemanticTags.Add(effect.value);
                    }
                    break;
            }
        }

        private void SpreadToNeighbors(int step, int roomId, string tagType, float magnitude,
            NarrativeWorldState worldState, GenerationContext context, SeededRandom rng)
        {
            if (context.Graph == null || roomId < 0) return;

            var neighbors = context.Graph.GetNeighbors(roomId);
            foreach (var neighbor in neighbors)
            {
                if (rng.NextBool(System.Math.Abs(magnitude)))
                {
                    var rs = worldState.GetRoomState(neighbor.Id);
                    if (tagType == "corruption")
                    {
                        rs.Corruption += System.Math.Abs(magnitude) * 0.5f;
                        rs.SemanticTags.Add("corruption_spread");
                    }
                }
            }
        }

        private void ApplyGlobalDecay(int step, NarrativeConfig config, NarrativeWorldState worldState)
        {
            foreach (var roomState in worldState.RoomStates.Values)
            {
                // Natural decay in abandoned rooms
                if (roomState.IsAbandoned)
                    roomState.Decay = System.Math.Min(1f, roomState.Decay + config.decayRate * 0.1f);

                // Conflict intensity dies down over time
                roomState.ConflictIntensity = System.Math.Max(0f, roomState.ConflictIntensity - 0.05f);

                // Corruption slowly spreads
                if (roomState.Corruption > 0.5f)
                    roomState.Corruption = System.Math.Min(1f, roomState.Corruption + 0.01f);
            }
        }

        private void EnforceConstraints(int step, NarrativeConfig config, NarrativeWorldState worldState,
            GenerationContext context)
        {
            foreach (var constraint in config.constraints)
            {
                if (!constraint.isHard) continue;

                switch (constraint.type)
                {
                    case ConstraintType.FactionMustSurvive:
                        var faction = worldState.GetFactionState(constraint.targetFactionId);
                        if (faction.IsEliminated)
                        {
                            faction.IsEliminated = false;
                            faction.IsActive = true;
                            faction.Morale = 0.3f;
                            faction.Strength = 0.5f;
                            UnityEngine.Debug.Log($"[Narrative] Constraint: Revived {constraint.targetFactionId}");
                        }
                        break;

                    case ConstraintType.FactionMustFall:
                        // Will be enforced at end
                        break;
                }
            }
        }

        private void ProcessCascadingEvents(int step, NarrativeConfig config, NarrativeWorldState worldState,
            GenerationContext context, SeededRandom rng)
        {
            // Check for conditional/triggered events
            foreach (var evt in config.timelineEvents)
            {
                if (evt.timing != EventTiming.Conditional && evt.timing != EventTiming.Triggered) continue;
                if (worldState.Timeline.Entries.Any(e => e.EventType == evt.eventId)) continue; // Already fired

                if (CheckPreconditions(evt, worldState))
                {
                    ExecuteNarrativeEvent(step, evt, worldState, context, rng);
                }
            }
        }

        private void EnforceProtectedOutcomes(NarrativeConfig config, NarrativeWorldState worldState,
            GenerationContext context)
        {
            foreach (var outcome in config.protectedOutcomes)
            {
                switch (outcome.type)
                {
                    case ProtectedOutcomeType.FactionSurvives:
                        var faction = worldState.GetFactionState(outcome.factionId);
                        if (faction.IsEliminated)
                        {
                            faction.IsEliminated = false;
                            faction.IsActive = true;
                            faction.Morale = 0.2f;
                            UnityEngine.Debug.Log($"[Narrative] Protected outcome: {outcome.factionId} must survive");
                        }
                        break;

                    case ProtectedOutcomeType.FactionControlsArea:
                        // Ensure faction has at least some territory
                        var f = worldState.GetFactionState(outcome.factionId);
                        if (f.TerritorySize == 0 && context.SpatialMap != null)
                        {
                            for (int i = 0; i < context.SpatialMap.Rooms.Count; i++)
                            {
                                var rs = worldState.GetRoomState(i);
                                if (string.IsNullOrEmpty(rs.CurrentOwner))
                                {
                                    rs.SetOwner(outcome.factionId, worldState.CurrentStep);
                                    f.ControlledRoomIds.Add(i);
                                    f.TerritorySize++;
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
        }

        private void FinalizeWorldState(NarrativeWorldState worldState, GenerationContext context)
        {
            if (context.SpatialMap == null) return;

            for (int i = 0; i < context.SpatialMap.Rooms.Count; i++)
            {
                var roomState = worldState.GetRoomState(i);

                // Generate semantic label
                roomState.RoomSemanticLabel = GenerateSemanticLabel(roomState);

                // Apply final traversal safety
                if (roomState.IsCollapsed)
                    roomState.TraversalSafety = 0f;
                else if (roomState.IsFlooded)
                    roomState.TraversalSafety = System.Math.Min(roomState.TraversalSafety, 0.3f);
                else if (roomState.IsWarzone)
                    roomState.TraversalSafety = System.Math.Min(roomState.TraversalSafety, 0.5f);
            }
        }

        private string GenerateSemanticLabel(RoomNarrativeState roomState)
        {
            if (roomState.IsCollapsed) return "collapsed_ruin";
            if (roomState.IsRitualSite) return "ritual_chamber";
            if (roomState.IsWarzone) return "warzone";
            if (roomState.IsFlooded) return "flooded_chamber";
            if (roomState.IsBarricaded && !string.IsNullOrEmpty(roomState.CurrentOwner))
                return "fortified_position";
            if (roomState.IsAbandoned) return "abandoned_area";
            if (roomState.Corruption > 0.5f) return "corrupted_zone";
            if (roomState.IsSafeZone) return "safe_haven";
            if (!string.IsNullOrEmpty(roomState.CurrentOwner)) return "occupied_territory";
            return "neutral_area";
        }

        /// <summary>
        /// Bridges narrative simulation results into the existing HistoryLog for backward compatibility.
        /// </summary>
        private void BridgeToHistoryLog(NarrativeWorldState worldState, GenerationContext context)
        {
            if (context.HistoryLog == null)
                context.HistoryLog = new Data.HistoryLog();

            foreach (var entry in worldState.Timeline.Entries)
            {
                context.HistoryLog.AddEvent(new Data.HistoryEvent
                {
                    Step = entry.Step,
                    AgentType = entry.Actor,
                    EventType = entry.EventType,
                    AffectedRoomId = entry.AffectedRoomId,
                    Description = entry.Description,
                    Data = new Dictionary<string, string>(entry.Data)
                });

                if (entry.AffectedRoomId >= 0)
                {
                    context.HistoryLog.RecordFaction(entry.AffectedRoomId, entry.Actor);
                }
            }

            // Apply room states to spatial map
            if (context.SpatialMap != null)
            {
                foreach (var kvp in worldState.RoomStates)
                {
                    if (kvp.Key >= context.SpatialMap.Rooms.Count) continue;
                    var room = context.SpatialMap.Rooms[kvp.Key];
                    var state = kvp.Value;

                    if (!string.IsNullOrEmpty(state.CurrentOwner))
                        room.FactionOwner = state.CurrentOwner;

                    foreach (var tag in state.SemanticTags)
                    {
                        if (!room.Properties.ContainsKey("narrative_tags"))
                            room.Properties["narrative_tags"] = "";
                        room.Properties["narrative_tags"] += tag + ";";
                    }
                }
            }
        }
    }
}

