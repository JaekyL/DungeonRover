#if UNITY_EDITOR
using DungeonGeneration.Narrative.Debug;
using UnityEditor;
using UnityEngine;

namespace DungeonGeneration.Narrative.Editor
{
    [CustomEditor(typeof(NarrativeGenerator))]
    public class NarrativeGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var generator = (NarrativeGenerator)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Narrative Controls", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate with Narrative", GUILayout.Height(30)))
                {
                    generator.GenerateWithNarrative();
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Replay with Narrative", GUILayout.Height(30)))
                {
                    generator.ReplayWithNarrative();
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Narrative Debug", GUILayout.Height(25)))
                {
                    NarrativeDebugWindow.ShowWindow();
                }

                if (GUILayout.Button("Add Debug Visualizer", GUILayout.Height(25)))
                {
                    if (generator.GetComponent<NarrativeDebugVisualizer>() == null)
                        generator.gameObject.AddComponent<NarrativeDebugVisualizer>();
                }
            }

            // Show narrative config summary
            if (generator.NarrativeConfig != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Scenario Summary", EditorStyles.boldLabel);

                var config = generator.NarrativeConfig;
                EditorGUILayout.LabelField($"Name: {config.scenarioName}");
                EditorGUILayout.LabelField($"Factions: {config.factions.Count}");
                EditorGUILayout.LabelField($"Characters: {config.characters.Count}");
                EditorGUILayout.LabelField($"Events: {config.timelineEvents.Count}");
                EditorGUILayout.LabelField($"Constraints: {config.constraints.Count}");
                EditorGUILayout.LabelField($"Simulation Steps: {config.simulationSteps}");

                // Quick faction overview
                if (config.factions.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("Factions:", EditorStyles.miniLabel);
                    foreach (var faction in config.factions)
                    {
                        if (faction == null) continue;
                        EditorGUILayout.LabelField($"  • {faction.displayName} ({faction.factionId})" +
                                                   $" | Aggression={faction.behaviorProfile.aggression:F1}" +
                                                   $" | Territory={faction.preferredTerritorySize}");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Assign a NarrativeConfig ScriptableObject to enable narrative simulation.\n" +
                    "Create one via: Create > Dungeon Generation > Narrative > Narrative Config",
                    MessageType.Info);
            }
        }
    }
}
#endif

