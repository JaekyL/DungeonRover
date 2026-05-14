using TraversalAI.Core;
using TraversalAI.Goals;
using TraversalAI.Perception;
using TraversalAI.UtilityAI;
using UnityEngine;

namespace TraversalAI.Debug
{
    /// <summary>
    /// Gizmo-based debug visualizer for the traversal AI system.
    /// Renders dungeon graph, perception, goals, paths, and influence maps in the Scene view.
    /// </summary>
    [RequireComponent(typeof(TraversalAIController))]
    public class TraversalDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization Toggles")]
        public bool showDungeonGraph = true;
        public bool showPerception = true;
        public bool showCurrentGoal = true;
        public bool showPath = true;
        public bool showNodeScores = true;
        public bool showInfluenceMap;
        public bool showMemoryTrail = true;
        public bool showUtilityBreakdown = true;

        [Header("Influence Map Visualization")]
        public InfluenceMap.InfluenceLayerType influenceLayerToShow = InfluenceMap.InfluenceLayerType.Danger;
        [Range(0.1f, 5f)] public float influenceCellScale = 1f;

        [Header("Colors")]
        public Color unexploredColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public Color exploredColor = new Color(0.2f, 0.8f, 0.2f, 0.7f);
        public Color dangerousColor = new Color(1f, 0.2f, 0.2f, 0.7f);
        public Color lootColor = new Color(1f, 0.9f, 0.1f, 0.7f);
        public Color safeColor = new Color(0.2f, 0.6f, 1f, 0.7f);
        public Color currentNodeColor = Color.white;
        public Color goalColor = new Color(0f, 1f, 0.5f, 1f);
        public Color pathColor = new Color(0f, 0.8f, 1f, 0.8f);
        public Color edgeColor = new Color(0.6f, 0.6f, 0.6f, 0.4f);
        public Color memoryTrailColor = new Color(0.4f, 0.2f, 0.8f, 0.3f);

        private TraversalAIController _controller;

        private void Awake()
        {
            _controller = GetComponent<TraversalAIController>();
        }

        private void OnDrawGizmos()
        {
            if (_controller == null || _controller.DungeonGraph == null) return;

            var graph = _controller.DungeonGraph;

            if (showDungeonGraph) DrawDungeonGraph(graph);
            if (showPerception) DrawPerception(graph);
            if (showCurrentGoal) DrawCurrentGoal(graph);
            if (showPath) DrawPath(graph);
            if (showMemoryTrail) DrawMemoryTrail(graph);
            if (showInfluenceMap) DrawInfluenceMap();
            if (showNodeScores) DrawNodeScores(graph);
        }

        private void DrawDungeonGraph(TraversalDungeonGraph graph)
        {
            // Draw edges
            foreach (var edge in graph.Edges)
            {
                var fromNode = graph.GetNode(edge.FromNodeId);
                var toNode = graph.GetNode(edge.ToNodeId);
                if (fromNode == null || toNode == null) continue;

                Gizmos.color = edge.IsSecret ? new Color(1f, 0.5f, 0f, 0.3f) : edgeColor;
                if (!edge.IsPassable)
                    Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

                Gizmos.DrawLine(fromNode.WorldPosition, toNode.WorldPosition);
            }

            // Draw nodes
            foreach (var node in graph.Nodes)
            {
                Gizmos.color = GetNodeColor(node);
                float size = node.HasTag(NodeTag.HubRoom) ? 1.5f : 0.8f;
                Gizmos.DrawSphere(node.WorldPosition, size);

                // Draw node ID label
#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    node.WorldPosition + Vector3.up * 2f,
                    $"{node.Id}: {node.Label}");
#endif
            }

            // Highlight current node
            var currentNode = graph.GetNode(_controller.CurrentNodeId);
            if (currentNode != null)
            {
                Gizmos.color = currentNodeColor;
                Gizmos.DrawWireSphere(currentNode.WorldPosition, 2f);
            }
        }

        private Color GetNodeColor(DungeonNode node)
        {
            if (node.HasTag(NodeTag.Dangerous) || node.HasTag(NodeTag.BossArea))
                return dangerousColor;
            if (node.HasTag(NodeTag.Loot))
                return lootColor;
            if (node.HasTag(NodeTag.SafeZone))
                return safeColor;
            if (node.HasTag(NodeTag.Explored))
                return exploredColor;
            return unexploredColor;
        }

        private void DrawPerception(TraversalDungeonGraph graph)
        {
            if (_controller.Perception?.PerceivedState == null) return;

            var perceivedState = _controller.Perception.PerceivedState;
            var visibleIds = perceivedState.GetVisibleNodeIds();

            foreach (var nodeId in visibleIds)
            {
                var node = graph.GetNode(nodeId);
                if (node == null) continue;

                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(node.WorldPosition, 1.5f);
            }

            // Draw perception radius
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, _controller.Perception.PerceptionRadius);
        }

        private void DrawCurrentGoal(TraversalDungeonGraph graph)
        {
            if (_controller.CurrentGoal == null) return;

            int targetId = _controller.CurrentGoal.TargetNodeId;
            var targetNode = graph.GetNode(targetId);
            if (targetNode == null) return;

            // Draw goal marker
            Gizmos.color = goalColor;
            Gizmos.DrawWireCube(targetNode.WorldPosition, Vector3.one * 3f);

            // Draw line from current position to goal
            Gizmos.color = new Color(goalColor.r, goalColor.g, goalColor.b, 0.4f);
            Gizmos.DrawLine(transform.position, targetNode.WorldPosition);

            // Label
#if UNITY_EDITOR
            UnityEditor.Handles.color = goalColor;
            UnityEditor.Handles.Label(
                targetNode.WorldPosition + Vector3.up * 3.5f,
                $"GOAL: {_controller.CurrentGoal.GetDescription()}");
#endif
        }

        private void DrawPath(TraversalDungeonGraph graph)
        {
            // Draw tile-level waypoints if available (actual movement path)
            var tileWaypoints = _controller.TileWaypoints;
            if (tileWaypoints != null && tileWaypoints.Count > 1)
            {
                int wpIndex = _controller.WaypointIndex;
                for (int i = Mathf.Max(0, wpIndex - 1); i < tileWaypoints.Count - 1; i++)
                {
                    // Fade already-visited waypoints
                    float alpha = i < wpIndex ? 0.15f : 0.8f;
                    Gizmos.color = new Color(pathColor.r, pathColor.g, pathColor.b, alpha);
                    Gizmos.DrawLine(tileWaypoints[i], tileWaypoints[i + 1]);
                }

                // Draw small spheres at waypoint corners
                Gizmos.color = new Color(pathColor.r, pathColor.g, pathColor.b, 0.6f);
                for (int i = wpIndex; i < tileWaypoints.Count; i++)
                {
                    Gizmos.DrawSphere(tileWaypoints[i], 0.15f);
                }

                // Highlight current target waypoint
                if (wpIndex < tileWaypoints.Count)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(tileWaypoints[wpIndex], 0.3f);
                }

                return;
            }

            // Fallback: draw graph-level path
            if (_controller.CurrentPath == null || !_controller.CurrentPath.Success) return;

            var path = _controller.CurrentPath.NodePath;
            Gizmos.color = pathColor;

            for (int i = 0; i < path.Count - 1; i++)
            {
                var fromNode = graph.GetNode(path[i]);
                var toNode = graph.GetNode(path[i + 1]);
                if (fromNode == null || toNode == null) continue;

                Gizmos.DrawLine(
                    fromNode.WorldPosition + Vector3.up * 0.5f,
                    toNode.WorldPosition + Vector3.up * 0.5f);
            }
        }

        private void DrawMemoryTrail(TraversalDungeonGraph graph)
        {
            if (_controller.Memory == null) return;

            var history = _controller.Memory.VisitHistory;
            if (history.Count < 2) return;

            Gizmos.color = memoryTrailColor;

            for (int i = 0; i < history.Count - 1; i++)
            {
                var fromNode = graph.GetNode(history[i]);
                var toNode = graph.GetNode(history[i + 1]);
                if (fromNode == null || toNode == null) continue;

                // Fade trail over time
                float alpha = (float)i / history.Count * 0.6f;
                Gizmos.color = new Color(memoryTrailColor.r, memoryTrailColor.g, memoryTrailColor.b, alpha);
                Gizmos.DrawLine(
                    fromNode.WorldPosition + Vector3.up * 0.2f,
                    toNode.WorldPosition + Vector3.up * 0.2f);
            }
        }

        private void DrawInfluenceMap()
        {
            if (_controller.InfluenceMapData == null) return;

            var layer = _controller.InfluenceMapData.GetLayer(influenceLayerToShow);
            if (layer == null) return;

            var values = layer.GetRawValues();
            int width = layer.Width;
            int height = layer.Height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float v = values[x, y];
                    if (v < 0.01f) continue;

                    Vector3 pos = layer.GridToWorld(x, y);
                    Color c = Color.Lerp(Color.blue, Color.red, v);
                    c.a = v * 0.5f;
                    Gizmos.color = c;
                    Gizmos.DrawCube(pos, Vector3.one * influenceCellScale * 0.9f);
                }
            }
        }

        private void DrawNodeScores(TraversalDungeonGraph graph)
        {
            if (_controller.GoalEval == null) return;

            var evaluation = _controller.GoalEval.LastEvaluation;
            if (evaluation == null || evaluation.Count == 0) return;

#if UNITY_EDITOR
            for (int i = 0; i < Mathf.Min(evaluation.Count, 5); i++)
            {
                var scored = evaluation[i];
                var node = graph.GetNode(scored.Goal.TargetNodeId);
                if (node == null) continue;

                float hue = Mathf.Lerp(0f, 0.33f, scored.Score);
                UnityEditor.Handles.color = Color.HSVToRGB(hue, 1f, 1f);
                UnityEditor.Handles.Label(
                    node.WorldPosition + Vector3.up * (4f + i * 0.5f),
                    $"#{i + 1} {scored.Goal.GoalType}: {scored.Score:F3}");
            }
#endif
        }

        /// <summary>Draw utility breakdown in GUI overlay.</summary>
        private void OnGUI()
        {
            if (!showUtilityBreakdown || _controller == null || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 10, 350, 400));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>{gameObject.name}</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label($"State: {_controller.StateMachine?.CurrentStateName ?? "N/A"}");
            GUILayout.Label($"Strategy: {_controller.Strategy?.StrategyName ?? "N/A"}");
            GUILayout.Label($"Goal: {_controller.CurrentGoal?.GetDescription() ?? "None"}");
            GUILayout.Label($"Node: {_controller.CurrentNodeId}");
            GUILayout.Label($"Health: {_controller.Health:P0}");
            GUILayout.Label($"Visited: {_controller.Memory?.UniqueVisits ?? 0}/{_controller.DungeonGraph?.Nodes.Count ?? 0}");
            GUILayout.Label($"Tile Pathfinder: {(_controller.HasTilePathfinder ? "Active" : "Disabled")}");
            if (_controller.TileWaypoints != null)
                GUILayout.Label($"Waypoints: {_controller.WaypointIndex}/{_controller.TileWaypoints.Count}");

            // Show top goals
            var eval = _controller.GoalEval?.LastEvaluation;
            if (eval != null && eval.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("<b>Top Goals:</b>", new GUIStyle(GUI.skin.label) { richText = true });
                for (int i = 0; i < Mathf.Min(eval.Count, 5); i++)
                {
                    GUILayout.Label($"  {i + 1}. {eval[i].Goal.GoalType} ({eval[i].Score:F3})");
                }
            }

            // Show triggered rules
            var triggered = _controller.RuleEval?.LastTriggeredRules;
            if (triggered != null && triggered.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("<b>Active Rules:</b>", new GUIStyle(GUI.skin.label) { richText = true });
                foreach (var rule in triggered)
                {
                    GUILayout.Label($"  >> {rule.ruleName}");
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}

