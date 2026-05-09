namespace DungeonGeneration.Data
{
    public enum RoomType
    {
        Normal, Start, Boss, Treasure, Hub, DeadEnd, Secret, Transition, MiniBoss
    }
    public enum EdgeType
    {
        Normal, Locked, Secret, OneWay, Shortcut, Vertical
    }
    public enum RoomPurposeType
    {
        None, Barracks, Shrine, Library, Prison, Treasury, Armory,
        RitualChamber, FloodedArchive, AbandonedCamp, Nest, Corridor,
        Entrance, ThroneRoom, Kitchen, StorageRoom, Workshop, Garden, Crypt
    }
    public enum TileType
    {
        Wall, Floor, Corridor, Door, SecretDoor, StairsUp, StairsDown, Water, Pit, Rubble
    }
    public enum DoorType
    {
        Normal, Locked, Secret, Barricaded, Broken, Boss
    }
    public enum StoryMarkerType
    {
        BloodTrail, BrokenDoor, BurnMarks, Skeleton, Barricade, Note,
        LootRemains, Decay, WaterDamage, CollapsedWall, RitualMarking,
        Graffiti, Campfire, WeaponScatter, FungalGrowth
    }
    public enum EncounterType
    {
        Patrol, Ambush, Guard, Boss, MiniBoss, Nest, FactionConflict, Trap, Environmental
    }
    public enum LayoutAlgorithm
    {
        BSP, CellularAutomata, CorridorFirst, ModularRoomPlacement, Hybrid
    }
    public enum HistoryAgentType
    {
        Builders, Miners, Cultists, Invaders, Corruption, Flooding, Fire, Scavengers, Monsters
    }
}
