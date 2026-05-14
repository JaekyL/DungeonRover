using TraversalAI.Configuration;
using UnityEditor;
using UnityEngine;

namespace TraversalAI.Editor
{
    /// <summary>
    /// Custom inspector for the TraversalAIController MonoBehaviour.
    /// Provides runtime debug controls and visualization toggles.
    /// </summary>
    [CustomEditor(typeof(TraversalAIController))]
    public class TraversalAIControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var controller = (TraversalAIController)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Runtime controls available in Play mode.", MessageType.Info);
                return;
            }

            // Strategy selector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Change Strategy:", GUILayout.Width(120));

            foreach (TraversalStrategyType strategy in System.Enum.GetValues(typeof(TraversalStrategyType)))
            {
                if (GUILayout.Button(strategy.ToString(), EditorStyles.miniButton))
                {
                    controller.SetStrategy(strategy);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Health slider
            EditorGUILayout.Space();
            controller.Health = EditorGUILayout.Slider("Health", controller.Health, 0f, 1f);

            // Status display
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Current State: {controller.StateMachine?.CurrentStateName ?? "N/A"}");
            EditorGUILayout.LabelField($"Current Strategy: {controller.Strategy?.StrategyName ?? "N/A"}");
            EditorGUILayout.LabelField($"Current Goal: {controller.CurrentGoal?.GetDescription() ?? "None"}");
            EditorGUILayout.LabelField($"Current Node: {controller.CurrentNodeId}");
            EditorGUILayout.LabelField($"Visited Nodes: {controller.Memory?.UniqueVisits ?? 0}");
            EditorGUILayout.LabelField($"Total Moves: {controller.Memory?.TotalVisits ?? 0}");

            // Goal evaluation breakdown
            var eval = controller.GoalEval?.LastEvaluation;
            if (eval != null && eval.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Goal Evaluation", EditorStyles.boldLabel);
                for (int i = 0; i < Mathf.Min(eval.Count, 8); i++)
                {
                    EditorGUILayout.LabelField(
                        $"  {i + 1}. {eval[i].Goal.GoalType} -> {eval[i].Goal.TargetNodeId}",
                        $"Score: {eval[i].Score:F4}");
                }
            }

            // Triggered rules
            var rules = controller.RuleEval?.LastTriggeredRules;
            if (rules != null && rules.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Triggered Rules", EditorStyles.boldLabel);
                foreach (var rule in rules)
                {
                    EditorGUILayout.LabelField($"  >> {rule.ruleName}: {rule.action}");
                }
            }

            // Force repaint for live updates
            Repaint();
        }
    }
}

