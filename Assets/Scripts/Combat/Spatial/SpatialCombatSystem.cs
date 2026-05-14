// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Spatial Combat System
// =============================================================================
// Body blocking, crowd density, chokepoint detection, and weapon reach management.
// Makes physical space tactically meaningful.
// =============================================================================

using System.Collections.Generic;
using Combat.Core;
using UnityEngine;

namespace Combat.Spatial
{
    /// <summary>
    /// Spatial combat system that evaluates space around agents.
    /// Handles crowd density, body blocking, and spatial queries.
    /// </summary>
    public class SpatialCombatSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _bodyBlockRadius = 0.5f;
        [SerializeField] private float _crowdDensityRadius = 3f;
        [SerializeField] private float _updateInterval = 0.3f;
        [SerializeField] private LayerMask _wallLayer;
        [SerializeField] private LayerMask _cliffLayer;

        private float _lastUpdateTime;

        // Cached spatial data per agent
        private readonly Dictionary<CombatAgent, SpatialData> _spatialData = new Dictionary<CombatAgent, SpatialData>();

        public static SpatialCombatSystem Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Time.time - _lastUpdateTime < _updateInterval) return;
            _lastUpdateTime = Time.time;
            UpdateSpatialData();
        }

        private void UpdateSpatialData()
        {
            foreach (var agent in CombatAgentRegistry.AllAgents)
            {
                if (!agent.IsAlive) continue;
                if (!_spatialData.ContainsKey(agent))
                    _spatialData[agent] = new SpatialData();

                var data = _spatialData[agent];
                CalculateSpatialData(agent, data);
            }

            // Clean up dead agents
            var toRemove = new List<CombatAgent>();
            foreach (var kvp in _spatialData)
            {
                if (kvp.Key == null || !kvp.Key.IsAlive)
                    toRemove.Add(kvp.Key);
            }
            foreach (var key in toRemove)
                _spatialData.Remove(key);
        }

        private void CalculateSpatialData(CombatAgent agent, SpatialData data)
        {
            Vector3 pos = agent.transform.position;

            // Wall detection
            data.NearestWallDistance = float.MaxValue;
            data.NearestWallNormal = Vector3.zero;
            if (Physics.Raycast(pos, agent.transform.forward, out var hitFwd, 10f, _wallLayer))
            {
                data.NearestWallDistance = hitFwd.distance;
                data.NearestWallNormal = hitFwd.normal;
            }
            // Ray in multiple directions for better wall detection
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                if (Physics.Raycast(pos, dir, out var hit, 10f, _wallLayer))
                {
                    if (hit.distance < data.NearestWallDistance)
                    {
                        data.NearestWallDistance = hit.distance;
                        data.NearestWallNormal = hit.normal;
                    }
                }
            }

            // Cliff detection
            data.NearestCliffDistance = float.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                if (Physics.Raycast(pos, dir, out var hit, 10f, _cliffLayer))
                {
                    if (hit.distance < data.NearestCliffDistance)
                        data.NearestCliffDistance = hit.distance;
                }
            }

            // Corridor detection (walls on both sides within narrow distance)
            int wallCount = 0;
            float corridorThreshold = 4f;
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                if (Physics.Raycast(pos, dir, out var hit, corridorThreshold, _wallLayer))
                    wallCount++;
            }
            data.IsInCorridor = wallCount >= 2;

            // Chokepoint = corridor with enemies on one side
            data.IsInChokepoint = data.IsInCorridor && agent.Context != null && agent.Context.EngagementCount > 0;

            // Open space = no walls nearby
            data.IsInOpenSpace = data.NearestWallDistance > 6f;

            // Doorway detection (very narrow corridor)
            data.IsInDoorway = wallCount >= 3;

            // Crowd density
            data.LocalCrowdDensity = 0;
            foreach (var other in CombatAgentRegistry.AllAgents)
            {
                if (other == agent) continue;
                if (Vector3.Distance(pos, other.transform.position) < _crowdDensityRadius)
                    data.LocalCrowdDensity++;
            }
        }

        /// <summary>Get spatial data for an agent.</summary>
        public SpatialData GetSpatialData(CombatAgent agent)
        {
            if (_spatialData.TryGetValue(agent, out var data))
                return data;
            return new SpatialData();
        }

        /// <summary>
        /// Check if movement from A to B is blocked by other agents (body blocking).
        /// </summary>
        public bool IsPathBodyBlocked(Vector3 from, Vector3 to, CombatAgent mover)
        {
            Vector3 dir = (to - from).normalized;
            float dist = Vector3.Distance(from, to);

            foreach (var agent in CombatAgentRegistry.AllAgents)
            {
                if (agent == mover || !agent.IsAlive) continue;

                // Check if agent is roughly on the path
                Vector3 toAgent = agent.transform.position - from;
                float dot = Vector3.Dot(toAgent, dir);
                if (dot < 0 || dot > dist) continue;

                Vector3 closestPoint = from + dir * dot;
                float perpDist = Vector3.Distance(closestPoint, agent.transform.position);
                if (perpDist < _bodyBlockRadius * 2)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get the terrain feature at a position.
        /// </summary>
        public TerrainFeature GetTerrainFeatureAt(Vector3 position)
        {
            SpatialData data = new SpatialData();
            // Quick raycasts
            int wallCount = 0;
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                if (Physics.Raycast(position, dir, 3f, _wallLayer))
                    wallCount++;
            }

            if (wallCount >= 3) return TerrainFeature.Doorway;
            if (wallCount >= 2) return TerrainFeature.Corridor;
            if (wallCount == 0) return TerrainFeature.OpenSpace;
            return TerrainFeature.Wall;
        }
    }

    /// <summary>
    /// Cached spatial data for a combat agent.
    /// </summary>
    [System.Serializable]
    public class SpatialData
    {
        public float NearestWallDistance = float.MaxValue;
        public Vector3 NearestWallNormal;
        public float NearestCliffDistance = float.MaxValue;
        public bool IsInCorridor;
        public bool IsInChokepoint;
        public bool IsInDoorway;
        public bool IsInOpenSpace;
        public int LocalCrowdDensity;
    }
}

