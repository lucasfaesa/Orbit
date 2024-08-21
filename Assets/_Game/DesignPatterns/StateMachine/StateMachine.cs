using System.Collections.Generic;
using UnityEngine;


namespace DesignPatterns
{
    public class StateMachine<T>
    {
        public State<T> CurrentState { get; private set; }

        public void Initialize(State<T> startingState)
        {
            CurrentState = startingState;
            startingState.Enter();
        }

        public void ChangeState(State<T> newState)
        {
            if (newState == CurrentState)
                return;
            
            CurrentState.Exit();
            CurrentState = newState;
            newState.Enter();
        }

        public void Update()
        {
            CurrentState?.LogicUpdate();
        }

        public void FixedUpdate()
        {
            CurrentState?.PhysicsUpdate();
        }
    }
}
