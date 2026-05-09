#if UNITY_EDITOR
using DungeonGeneration.Core;
using UnityEditor;
using UnityEngine;

namespace DungeonGeneration.Editor
{
    [CustomEditor(typeof(DungeonGenerator))]
    public class DungeonGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var generator = (DungeonGenerator)target;
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Generation Controls", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate", GUILayout.Height(30)))
                {
                    generator.Generate();
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Replay Seed", GUILayout.Height(30)))
                {
                    generator.ReplayLastSeed();
                    SceneView.RepaintAll();
                }
            }
            if (generator.LastContext != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Last Generation Info", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Seed: {generator.LastSeed}");
                var ctx = generator.LastContext;
                if (ctx.SpatialMap != null)
                {
                    EditorGUILayout.LabelField($"Rooms: {ctx.SpatialMap.Rooms.Count}");
                    EditorGUILayout.LabelField($"Corridors: {ctx.SpatialMap.Corridors.Count}");
                    EditorGUILayout.LabelField($"Doors: {ctx.SpatialMap.Doors.Count}");
                }
                EditorGUILayout.LabelField($"Story Markers: {ctx.StoryMarkers.Count}");
                EditorGUILayout.LabelField($"Decorations: {ctx.Decorations.Count}");
                EditorGUILayout.LabelField($"Encounters: {ctx.Encounters.Count}");
                if (ctx.HistoryLog != null)
                    EditorGUILayout.LabelField($"History Events: {ctx.HistoryLog.Events.Count}");
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Open Debug Window"))
                {
                    DungeonDebugWindow.ShowWindow();
                }

                // Render controls
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);

                var prefabRenderer = generator.GetComponent<Rendering.DungeonRenderer>();
                var proceduralRenderer = generator.GetComponent<Rendering.DungeonProceduralRenderer>();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (prefabRenderer != null)
                    {
                        if (GUILayout.Button("Re-Render (Prefab)", GUILayout.Height(25)))
                        {
                            prefabRenderer.Render(ctx);
                            SceneView.RepaintAll();
                        }
                    }
                    if (proceduralRenderer != null)
                    {
                        if (GUILayout.Button("Re-Render (Procedural)", GUILayout.Height(25)))
                        {
                            proceduralRenderer.Render(ctx);
                            SceneView.RepaintAll();
                        }
                    }
                }

                if (GUILayout.Button("Clear Rendered Dungeon", GUILayout.Height(22)))
                {
                    if (prefabRenderer != null) prefabRenderer.Clear();
                    if (proceduralRenderer != null) proceduralRenderer.Clear();
                    SceneView.RepaintAll();
                }

                if (prefabRenderer == null && proceduralRenderer == null)
                {
                    EditorGUILayout.HelpBox(
                        "No renderer attached. Add DungeonProceduralRenderer for instant visuals, " +
                        "or DungeonRenderer + DungeonRenderConfig for prefab-based rendering.",
                        MessageType.Info);

                    if (GUILayout.Button("Add Procedural Renderer"))
                    {
                        generator.gameObject.AddComponent<Rendering.DungeonProceduralRenderer>();
                    }
                }
            }
        }
    }
}
#endif
