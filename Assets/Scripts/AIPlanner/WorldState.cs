using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIPlanner.GOAP
{
    [System.Serializable]
    public struct WorldState
    {
        [HideInInspector] public string StateName;

        [SerializeField] public List<State> States;

        public void Initialize(GameObject GameObject)
        {
            for (int i = 0; i < States.Count; ++i)
            {
                State state = States[i];
                state.Initialize(GameObject);
                States[i] = state;
            }
        }

        public void ComputeHashValues()
        {
            for (int i = 0; i < States.Count; ++i)
            {
                State state = States[i];
                state.StateValue.ComputeHashValue();
                States[i] = state;
            }
        }

        public void UpdateStates()
        {
            for (int i = 0; i < States.Count; i++)
            {
                State state = States[i];

                if (state.Update())
                    States[i] = state;
            }
        }

        public State GetState(string stateName)
        {
            for (int i = 0; i < States.Count; ++i)
            {
                State state = States[i];
                if (state.Name == stateName)
                    return state;
            }

            return new State();
        }
    }
}
