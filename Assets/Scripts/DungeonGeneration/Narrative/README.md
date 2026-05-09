# Narrative Simulation Framework

## Architektur-Übersicht

Erweitert die bestehende Dungeon-Generation-Pipeline um narrative-gesteuerte historische Simulation, die **auf den generierten Dungeons operiert** und sie durch Environmental Storytelling anreichert.

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                              GENERATION PIPELINE                                 │
│                                                                                  │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────────┐  │
│  │ MacroGr. │ → │ Spatial  │ → │ Semantic │ → │ Narrative│ → │ Room Purpose │  │
│  │  (100)   │   │  (200)   │   │  Extract │   │   Sim    │   │    (400)     │  │
│  └──────────┘   └──────────┘   │  (250)   │   │  (305)   │   └──────────────┘  │
│                                └──────────┘   └──────────┘                      │
│                                                                                  │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────────┐  │
│  │ Spatial  │ → │ Traversal│ → │  Story   │ → │ Env.Story│ → │ Decoration   │  │
│  │ Interp.  │   │ Validat. │   │ telling  │   │ telling  │   │   (700)      │  │
│  │  (450)   │   │  (500)   │   │  (600)   │   │  (605)   │   └──────────────┘  │
│  └──────────┘   └──────────┘   └──────────┘   └──────────┘                      │
│                                                                                  │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐                      │
│  │ Prop     │ → │Encounters│ → │ Narr.    │ → │ Optimiz. │                      │
│  │ Mutation │   │  (800)   │   │ Valid.   │   │  (900)   │                      │
│  │  (705)   │   └──────────┘   │  (855)   │   └──────────┘                      │
│  └──────────┘                  └──────────┘                                      │
│                                                                                  │
│  ████████ = NEUE Narrative Stages                                                │
│  ░░░░░░░░ = Bestehende Stages                                                   │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## 8-Schichten-Architektur

```
┌─────────────────────────────────────────────────────────┐
│  Layer 8: Validation & Readability        (Priority 855)│
│  ├─ IReadabilityRule Implementierungen                  │
│  ├─ Auto-Fix für kritische Probleme                     │
│  └─ Readability Scoring pro Raum                        │
├─────────────────────────────────────────────────────────┤
│  Layer 7: Prop/Decoration Mutation        (Priority 705)│
│  ├─ IPropMutator Implementierungen                      │
│  ├─ Damage, Water, Fire, Corruption, Aging, Looting     │
│  └─ Layered Storytelling auf existierenden Props        │
├─────────────────────────────────────────────────────────┤
│  Layer 6: Environmental Storytelling      (Priority 605)│
│  ├─ IStoryScene Implementierungen                       │
│  ├─ Battle, Camp, Ritual, Barricade, Escape, Decay      │
│  └─ Coherente Storytelling-Szenen pro Raum              │
├─────────────────────────────────────────────────────────┤
│  Layer 5: Spatial Interpretation          (Priority 450)│
│  ├─ ISpatialInterpreter Implementierungen               │
│  ├─ Conflict, Abandonment, Occupation, Ritual           │
│  ├─ Collapse, Corruption, Flood, Desperation            │
│  └─ Konvertiert abstrakte Zustände in räumliche Marker  │
├─────────────────────────────────────────────────────────┤
│  Layer 4: World State                     (persistent)  │
│  ├─ NarrativeWorldState                                 │
│  ├─ RoomNarrativeState, FactionState, CharacterState    │
│  ├─ TerritoryMap, WorldTimeline                         │
│  └─ Queryable History per Room/Faction/Event            │
├─────────────────────────────────────────────────────────┤
│  Layer 3: Historical Simulation           (Priority 305)│
│  ├─ INarrativeAgent / FactionAgent                      │
│  ├─ Constrained Simulation mit Protected Outcomes       │
│  ├─ Cascading Events, Desperation Behaviors             │
│  └─ Bridge zu bestehendem HistoryLog                    │
├─────────────────────────────────────────────────────────┤
│  Layer 2: Semantic Extraction             (Priority 250)│
│  ├─ Konvertiert authored Narrative in Simulation-Daten  │
│  ├─ Initialisiert Factions, Characters, Resources       │
│  └─ Verteilt initiale Territorien                       │
├─────────────────────────────────────────────────────────┤
│  Layer 1: Narrative Authoring         (ScriptableObjects│
│  ├─ NarrativeConfig (Master-Konfiguration)              │
│  ├─ FactionDefinition (Fraktionen, Motivationen)        │
│  ├─ CharacterArchetype (Charaktere, Rollen)             │
│  └─ NarrativeEventDefinition (Timeline-Events)          │
└─────────────────────────────────────────────────────────┘
```

## Ordnerstruktur

```
Assets/Scripts/DungeonGeneration/Narrative/
├── NarrativeGenerator.cs                    # Integration Component (MonoBehaviour)
│
├── Authoring/                               # Layer 1: ScriptableObject Authoring
│   ├── NarrativeConfig.cs                   # Master Narrative Config
│   ├── FactionDefinition.cs                 # Faction Definition
│   ├── CharacterArchetype.cs                # Character Archetype
│   └── NarrativeEventDefinition.cs          # Timeline Events
│
├── Semantics/                               # Layer 2: Semantic Extraction
│   └── SemanticExtractionStage.cs           # Extraction Pipeline Stage
│
├── Simulation/                              # Layer 3: Historical Simulation
│   ├── INarrativeAgent.cs                   # Agent Interface
│   ├── FactionAgent.cs                      # Generic Faction Agent
│   └── NarrativeSimulationStage.cs          # Simulation Pipeline Stage
│
├── WorldState/                              # Layer 4: Persistent World State
│   └── NarrativeWorldState.cs               # All World State Data
│       ├── NarrativeWorldState
│       ├── RoomNarrativeState
│       ├── FactionState
│       ├── CharacterState
│       ├── TerritoryMap
│       └── WorldTimeline
│
├── Interpretation/                          # Layer 5: Spatial Interpretation
│   ├── ISpatialInterpreter.cs               # Interpreter Interface + Data
│   ├── SpatialInterpretationStage.cs        # Interpretation Pipeline Stage
│   └── Interpreters/
│       └── SpatialInterpreters.cs           # All Interpreter Implementations
│           ├── ConflictInterpreter
│           ├── AbandonmentInterpreter
│           ├── OccupationInterpreter
│           ├── RitualInterpreter
│           ├── CollapseInterpreter
│           ├── CorruptionInterpreter
│           ├── FloodInterpreter
│           └── DesperationInterpreter
│
├── EnvironmentalStorytelling/               # Layer 6: Environmental Storytelling
│   └── EnvironmentalStorytellingStage.cs    # Storytelling Pipeline Stage
│       ├── BattleScene
│       ├── AbandonedCampScene
│       ├── RitualScene
│       ├── BarricadeScene
│       ├── EscapeScene
│       ├── DecayScene
│       ├── DefensivePositionScene
│       └── LootingEvidenceScene
│
├── PropMutation/                            # Layer 7: Prop Mutation
│   └── PropMutationStage.cs                 # Mutation Pipeline Stage
│       ├── DamageMutator
│       ├── WaterDamageMutator
│       ├── FireDamageMutator
│       ├── CorruptionMutator
│       ├── AgingMutator
│       └── LootingMutator
│
├── Validation/                              # Layer 8: Validation & Readability
│   └── NarrativeValidationStage.cs          # Validation Pipeline Stage
│       ├── MarkerDensityRule
│       ├── ContradictoryStatesRule
│       ├── CriticalPathBlockedRule
│       ├── EmptyStorytellingRule
│       ├── OvercrowdedRoomRule
│       └── OrphanedEvidenceRule
│
├── Debug/                                   # Debug Visualization
│   └── NarrativeDebugVisualizer.cs          # Gizmo-based Scene View Visualizer
│
└── Editor/                                  # Unity Editor Tools
    ├── NarrativeDebugWindow.cs              # EditorWindow (7 Tabs)
    └── NarrativeGeneratorEditor.cs          # Custom Inspector
```

## Schnellstart

### 1. Narrative Assets erstellen

Rechtsklick im Project-Fenster:

- `Create > Dungeon Generation > Narrative > Narrative Config`
- `Create > Dungeon Generation > Narrative > Faction Definition`
- `Create > Dungeon Generation > Narrative > Character Archetype`
- `Create > Dungeon Generation > Narrative > Narrative Event`

### 2. Faktionen konfigurieren

Beispiel: Drei-Fraktionen-Szenario

**Cult of the Deep (Faction)**
- Motivationen: Awakening (0.9), Faith (0.7), Power (0.5)
- Aggression: 0.3 | Fanaticism: 0.8 | Rationality: 0.3
- Preferred Territory: 3 Räume
- Desperation: PerformRitual bei 0.6

**Miners Guild (Faction)**
- Motivationen: Survival (0.9), Wealth (0.6)
- Aggression: 0.2 | Rationality: 0.8 | Adaptability: 0.7
- Preferred Territory: 4 Räume
- Desperation: Flee bei 0.7, Barricade bei 0.5

**Church Wardens (Faction)**
- Motivationen: Containment (0.9), Protection (0.8)
- Aggression: 0.5 | Defensiveness: 0.8 | Rationality: 0.7
- Preferred Territory: 3 Räume
- Desperation: LastStand bei 0.8

### 3. Timeline Events erstellen

```
Step 0:   Miners establish mining camp
Step 3:   Cult discovers ancient artifact
Step 5:   Church sends wardens to contain threat
Step 8:   Cave-in disaster (Conditional: Miners active)
Step 10:  Cult begins awakening ritual
Step 12:  Church-Cult conflict escalates
Step 15:  Containment failure → corruption spreads
Step 18:  Final battle
```

### 4. Generator einrichten

- Add `NarrativeGenerator` Component zum DungeonGenerator GameObject
- Weise `NarrativeConfig` zu
- "Generate with Narrative" Button klicken

### 5. Debuggen

- `Window > Dungeon Generation > Narrative Debug`
- 7 Tabs: Overview, Factions, Territory, Timeline, Room History, Readability, Interpretation
- Add `NarrativeDebugVisualizer` für Gizmo-Visualisierung im Scene View

## Beispiel: Raum-Evolution

Ein einzelner Raum kann durch die Simulation mehrere narrative Schichten erhalten:

```
Step  0: Miners claim room → "mining_camp"
         → bedrolls, tools, supply crates

Step  3: Miners expand operations
         → additional mining equipment

Step  7: Cave-in damages room → StructuralDamage = 0.4
         → rubble, collapsed walls, warning signs

Step  9: Miners abandon room → "abandoned_in_haste"
         → dropped tools, scattered belongings

Step 12: Cult occupies room → "ritual_site"
         → ritual markings, candles, altar over old camp

Step 15: Corruption spreads → Corruption = 0.6
         → fungal growth, discolored walls, corrupted tiles

Step 18: Church attacks → "battle_site"
         → skeletons at entries, blood trails, weapon scatter

FINAL ROOM STATE:
- Semantic Label: "warzone"
- Ownership History: Miners → abandoned → Cult → Church
- Environmental Layers:
  1. Old mining equipment (decayed)
  2. Collapsed ceiling section
  3. Abandoned belongings
  4. Ritual markings under rubble
  5. Corruption growth on walls
  6. Battle debris at entrances
```

## Design-Prinzipien

| Prinzip | Umsetzung |
|---------|-----------|
| **Authorial Control** | ScriptableObjects für alle Narrative-Daten, Protected Outcomes |
| **Readability** | Validation Stage verhindert inkoherentes Storytelling |
| **Semantic Coherence** | SemanticTags verbinden Simulation → Interpretation → Props |
| **Layered Generation** | 8 unabhängige Layers, jeder konsumiert und produziert Daten |
| **Deterministic** | SeededRandom.Fork() für reproduzierbare Ergebnisse |
| **Guided Emergence** | Constraints + Protected Outcomes verhindern Chaos |
| **Extensibility** | Interfaces für jeden Layer (ISpatialInterpreter, IStoryScene, etc.) |
| **Backward Compatibility** | BridgeToHistoryLog() integriert mit bestehenden Stages |

## Erweiterbarkeit

### Neuen Spatial Interpreter hinzufügen
```csharp
public class PlagueInterpreter : ISpatialInterpreter
{
    public string InterpreterName => "Plague";
    public int Priority => 65;
    public bool CanInterpret(RoomNarrativeState s, RoomInstance r) =>
        s.SemanticTags.Contains("plague");
    public void Interpret(RoomNarrativeState s, RoomInstance r,
        GenerationContext ctx, InterpretationResult result, SeededRandom rng)
    {
        // Generate plague evidence...
    }
}
```

### Neue Story Scene hinzufügen
```csharp
public class SacrificeScene : IStoryScene
{
    public string SceneName => "Sacrifice";
    public bool CanGenerate(RoomNarrativeState s, RoomInstance r) =>
        s.SemanticTags.Contains("sacrifice");
    public void Generate(RoomNarrativeState s, RoomInstance r,
        GenerationContext ctx, SeededRandom rng)
    {
        // Generate sacrifice scene...
    }
}
```

### Neuen Prop Mutator hinzufügen
```csharp
public class FrostMutator : IPropMutator
{
    public string MutatorName => "Frost";
    public int Priority => 70;
    public bool CanMutate(DecorationInstance d, RoomNarrativeState s) =>
        s.SemanticTags.Contains("frozen");
    public void Mutate(DecorationInstance d, RoomNarrativeState s, SeededRandom rng)
    {
        d.DecorationId += "_frozen";
    }
}
```

### Neue Readability Rule hinzufügen
```csharp
public class TooManyFactionsRule : IReadabilityRule
{
    public string RuleName => "TooManyFactions";
    public ReadabilityIssue Validate(int roomId, ...) {
        // Check if too many factions left evidence in same room
    }
}
```

