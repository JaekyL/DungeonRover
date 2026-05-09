# DungeonRover - Procedural Dungeon Generation Framework

## Architektur-Übersicht

```
┌──────────────────────────────────────────────────────────────────────┐
│                        DungeonGenerator (MonoBehaviour)              │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                    GenerationPipeline                         │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │   │
│  │  │  Stage 1 │→ │  Stage 2 │→ │  Stage 3 │→ │  Stage N │    │   │
│  │  │ (P=100)  │  │ (P=200)  │  │ (P=300)  │  │ (P=900)  │    │   │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │   │
│  └────────────────────────┬─────────────────────────────────────┘   │
│                           │                                         │
│  ┌────────────────────────▼─────────────────────────────────────┐   │
│  │                  GenerationContext                            │   │
│  │  • SeededRandom    • DungeonGraph    • SpatialMap            │   │
│  │  • HistoryLog      • StoryMarkers   • Decorations           │   │
│  │  • Encounters      • CustomData     • DungeonConfig (SO)    │   │
│  └──────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
```

## Generation Pipeline (9 Stages)

| # | Stage | Priority | Klasse | Beschreibung |
|---|-------|----------|--------|--------------|
| 1 | Macro Graph | 100 | `MacroGraphStage` | Abstrakter Progressionsgraph (Critical Path, Branches, Loops, Lock/Key) |
| 2 | Spatial Layout | 200 | `SpatialLayoutStage` | Konvertiert Graph in Tile-basierte Geometrie (BSP, Cellular Automata) |
| 3 | History Simulation | 300 | `HistorySimulationStage` | Agent-basierte historische Simulation (Kultisten, Überflutung, Feuer...) |
| 4 | Room Purpose | 400 | `RoomPurposeStage` | Semantische Raumidentitäten (Barracke, Schrein, Bibliothek...) |
| 5 | Traversal Validation | 500 | `TraversalValidationStage` | Konnektivitätsprüfung, Critical-Path-Validierung, Heatmap |
| 6 | Storytelling | 600 | `StorytellingStage` | Environmental Storytelling Markers aus History-Ergebnissen |
| 7 | Decoration | 700 | `DecorationStage` | Prozedurales Dekorationsplacement mit Mikrovariation |
| 8 | Encounters | 800 | `EncounterStage` | Gegner-Encounter basierend auf Semantik, Fraktion, Schwierigkeit |
| 9 | Optimization | 900 | `OptimizationStage` | Bereinigung, Density-Clamping, Statistiken |

## Ordnerstruktur

```
Assets/Scripts/DungeonGeneration/
├── Core/                          # Kern-Architektur
│   ├── IGenerationStage.cs        # Pipeline-Stage Interface
│   ├── GenerationPipeline.cs      # Pipeline-Executor
│   ├── GenerationContext.cs       # Shared Data Context
│   ├── DungeonConfig.cs           # ScriptableObject Konfiguration
│   ├── DungeonGenerator.cs        # MonoBehaviour Entry Point
│   └── SeededRandom.cs            # Deterministischer RNG
│
├── Data/                          # Datenstrukturen
│   ├── Enums.cs                   # Alle Enumerationen
│   ├── DungeonGraph.cs            # Graph (Nodes + Edges)
│   ├── SpatialMap.cs              # Tile-Map + Room/Corridor Instances
│   ├── HistoryLog.cs              # History Events
│   ├── StoryMarker.cs             # Storytelling Marker
│   ├── DecorationInstance.cs      # Deko-Instanzen
│   └── EncounterInstance.cs       # Encounter + SpawnPoints
│
├── MacroGraph/                    # Stage 1
│   └── MacroGraphStage.cs         # Graph-Generierung
│
├── SpatialLayout/                 # Stage 2
│   ├── ISpatialLayoutAlgorithm.cs # Layout-Algorithmus Interface
│   ├── BSPLayoutAlgorithm.cs      # Binary Space Partitioning
│   ├── CellularAutomataAlgorithm.cs # Zelluläre Automaten (Höhlen)
│   └── SpatialLayoutStage.cs      # Stage-Koordinator
│
├── History/                       # Stage 3
│   ├── IHistoryAgent.cs           # History-Agent Interface
│   ├── HistorySimulationStage.cs  # Simulations-Koordinator
│   └── Agents/
│       ├── CultistAgent.cs        # Kultisten (Ritualräume)
│       ├── FloodingAgent.cs       # Überflutung (Wasserschäden)
│       ├── MonsterAgent.cs        # Monster (Nester)
│       ├── FireAgent.cs           # Feuer (Brandschäden)
│       ├── InvaderAgent.cs        # Eindringlinge (Schlachtspuren)
│       └── CorruptionAgent.cs     # Korruption (Biom-Änderungen)
│
├── RoomPurpose/                   # Stage 4
│   └── RoomPurposeStage.cs
│
├── Validation/                    # Stage 5
│   └── TraversalValidationStage.cs
│
├── Storytelling/                  # Stage 6
│   └── StorytellingStage.cs       # + 7 IStoryRule Implementierungen
│
├── Decoration/                    # Stage 7
│   └── DecorationStage.cs
│
├── Encounters/                    # Stage 8
│   └── EncounterStage.cs
│
├── Optimization/                  # Stage 9
│   └── OptimizationStage.cs
│
├── Debug/                         # Debug-Visualisierung
│   └── DungeonDebugVisualizer.cs  # Gizmo-basierte Visualisierung
│
└── Editor/                        # Unity Editor Tools
    ├── DungeonGeneratorEditor.cs  # Custom Inspector
    └── DungeonDebugWindow.cs      # EditorWindow mit Tab-Navigation
```

## Schnellstart

### 1. DungeonConfig erstellen
- Rechtsklick im Project → `Create > Dungeon Generation > Dungeon Config`
- Konfiguriere Dungeon-Größe, Raumanzahl, Algorithmus, History-Agenten etc.

### 2. Generator einrichten
- Erstelle ein leeres GameObject in der Szene
- Füge `DungeonGenerator` Component hinzu
- Weise die DungeonConfig zu
- Optional: Füge `DungeonDebugVisualizer` für Gizmo-Visualisierung hinzu

### 3. Generieren
- Im Inspector: "Generate" Button klicken
- Oder: `ContextMenu > Generate Dungeon`
- Oder per Code: `generator.Generate()` / `generator.Generate(seed)`

### 4. Debug
- Gizmos im Scene View zeigen Tilemap, Graph, Räume, Marker, Encounters
- `Window > Dungeon Generation > Debug Window` für detaillierte Inspektion
- Seed-Replay über Inspector oder `generator.ReplayLastSeed()`

## Erweiterbarkeit

### Neuen Pipeline-Stage hinzufügen
```csharp
public class MyCustomStage : IGenerationStage
{
    public string StageName => "Custom Stage";
    public int Priority => 550; // Zwischen Validation und Storytelling
    
    public void Execute(GenerationContext context)
    {
        // Zugriff auf alle bisherigen Daten via context
    }
}

// Registrieren:
generator.AddStage(new MyCustomStage());
```

### Neuen Layout-Algorithmus hinzufügen
```csharp
public class WFCLayoutAlgorithm : ISpatialLayoutAlgorithm
{
    public string AlgorithmName => "Wave Function Collapse";
    public void Generate(SpatialMap map, DungeonGraph graph, DungeonConfig config, SeededRandom rng)
    {
        // WFC Implementation
    }
}
```

### Neuen History-Agent hinzufügen
```csharp
public class ScavengerAgent : IHistoryAgent
{
    public string AgentName => "Scavengers";
    public int Priority => 60;
    public void Initialize(GenerationContext context, float intensity, SeededRandom rng) { }
    public void SimulateStep(int step, GenerationContext context, SeededRandom rng) { }
}
```

### Neue Storytelling-Regel hinzufügen
```csharp
public class PlagueRule : IStoryRule
{
    public void Apply(RoomInstance room, List<HistoryEvent> events, 
                      GenerationContext context, SeededRandom rng) { }
}
```

## Narrative Simulation Framework

Das Dungeon-Generation-System wurde um ein narrativ-gesteuertes historisches Simulationsframework erweitert.

Siehe: `Narrative/README.md` für vollständige Dokumentation.

### Zusätzliche Pipeline-Stages (Narrative)

| # | Stage | Priority | Klasse | Beschreibung |
|---|-------|----------|--------|--------------|
| - | Config Injection | 1 | `NarrativeConfigInjector` | Injiziert NarrativeConfig in den Kontext |
| - | Semantic Extraction | 250 | `SemanticExtractionStage` | Extrahiert Narrative-Daten für Simulation |
| - | Narrative Simulation | 305 | `NarrativeSimulationStage` | Constrained historische Simulation |
| - | Spatial Interpretation | 450 | `SpatialInterpretationStage` | Konvertiert Sim-State in räumliche Marker |
| - | Env. Storytelling | 605 | `EnvironmentalStorytellingStage` | Szenen-basiertes Environmental Storytelling |
| - | Prop Mutation | 705 | `PropMutationStage` | Mutiert Dekorationen basierend auf Narrative |
| - | Narrative Validation | 855 | `NarrativeValidationStage` | Readability-Validierung |

### Schnellstart (Narrative)

1. `NarrativeGenerator` Component zum DungeonGenerator hinzufügen
2. `NarrativeConfig` ScriptableObject erstellen und konfigurieren
3. Faktionen, Charaktere und Events definieren
4. "Generate with Narrative" Button klicken

## Design-Prinzipien

- **Data-Driven**: ScriptableObjects für Konfiguration
- **Deterministic**: Seeded RNG mit Fork() für reproduzierbare Ergebnisse
- **Layered Pipeline**: Stages laufen unabhängig, kommunizieren über GenerationContext
- **Open/Closed**: Neue Features über Interfaces hinzufügen, ohne bestehenden Code zu ändern
- **2D/3D agnostisch**: Datenstrukturen sind Tile-basiert, Rendering ist separat
- **Authored Emergence**: Narrative-Simulation ist geführt, nicht chaotisch
- **Environmental Storytelling**: Jedes Prop existiert, weil jemand es benutzt hat

