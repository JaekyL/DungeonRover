using System.Collections.Generic;
using UnityEngine;

namespace TraversalAI.StateMachine
{
    /// <summary>
    /// Manages AI state transitions. States orchestrate subsystems.
    /// </summary>
    public class TraversalStateMachine
    {
        private Dictionary<TraversalStateType, ITraversalState> _states
            = new Dictionary<TraversalStateType, ITraversalState>();

        private ITraversalState _currentState;
        private TraversalStateType _currentStateType;
        private TraversalStateContext _context;

        public TraversalStateType CurrentStateType => _currentStateType;
        public string CurrentStateName => _currentState?.StateName ?? "None";

        public TraversalStateMachine(TraversalStateContext context)
        {
            _context = context;
        }

        public void RegisterState(TraversalStateType type, ITraversalState state)
        {
            _states[type] = state;
        }

        public void SetInitialState(TraversalStateType type)
        {
            if (_states.TryGetValue(type, out var state))
            {
                _currentState = state;
                _currentStateType = type;
                _currentState.Enter(_context);
            }
        }

        public void Update()
        {
            if (_currentState == null) return;

            // Check for transitions
            var transition = _currentState.CheckTransition(_context);
            if (transition.HasValue && transition.Value != _currentStateType)
            {
                TransitionTo(transition.Value);
            }

            _currentState.Update(_context);
        }

        public void TransitionTo(TraversalStateType newState)
        {
            if (!_states.TryGetValue(newState, out var state))
            {
                UnityEngine.Debug.LogWarning($"[TraversalStateMachine] State {newState} not registered.");
                return;
            }

            _currentState?.Exit(_context);
            _currentStateType = newState;
            _currentState = state;
            _currentState.Enter(_context);
        }
    }
}

