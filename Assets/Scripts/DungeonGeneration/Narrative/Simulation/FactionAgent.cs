using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Narrative.Authoring;
using DungeonGeneration.Narrative.WorldState;

namespace DungeonGeneration.Narrative.Simulation
{
    /// <summary>
    /// Generic faction simulation agent that drives faction behavior based on authored
    /// FactionDefinition data. Handles territory expansion, defense, conflict, migration,
    /// desperation escalation, and environmental consequences.
    /// </summary>
    public class FactionAgent : INarrativeAgent
    {
        public string AgentId => _definition.factionId;
        public string FactionId => _definition.factionId;
        public int Priority { get; }

        private readonly FactionDefinition _definition;
        private readonly int _priority;

        public FactionAgent(FactionDefinition definition, int priority = 0)
        {
            _definition = definition;
            _priority = priority;
            Priority = priority;
        }

        public void Initialize(NarrativeWorldState worldState, GenerationContext context, SeededRandom rng)
        {
            var state = worldState.GetFactionState(_definition.factionId);
            state.IsActive = true;
            state.Morale = 1f;
            state.Strength = 1f;
        }

        public void SimulateStep(int step, NarrativeWorldState worldState, GenerationContext context, SeededRandom rng)
        {
            var factionState = worldState.GetFactionState(_definition.factionId);
            if (!factionState.IsActive || factionState.IsEliminated) return;

            // Update desperation
            UpdateDesperation(factionState, worldState);

            // Check for desperation behaviors
            if (factionState.Desperation >= _definition.desperationThreshold)
            {
                ExecuteDesperationBehavior(step, factionState, worldState, context, rng);
            }

            // Normal behaviors based on behavior profile
            var profile = _definition.behaviorProfile;

            // Territorial expansion
            if (rng.NextBool(_definition.expansionDrive * (1f - factionState.Desperation)))
            {
                TryExpand(step, factionState, worldState, context, rng);
            }

            // Defense
            if (rng.NextBool(_definition.defensiveness))
            {
                Fortify(step, factionState, worldState, context, rng);
            }

            // Resource consumption
            ConsumeResources(step, factionState, worldState);

            // Conflicts with neighbors
            if (profile.aggression > 0f && rng.NextBool(profile.aggression * 0.3f))
            {
                TryConflict(step, factionState, worldState, context, rng);
            }

            // Environmental effects from occupation
            ApplyOccupationEffects(step, factionState, worldState, context, rng);

            // Morale decay
            factionState.Morale = System.Math.Max(0f, factionState.Morale - 0.02f);

            // Check elimination
            if (factionState.TerritorySize <= 0 && factionState.Morale <= 0.1f)
            {
                EliminateFaction(step, factionState, worldState);
            }
        }

        public void OnEventTriggered(string eventType, int roomId, NarrativeWorldState worldState)
        {
            var factionState = worldState.GetFactionState(_definition.factionId);

            switch (eventType)
            {
                case "attack":
                    factionState.Morale -= 0.1f;
                    factionState.Desperation += 0.15f;
                    break;
                case "territory_lost":
                    factionState.Morale -= 0.2f;
                    factionState.Desperation += 0.2f;
                    factionState.ControlledRoomIds.Remove(roomId);
                    factionState.TerritorySize = factionState.ControlledRoomIds.Count;
                    break;
                case "resource_depleted":
                    factionState.Desperation += 0.3f;
                    break;
                case "ally_destroyed":
                    factionState.Morale -= 0.15f;
                    break;
            }
        }

        public float GetDesperation()
        {
            return _definition.behaviorProfile.fanaticism > 0.5f ? 0f : 0.5f; // Fanatics don't feel desperation normally
        }

        private void UpdateDesperation(FactionState state, NarrativeWorldState worldState)
        {
            float desperation = 0f;

            // Territory pressure
            if (state.TerritorySize < _definition.preferredTerritorySize)
                desperation += (1f - (float)state.TerritorySize / _definition.preferredTerritorySize) * 0.4f;

            // Resource scarcity
            foreach (var need in _definition.resourceNeeds)
            {
                if (worldState.IsResourceScarce(need.resourceId))
                    desperation += need.criticality * 0.3f;
            }

            // Low morale
            if (state.Morale < 0.3f)
                desperation += (1f - state.Morale) * 0.3f;

            state.Desperation = System.Math.Min(1f, desperation);
        }

        private void ExecuteDesperationBehavior(int step, FactionState factionState,
            NarrativeWorldState worldState, GenerationContext context, SeededRandom rng)
        {
            if (_definition.desperationBehaviors.Count == 0) return;

            var triggered = _definition.desperationBehaviors
                .Where(b => factionState.Desperation >= b.triggerThreshold)
                .ToList();

            if (triggered.Count == 0) return;
            var behavior = rng.Choose(triggered);

            switch (behavior.action)
            {
                case DesperationAction.Fortify:
                    FortifyAllRooms(step, factionState, worldState);
                    break;
                case DesperationAction.Flee:
                    Migrate(step, factionState, worldState, context, rng);
                    break;
                case DesperationAction.Barricade:
                    BarricadeEntries(step, factionState, worldState, context);
                    break;
                case DesperationAction.LastStand:
                    LastStand(step, factionState, worldState);
                    break;
                case DesperationAction.PerformRitual:
                    PerformRitual(step, factionState, worldState, context, rng);
                    break;
                case DesperationAction.Abandon:
                    AbandonTerritory(step, factionState, worldState, context, rng);
                    break;
            }

            worldState.Timeline.Record(step, "desperation_behavior",
                $"{_definition.displayName} executes {behavior.action}: {behavior.description}",
                _definition.factionId, -1, 0.7f);
        }

        private void TryExpand(int step, FactionState factionState, NarrativeWorldState worldState,
            GenerationContext context, SeededRandom rng)
        {
            if (context.Graph == null) return;

            var expandableRooms = new List<int>();
            foreach (var roomId in factionState.ControlledRoomIds.ToList())
            {
                var neighbors = context.Graph.GetNeighbors(roomId);
                foreach (var neighbor in neighbors)
                {
                    if (neighbor.Id < context.SpatialMap.Rooms.Count)
                    {
                        var neighborState = worldState.GetRoomState(neighbor.Id);
                        if (string.IsNullOrEmpty(neighborState.CurrentOwner))
                            expandableRooms.Add(neighbor.Id);
                    }
                }
            }

            if (expandableRooms.Count == 0) return;

            var targetId = rng.Choose(expandableRooms);
            ClaimRoom(step, targetId, factionState, worldState);
        }

        private void ClaimRoom(int step, int roomId, FactionState factionState, NarrativeWorldState worldState)
        {
            var roomState = worldState.GetRoomState(roomId);
            roomState.SetOwner(_definition.factionId, step);
            worldState.Territories.SetOwner(roomId, _definition.factionId, step);
            factionState.ControlledRoomIds.Add(roomId);
            factionState.TerritorySize = factionState.ControlledRoomIds.Count;

            foreach (var tag in _definition.environmentalTags)
                if (!roomState.SemanticTags.Contains(tag))
                    roomState.SemanticTags.Add(tag);

            roomState.AddHistoryEntry(step, $"{_definition.displayName} claims this room", _definition.factionId, 0.4f);

            worldState.Timeline.Record(step, "claim_territory",
                $"{_definition.displayName} expands into Room {roomId}",
                _definition.factionId, roomId, 0.3f);
        }

        private void Fortify(int step, FactionState factionState, NarrativeWorldState worldState,
            GenerationContext context, SeededRandom rng)
        {
            if (factionState.ControlledRoomIds.Count == 0) return;

            // Fortify border rooms
            foreach (var roomId in factionState.ControlledRoomIds)
            {
                if (context.Graph == null) continue;
                var neighbors = context.Graph.GetNeighbors(roomId);
                bool isBorder = neighbors.Any(n =>
                {
                    var nState = worldState.GetRoomState(n.Id);
                    return nState.CurrentOwner != _definition.factionId;
                });

                if (isBorder && rng.NextBool(0.3f))
                {
                    var roomState = worldState.GetRoomState(roomId);
                    roomState.DangerLevel += 0.1f;
                    if (!roomState.SemanticTags.Contains("fortified"))
                        roomState.SemanticTags.Add("fortified");

                    roomState.AddHistoryEntry(step, $"{_definition.displayName} fortifies defenses",
                        _definition.factionId, 0.2f);
                }
            }
        }

        private void ConsumeResources(int step, FactionState factionState, NarrativeWorldState worldState)
        {
            foreach (var need in _definition.resourceNeeds)
            {
                float consumption = need.consumptionRate * factionState.TerritorySize * 0.1f;
                worldState.ConsumeResource(need.resourceId, consumption);

                if (worldState.IsResourceScarce(need.resourceId))
                {
                    factionState.Morale -= need.criticality * 0.05f;

                    worldState.Timeline.Record(step, "resource_scarcity",
                        $"{_definition.displayName} suffers from {need.resourceId} scarcity",
                        _definition.factionId, -1, need.criticality * 0.5f);
                }
            }
        }

        private void TryConflict(int step, FactionState factionState, NarrativeWorldState worldState,
            GenerationContext context, SeededRandom rng)
        {
            if (context.Graph == null) return;

            foreach (var roomId in factionState.ControlledRoomIds.ToList())
            {
                var neighbors = context.Graph.GetNeighbors(roomId);
                foreach (var neighbor in neighbors)
                {
                    if (neighbor.Id >= context.SpatialMap.Rooms.Count) continue;
                    var neighborState = worldState.GetRoomState(neighbor.Id);

                    if (!string.IsNullOrEmpty(neighborState.CurrentOwner) &&
                        neighborState.CurrentOwner != _definition.factionId)
                    {
                        float disposition = worldState.GetDisposition(_definition.factionId, neighborState.CurrentOwner);
                        if (disposition < -0.3f || _definition.behaviorProfile.aggression > 0.6f)
                        {
                            ExecuteConflict(step, roomId, neighbor.Id, neighborState.CurrentOwner,
                                factionState, worldState, context, rng);
                            return; // One conflict per step
                        }
                    }
                }
            }
        }

        private void ExecuteConflict(int step, int attackerRoomId, int defenderRoomId, string defenderFactionId,
            FactionState attackerState, NarrativeWorldState worldState, GenerationContext context, SeededRandom rng)
        {
            var defenderState = worldState.GetFactionState(defenderFactionId);
            float attackPower = attackerState.Strength * _definition.behaviorProfile.aggression;
            float defensePower = defenderState.Strength * (1f - defenderState.Desperation * 0.5f);

            bool attackerWins = rng.NextFloat() < attackPower / (attackPower + defensePower + 0.1f);

            var defenderRoom = worldState.GetRoomState(defenderRoomId);
            var attackerRoom = worldState.GetRoomState(attackerRoomId);

            // Both sides take damage
            defenderRoom.ConflictIntensity += 0.4f;
            defenderRoom.StructuralDamage += 0.15f;
            defenderRoom.IsWarzone = true;
            attackerRoom.ConflictIntensity += 0.2f;

            if (!defenderRoom.SemanticTags.Contains("battle_site"))
                defenderRoom.SemanticTags.Add("battle_site");

            if (attackerWins)
            {
                // Transfer territory
                defenderState.ControlledRoomIds.Remove(defenderRoomId);
                defenderState.TerritorySize = defenderState.ControlledRoomIds.Count;
                ClaimRoom(step, defenderRoomId, attackerState, worldState);

                defenderRoom.AddHistoryEntry(step,
                    $"{_definition.displayName} conquers from {defenderFactionId}",
                    _definition.factionId, 0.8f);

                // Notify defender
                var defenderAgent = worldState.GetFactionState(defenderFactionId);
                defenderAgent.Morale -= 0.2f;
            }
            else
            {
                attackerState.Morale -= 0.15f;
                attackerState.Strength -= 0.1f;
                defenderRoom.AddHistoryEntry(step,
                    $"{defenderFactionId} repels attack by {_definition.displayName}",
                    defenderFactionId, 0.6f);
            }

            worldState.Timeline.Record(step, "conflict",
                $"Battle at Room {defenderRoomId}: {_definition.displayName} vs {defenderFactionId} → " +
                $"{(attackerWins ? _definition.displayName + " wins" : defenderFactionId + " holds")}",
                _definition.factionId, defenderRoomId, 0.8f);
        }

        private void ApplyOccupationEffects(int step, FactionState factionState,
            NarrativeWorldState worldState, GenerationContext context, SeededRandom rng)
        {
            foreach (var roomId in factionState.ControlledRoomIds)
            {
                var roomState = worldState.GetRoomState(roomId);

                // Apply occupation-style tags
                if (rng.NextBool(0.2f) && !string.IsNullOrEmpty(_definition.occupationStyle))
                {
                    var tag = $"occupied_{_definition.occupationStyle}";
                    if (!roomState.SemanticTags.Contains(tag))
                        roomState.SemanticTags.Add(tag);
                }

                // Natural decay of unoccupied rooms
                roomState.Decay += 0.01f;

                // Mark as safe zone if no conflict
                if (roomState.ConflictIntensity < 0.1f && factionState.TerritorySize >= 2)
                    roomState.IsSafeZone = true;
            }
        }

        private void FortifyAllRooms(int step, FactionState factionState, NarrativeWorldState worldState)
        {
            foreach (var roomId in factionState.ControlledRoomIds)
            {
                var roomState = worldState.GetRoomState(roomId);
                roomState.IsBarricaded = true;
                if (!roomState.SemanticTags.Contains("emergency_fortification"))
                    roomState.SemanticTags.Add("emergency_fortification");
                roomState.AddHistoryEntry(step, $"{_definition.displayName} desperately fortifies",
                    _definition.factionId, 0.6f);
            }
        }

        private void Migrate(int step, FactionState factionState, NarrativeWorldState worldState,
            GenerationContext context, SeededRandom rng)
        {
            if (context.Graph == null || factionState.ControlledRoomIds.Count == 0) return;

            // Abandon current territory and move to farthest available rooms
            var abandonedRooms = new List<int>(factionState.ControlledRoomIds);
            foreach (var roomId in abandonedRooms)
            {
                var roomState = worldState.GetRoomState(roomId);
                roomState.SetOwner(null, step);
                roomState.IsAbandoned = true;
                roomState.SemanticTags.Add("abandoned_in_haste");
                roomState.AddHistoryEntry(step, $"{_definition.displayName} flees in desperation",
                    _definition.factionId, 0.7f);

                worldState.Timeline.Record(step, "migration",
                    $"{_definition.displayName} abandons Room {roomId}",
                    _definition.factionId, roomId, 0.6f);
            }

            factionState.ControlledRoomIds.Clear();
            factionState.TerritorySize = 0;

            // Find new rooms far from danger
            if (context.SpatialMap != null)
            {
                var candidates = new List<int>();
                for (int i = 0; i < context.SpatialMap.Rooms.Count; i++)
                {
                    var rs = worldState.GetRoomState(i);
                    if (string.IsNullOrEmpty(rs.CurrentOwner) && rs.DangerLevel < 0.3f)
                        candidates.Add(i);
                }

                if (candidates.Count > 0)
                {
                    int newHome = rng.Choose(candidates);
                    ClaimRoom(step, newHome, factionState, worldState);
                }
            }
        }

        private void BarricadeEntries(int step, FactionState factionState,
            NarrativeWorldState worldState, GenerationContext context)
        {
            foreach (var roomId in factionState.ControlledRoomIds)
            {
                var roomState = worldState.GetRoomState(roomId);
                roomState.IsBarricaded = true;
                roomState.TraversalSafety -= 0.3f;
                if (!roomState.SemanticTags.Contains("barricaded"))
                    roomState.SemanticTags.Add("barricaded");
            }
        }

        private void LastStand(int step, FactionState factionState, NarrativeWorldState worldState)
        {
            factionState.Strength *= 1.5f; // Temporary boost
            factionState.Morale = 0.5f;
            foreach (var roomId in factionState.ControlledRoomIds)
            {
                var roomState = worldState.GetRoomState(roomId);
                roomState.SemanticTags.Add("last_stand");
                roomState.ConflictIntensity += 0.3f;
            }
        }

        private void PerformRitual(int step, FactionState factionState, NarrativeWorldState worldState,
            GenerationContext context, SeededRandom rng)
        {
            if (factionState.ControlledRoomIds.Count == 0) return;
            int ritualRoom = rng.Choose(factionState.ControlledRoomIds);
            var roomState = worldState.GetRoomState(ritualRoom);
            roomState.IsRitualSite = true;
            roomState.Corruption += 0.3f;
            roomState.SemanticTags.Add("desperate_ritual");
            roomState.AddHistoryEntry(step, $"{_definition.displayName} performs desperate ritual",
                _definition.factionId, 0.9f);

            worldState.Timeline.Record(step, "ritual",
                $"{_definition.displayName} performs a desperate ritual in Room {ritualRoom}",
                _definition.factionId, ritualRoom, 0.9f);
        }

        private void AbandonTerritory(int step, FactionState factionState, NarrativeWorldState worldState,
            GenerationContext context, SeededRandom rng)
        {
            var rooms = new List<int>(factionState.ControlledRoomIds);
            // Keep only one room (most defensible)
            if (rooms.Count <= 1) return;

            for (int i = 1; i < rooms.Count; i++)
            {
                var roomState = worldState.GetRoomState(rooms[i]);
                roomState.SetOwner(null, step);
                roomState.IsAbandoned = true;
                roomState.SemanticTags.Add("abandoned");
                factionState.ControlledRoomIds.Remove(rooms[i]);
            }
            factionState.TerritorySize = factionState.ControlledRoomIds.Count;
        }

        private void EliminateFaction(int step, FactionState factionState, NarrativeWorldState worldState)
        {
            factionState.IsActive = false;
            factionState.IsEliminated = true;
            factionState.StepEliminated = step;

            foreach (var roomId in factionState.ControlledRoomIds)
            {
                var roomState = worldState.GetRoomState(roomId);
                roomState.SetOwner(null, step);
                roomState.IsAbandoned = true;
                roomState.SemanticTags.Add("faction_destroyed_here");
            }

            factionState.ControlledRoomIds.Clear();
            factionState.TerritorySize = 0;

            worldState.Timeline.Record(step, "faction_eliminated",
                $"{_definition.displayName} has been eliminated",
                _definition.factionId, -1, 1.0f);
        }
    }
}

