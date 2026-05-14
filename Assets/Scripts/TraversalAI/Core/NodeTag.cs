using System;

namespace TraversalAI.Core
{
    /// <summary>
    /// Tags that can be applied to dungeon nodes for AI decision-making.
    /// Flags enum allows combining multiple tags per node.
    /// </summary>
    [Flags]
    public enum NodeTag
    {
        None = 0,
        Unexplored = 1 << 0,
        Explored = 1 << 1,
        Dangerous = 1 << 2,
        Loot = 1 << 3,
        EnemyPresence = 1 << 4,
        Staircase = 1 << 5,
        SafeZone = 1 << 6,
        Shortcut = 1 << 7,
        DeadEnd = 1 << 8,
        Hidden = 1 << 9,
        Locked = 1 << 10,
        Intersection = 1 << 11,
        Corridor = 1 << 12,
        HubRoom = 1 << 13,
        BossArea = 1 << 14,
        Trapped = 1 << 15
    }
}

