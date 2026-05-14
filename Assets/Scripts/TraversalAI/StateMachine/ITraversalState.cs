namespace TraversalAI.StateMachine
{
    /// <summary>
    /// Interface for AI traversal states. States orchestrate subsystems
    /// but do not contain traversal logic themselves.
    /// </summary>
    public interface ITraversalState
    {
        string StateName { get; }
        void Enter(TraversalStateContext context);
        void Update(TraversalStateContext context);
        void Exit(TraversalStateContext context);

        /// <summary>Check if this state should transition to another state.</summary>
        TraversalStateType? CheckTransition(TraversalStateContext context);
    }

    public enum TraversalStateType
    {
        Exploring,
        Searching,
        Descending,
        Retreating,
        AvoidingThreat,
        Resting,
        Regrouping
    }
}

