namespace TraversalAI.Perception
{
    /// <summary>
    /// Represents the AI's knowledge state about a dungeon element.
    /// </summary>
    public enum VisibilityState
    {
        /// <summary>Completely unknown to the AI.</summary>
        Unknown,
        /// <summary>Inferred from context (e.g., heard sounds, logical deduction).</summary>
        Inferred,
        /// <summary>Previously seen but not currently visible.</summary>
        Remembered,
        /// <summary>Currently within perception range.</summary>
        Visible
    }
}

