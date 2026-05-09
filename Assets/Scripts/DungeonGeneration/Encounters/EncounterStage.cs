using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Data;
using UnityEngine;

namespace DungeonGeneration.Encounters
{
    /// <summary>
    /// Stage 8: Generates enemy encounters based on room semantics,
    /// faction ownership, difficulty, and history.
    /// </summary>
    public class EncounterStage : IGenerationStage
    {
        public string StageName => "Encounter Generation";
        public int Priority => 800;
        public void Execute(GenerationContext context)
        {
            var rng = context.Random.Fork("encounters");
            var config = context.Config;
            int maxDifficulty = context.Graph.Nodes.Max(n => n.DifficultyTier);
            foreach (var room in context.SpatialMap.Rooms)
            {
                if (!rng.NextBool(config.encounterDensity)) continue;
                if (room.FloorTiles.Count < 4) continue;
                var node = context.Graph.GetNode(room.GraphNodeId);
                if (node == null) continue;
                // No encounters in start room
                if (node.RoomType == RoomType.Start) continue;
                float normalizedDifficulty = maxDifficulty > 0 ? (float)node.DifficultyTier / maxDifficulty : 0f;
                float difficulty = config.difficultyEscalation.Evaluate(normalizedDifficulty);
                var encounter = GenerateEncounter(room, node, difficulty, context, rng);
                if (encounter != null)
                    context.Encounters.Add(encounter);
            }
        }
        private EncounterInstance GenerateEncounter(RoomInstance room, GraphNode node,
            float difficulty, GenerationContext context, SeededRandom rng)
        {
            var encounter = new EncounterInstance
            {
                RoomId = room.Id,
                Difficulty = difficulty,
                FactionId = room.FactionOwner ?? "neutral"
            };
            // Determine encounter type based on room purpose and position
            encounter.Type = DetermineEncounterType(room, node, rng);
            // Generate spawn points
            int enemyCount = Mathf.Max(1, Mathf.RoundToInt(difficulty * 5));
            var usedPositions = new HashSet<Vector2Int>();
            for (int i = 0; i < enemyCount && i < room.FloorTiles.Count; i++)
            {
                Vector2Int pos;
                int attempts = 0;
                do
                {
                    pos = rng.Choose(room.FloorTiles);
                    attempts++;
                } while (usedPositions.Contains(pos) && attempts < 20);
                if (attempts >= 20) continue;
                usedPositions.Add(pos);
                encounter.SpawnPoints.Add(new SpawnPoint
                {
                    Position = pos,
                    EnemyTypeId = GetEnemyType(encounter, difficulty, rng),
                    BehaviorOverride = encounter.Type == EncounterType.Ambush ? "hide" : null
                });
            }
            return encounter;
        }
        private EncounterType DetermineEncounterType(RoomInstance room, GraphNode node, SeededRandom rng)
        {
            if (node.RoomType == RoomType.Boss) return EncounterType.Boss;
            if (node.RoomType == RoomType.MiniBoss) return EncounterType.MiniBoss;
            if (room.Purpose == RoomPurposeType.Nest) return EncounterType.Nest;
            // Check for faction conflicts
            var factions = new HashSet<string>();
            if (!string.IsNullOrEmpty(room.FactionOwner)) factions.Add(room.FactionOwner);
            if (factions.Count > 1) return EncounterType.FactionConflict;
            var types = new[] { EncounterType.Patrol, EncounterType.Guard, EncounterType.Ambush };
            var weights = new[] { 0.4f, 0.35f, 0.25f };
            return rng.ChooseWeighted(types, weights);
        }
        private string GetEnemyType(EncounterInstance encounter, float difficulty, SeededRandom rng)
        {
            string faction = encounter.FactionId;
            if (faction == "Cultists")
                return rng.ChooseWeighted(
                    new[] { "cultist_acolyte", "cultist_priest", "cultist_champion" },
                    new[] { 0.5f, 0.35f, 0.15f * difficulty });
            if (faction == "Monsters")
                return rng.ChooseWeighted(
                    new[] { "spider", "rat_swarm", "cave_troll", "shadow_beast" },
                    new[] { 0.3f, 0.3f, 0.25f * difficulty, 0.15f * difficulty });
            // Generic enemies
            return rng.ChooseWeighted(
                new[] { "skeleton", "zombie", "ghost", "golem" },
                new[] { 0.4f, 0.3f, 0.2f, 0.1f * Mathf.Max(0.1f, difficulty) });
        }
    }
}
