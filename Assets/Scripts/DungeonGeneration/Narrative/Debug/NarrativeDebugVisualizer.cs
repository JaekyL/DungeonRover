#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Narrative.Authoring;
using DungeonGeneration.Narrative.WorldState;
using UnityEngine;

namespace DungeonGeneration.Narrative.Debug
{
    /// <summary>
    /// Gizmo-based visualizer for narrative simulation data in the Scene View.
    /// Draws faction territories, conflict zones, corruption spread, room states,
    /// and ownership boundaries.
    /// </summary>
    [RequireComponent(typeof(DungeonGenerator))]
    public class NarrativeDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization Layers")]
        public bool showTerritories = true;
        public bool showConflictZones = true;
        public bool showCorruption = true;
        public bool showDamage = false;
        public bool showAbandoned = true;
        public bool showBarricades = true;
        public bool showRitualSites = true;
        public bool showRoomLabels = true;

        [Header("Display")]
        [Range(0.1f, 2f)] public float tileSize = 1f;
        [Range(0f, 1f)] public float overlayAlpha = 0.3f;

        [Header("Faction Colors")]
        public List<FactionColorOverride> factionColors = new List<FactionColorOverride>();

        private DungeonGenerator _generator;

        private void OnDrawGizmos()
        {
            if (!Application.isEditor) return;

            _generator = GetComponent<DungeonGenerator>();
            if (_generator?.LastContext == null) return;

            var worldState = _generator.LastContext.GetCustomData<NarrativeWorldState>("narrative_world_state");
            if (worldState == null) return;

            var ctx = _generator.LastContext;
            if (ctx.SpatialMap == null) return;

            if (showTerritories) DrawTerritories(worldState, ctx);
            if (showConflictZones) DrawConflictZones(worldState, ctx);
            if (showCorruption) DrawCorruption(worldState, ctx);
            if (showDamage) DrawDamage(worldState, ctx);
            if (showAbandoned) DrawAbandoned(worldState, ctx);
            if (showBarricades) DrawBarricades(worldState, ctx);
            if (showRitualSites) DrawRitualSites(worldState, ctx);
            if (showRoomLabels) DrawRoomLabels(worldState, ctx);
        }

        private void DrawTerritories(NarrativeWorldState worldState, GenerationContext ctx)
        {
            foreach (var room in ctx.SpatialMap.Rooms)
            {
                var state = worldState.GetRoomState(room.Id);
                if (string.IsNullOrEmpty(state.CurrentOwner)) continue;

                Color color = GetFactionColor(state.CurrentOwner);
                color.a = overlayAlpha;
                Gizmos.color = color;

                foreach (var tile in room.FloorTiles)
                {
                    Vector3 pos = new Vector3(tile.x * tileSize, 0, tile.y * tileSize);
                    Gizmos.DrawCube(pos, new Vector3(tileSize * 0.9f, 0.05f, tileSize * 0.9f));
                }

                // Draw territory border
                color.a = overlayAlpha * 2f;
                Gizmos.color = color;
                var center = new Vector3(room.Bounds.center.x * tileSize, 0.1f, room.Bounds.center.y * tileSize);
                var size = new Vector3(room.Bounds.width * tileSize, 0.01f, room.Bounds.height * tileSize);
                Gizmos.DrawWireCube(center, size);
            }
        }

        private void DrawConflictZones(NarrativeWorldState worldState, GenerationContext ctx)
        {
            foreach (var room in ctx.SpatialMap.Rooms)
            {
                var state = worldState.GetRoomState(room.Id);
                if (state.ConflictIntensity < 0.1f && !state.IsWarzone) continue;

                float intensity = Mathf.Max(state.ConflictIntensity, state.IsWarzone ? 0.5f : 0f);
                Gizmos.color = new Color(1f, 0f, 0f, intensity * overlayAlpha);

                var center = new Vector3(room.Bounds.center.x * tileSize, 0.15f, room.Bounds.center.y * tileSize);
                float radius = Mathf.Sqrt(room.FloorTiles.Count) * tileSize * 0.3f;
                Gizmos.DrawWireSphere(center, radius);

                // Crossed swords icon (X mark)
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
                float s = tileSize * 0.8f;
                Gizmos.DrawLine(center + new Vector3(-s, 0, -s), center + new Vector3(s, 0, s));
                Gizmos.DrawLine(center + new Vector3(-s, 0, s), center + new Vector3(s, 0, -s));
            }
        }

        private void DrawCorruption(NarrativeWorldState worldState, GenerationContext ctx)
        {
            foreach (var room in ctx.SpatialMap.Rooms)
            {
                var state = worldState.GetRoomState(room.Id);
                if (state.Corruption < 0.1f) continue;

                Gizmos.color = new Color(0.4f, 0f, 0.8f, state.Corruption * overlayAlpha);

                foreach (var tile in room.FloorTiles)
                {
                    Vector3 pos = new Vector3(tile.x * tileSize, 0.02f, tile.y * tileSize);
                    Gizmos.DrawCube(pos, new Vector3(tileSize * 0.8f, 0.02f, tileSize * 0.8f));
                }
            }
        }

        private void DrawDamage(NarrativeWorldState worldState, GenerationContext ctx)
        {
            foreach (var room in ctx.SpatialMap.Rooms)
            {
                var state = worldState.GetRoomState(room.Id);
                if (state.StructuralDamage < 0.2f) continue;

                Gizmos.color = new Color(0.8f, 0.6f, 0f, state.StructuralDamage * overlayAlpha);
                var center = new Vector3(room.Bounds.center.x * tileSize, 0.2f, room.Bounds.center.y * tileSize);
                var size = new Vector3(room.Bounds.width * tileSize * 0.8f, 0.01f, room.Bounds.height * tileSize * 0.8f);
                Gizmos.DrawWireCube(center, size);
            }
        }

        private void DrawAbandoned(NarrativeWorldState worldState, GenerationContext ctx)
        {
            foreach (var room in ctx.SpatialMap.Rooms)
            {
                var state = worldState.GetRoomState(room.Id);
                if (!state.IsAbandoned) continue;

                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, overlayAlpha * 0.5f);
                var center = new Vector3(room.Bounds.center.x * tileSize, 0.05f, room.Bounds.center.y * tileSize);
                var size = new Vector3(room.Bounds.width * tileSize, 0.01f, room.Bounds.height * tileSize);
                Gizmos.DrawCube(center, size);
            }
        }

        private void DrawBarricades(NarrativeWorldState worldState, GenerationContext ctx)
        {
            foreach (var room in ctx.SpatialMap.Rooms)
            {
                var state = worldState.GetRoomState(room.Id);
                if (!state.IsBarricaded) continue;

                Gizmos.color = new Color(0.8f, 0.5f, 0f, 0.8f);
                foreach (var entry in room.EntryPoints)
                {
                    Vector3 pos = new Vector3(entry.x * tileSize, 0.3f, entry.y * tileSize);
                    Gizmos.DrawCube(pos, new Vector3(tileSize, 0.5f, tileSize * 0.3f));
                }
            }
        }

        private void DrawRitualSites(NarrativeWorldState worldState, GenerationContext ctx)
        {
            foreach (var room in ctx.SpatialMap.Rooms)
            {
                var state = worldState.GetRoomState(room.Id);
                if (!state.IsRitualSite) continue;

                Gizmos.color = new Color(0.6f, 0f, 0.8f, 0.6f);
                var center = new Vector3(room.Bounds.center.x * tileSize, 0.1f, room.Bounds.center.y * tileSize);
                float radius = tileSize * 2f;

                // Draw pentagram-like shape
                for (int i = 0; i < 5; i++)
                {
                    float angle1 = i * 72f * Mathf.Deg2Rad;
                    float angle2 = ((i + 2) % 5) * 72f * Mathf.Deg2Rad;
                    Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
                    Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;
                    Gizmos.DrawLine(p1, p2);
                }
            }
        }

        private void DrawRoomLabels(NarrativeWorldState worldState, GenerationContext ctx)
        {
#if UNITY_EDITOR
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };

            foreach (var room in ctx.SpatialMap.Rooms)
            {
                var state = worldState.GetRoomState(room.Id);
                if (string.IsNullOrEmpty(state.RoomSemanticLabel) || state.RoomSemanticLabel == "neutral_area")
                    continue;

                Vector3 pos = new Vector3(room.Bounds.center.x * tileSize, 0.5f, room.Bounds.center.y * tileSize);
                UnityEditor.Handles.Label(pos, $"R{room.Id}: {state.RoomSemanticLabel}", style);
            }
#endif
        }

        private Color GetFactionColor(string factionId)
        {
            foreach (var c in factionColors)
            {
                if (c.factionId == factionId)
                    return c.color;
            }

            // Auto-generate color from faction name hash
            int hash = factionId.GetHashCode();
            return Color.HSVToRGB(
                Mathf.Abs(hash % 1000) / 1000f,
                0.6f + (hash % 100) / 250f,
                0.7f + (hash % 50) / 150f
            );
        }
    }

    [System.Serializable]
    public class FactionColorOverride
    {
        public string factionId;
        public Color color = Color.white;
    }
}
#endif

