using TraversalAI.Configuration;
using UnityEditor;
using UnityEngine;

namespace TraversalAI.Editor
{
    /// <summary>
    /// Custom inspector for TraversalProfile ScriptableObjects.
    /// Provides a rich editing experience for AI behavior configuration.
    /// </summary>
    [CustomEditor(typeof(TraversalProfile))]
    public class TraversalProfileEditor : UnityEditor.Editor
    {
        private bool _showBehaviorRules = true;
        private bool _showUtilityWeights = true;
        private bool _showConsiderations = true;

        public override void OnInspectorGUI()
        {
            var profile = (TraversalProfile)target;

            // Header
            EditorGUILayout.LabelField("Traversal AI Profile", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Identity
            profile.profileName = EditorGUILayout.TextField("Profile Name", profile.profileName);
            profile.description = EditorGUILayout.TextArea(profile.description, GUILayout.Height(40));

            EditorGUILayout.Space();

            // Stance with apply button
            EditorGUILayout.BeginHorizontal();
            profile.stance = (ExplorationStance)EditorGUILayout.EnumPopup("Exploration Stance", profile.stance);
            if (GUILayout.Button("Apply Preset", GUILayout.Width(100)))
            {
                Undo.RecordObject(profile, "Apply Stance Preset");
                profile.ApplyStanceModifiers();
                EditorUtility.SetDirty(profile);
            }
            EditorGUILayout.EndHorizontal();

            // Strategy
            profile.primaryStrategy = (TraversalStrategyType)EditorGUILayout.EnumPopup("Primary Strategy", profile.primaryStrategy);

            EditorGUILayout.Space();

            // Utility Weights
            _showUtilityWeights = EditorGUILayout.Foldout(_showUtilityWeights, "Utility Weights", true);
            if (_showUtilityWeights)
            {
                EditorGUI.indentLevel++;
                profile.explorationWeight = EditorGUILayout.Slider("Exploration", profile.explorationWeight, 0f, 2f);
                profile.dangerAvoidanceWeight = EditorGUILayout.Slider("Danger Avoidance", profile.dangerAvoidanceWeight, 0f, 2f);
                profile.lootWeight = EditorGUILayout.Slider("Loot", profile.lootWeight, 0f, 2f);
                profile.progressionWeight = EditorGUILayout.Slider("Progression", profile.progressionWeight, 0f, 2f);
                profile.safetyWeight = EditorGUILayout.Slider("Safety", profile.safetyWeight, 0f, 2f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Danger & Risk
            profile.dangerTolerance = EditorGUILayout.Slider("Danger Tolerance", profile.dangerTolerance, 0f, 1f);
            profile.retreatHealthThreshold = EditorGUILayout.Slider("Retreat Health Threshold", profile.retreatHealthThreshold, 0f, 1f);

            // Perception
            profile.perceptionDepth = EditorGUILayout.IntSlider("Perception Depth", profile.perceptionDepth, 1, 5);
            profile.canDetectSecrets = EditorGUILayout.Toggle("Can Detect Secrets", profile.canDetectSecrets);

            EditorGUILayout.Space();

            // Behavior Rules
            _showBehaviorRules = EditorGUILayout.Foldout(_showBehaviorRules, $"Behavior Rules ({profile.behaviorRules.Count})", true);
            if (_showBehaviorRules)
            {
                EditorGUI.indentLevel++;
                DrawBehaviorRules(profile);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Considerations
            _showConsiderations = EditorGUILayout.Foldout(_showConsiderations, $"Utility Considerations ({profile.considerations.Count})", true);
            if (_showConsiderations)
            {
                EditorGUI.indentLevel++;
                var prop = serializedObject.FindProperty("considerations");
                EditorGUILayout.PropertyField(prop, true);
                EditorGUI.indentLevel--;
            }

            // Preset buttons
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Diver")) ApplyPreset(profile, TraversalPresets.CreateDiver());
            if (GUILayout.Button("Hunter")) ApplyPreset(profile, TraversalPresets.CreateHunter());
            if (GUILayout.Button("Scavenger")) ApplyPreset(profile, TraversalPresets.CreateScavenger());
            if (GUILayout.Button("Cartographer")) ApplyPreset(profile, TraversalPresets.CreateCartographer());
            if (GUILayout.Button("Ghost")) ApplyPreset(profile, TraversalPresets.CreateGhost());

            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(profile);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBehaviorRules(TraversalProfile profile)
        {
            for (int i = 0; i < profile.behaviorRules.Count; i++)
            {
                var rule = profile.behaviorRules[i];
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                rule.enabled = EditorGUILayout.Toggle(rule.enabled, GUILayout.Width(20));
                rule.ruleName = EditorGUILayout.TextField(rule.ruleName);
                rule.order = EditorGUILayout.IntField(rule.order, GUILayout.Width(30));

                // Move up/down buttons
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(25)))
                {
                    Undo.RecordObject(profile, "Move Rule Up");
                    var temp = profile.behaviorRules[i - 1];
                    profile.behaviorRules[i - 1] = profile.behaviorRules[i];
                    profile.behaviorRules[i] = temp;
                }
                GUI.enabled = i < profile.behaviorRules.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(25)))
                {
                    Undo.RecordObject(profile, "Move Rule Down");
                    var temp = profile.behaviorRules[i + 1];
                    profile.behaviorRules[i + 1] = profile.behaviorRules[i];
                    profile.behaviorRules[i] = temp;
                }
                GUI.enabled = true;

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(profile, "Remove Rule");
                    profile.behaviorRules.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                // Conditions
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Conditions:", EditorStyles.miniLabel);
                for (int j = 0; j < rule.conditions.Count; j++)
                {
                    EditorGUILayout.BeginHorizontal();
                    rule.conditions[j].parameter = (BehaviorRules.ConditionParameter)
                        EditorGUILayout.EnumPopup(rule.conditions[j].parameter);
                    rule.conditions[j].comparison = (BehaviorRules.ComparisonOperator)
                        EditorGUILayout.EnumPopup(rule.conditions[j].comparison, GUILayout.Width(100));
                    rule.conditions[j].threshold = EditorGUILayout.Slider(
                        rule.conditions[j].threshold, 0f, 1f);

                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        rule.conditions.RemoveAt(j);
                        j--;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("+ Add Condition", EditorStyles.miniButton))
                {
                    rule.conditions.Add(new BehaviorRules.Condition());
                }

                // Action
                EditorGUILayout.LabelField("Action:", EditorStyles.miniLabel);
                rule.action.type = (BehaviorRules.DirectiveType)
                    EditorGUILayout.EnumPopup("Type", rule.action.type);
                rule.action.targetGoalType = EditorGUILayout.TextField("Target Goal", rule.action.targetGoalType);
                rule.action.value = EditorGUILayout.FloatField("Value", rule.action.value);

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add Behavior Rule"))
            {
                Undo.RecordObject(profile, "Add Rule");
                profile.behaviorRules.Add(new BehaviorRules.BehaviorRule
                {
                    order = profile.behaviorRules.Count
                });
            }
        }

        private void ApplyPreset(TraversalProfile target, TraversalProfile preset)
        {
            Undo.RecordObject(target, "Apply Preset");
            target.profileName = preset.profileName;
            target.description = preset.description;
            target.stance = preset.stance;
            target.primaryStrategy = preset.primaryStrategy;
            target.explorationWeight = preset.explorationWeight;
            target.dangerAvoidanceWeight = preset.dangerAvoidanceWeight;
            target.lootWeight = preset.lootWeight;
            target.progressionWeight = preset.progressionWeight;
            target.safetyWeight = preset.safetyWeight;
            target.dangerTolerance = preset.dangerTolerance;
            target.behaviorRules = preset.behaviorRules;
            EditorUtility.SetDirty(target);
            DestroyImmediate(preset);
        }
    }
}

