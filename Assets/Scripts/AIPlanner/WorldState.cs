using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIPlanner.GOAP
{
    [System.Serializable]
    public struct WorldState
    {
        [HideInInspector] public string stateName;

        [SerializeField] public List<State> states;

        public void Initialize(GameObject GameObject)
        {
            for (int i = 0; i < states.Count; ++i)
            {
                State state = states[i];
                state.Initialize(GameObject);
                states[i] = state;
            }
        }

        public void ComputeHashValues()
        {
            for (int i = 0; i < states.Count; ++i)
            {
                State state = states[i];
                state.stateValue.ComputeHashValue();
                states[i] = state;
            }
        }

        public void UpdateStates()
        {
            for (int i = 0; i < states.Count; i++)
            {
                State state = states[i];

                if (state.Update())
                    states[i] = state;
            }
        }

        public State GetState(string stateName)
        {
            for (int i = 0; i < states.Count; ++i)
            {
                State state = states[i];
                if (state.name == stateName)
                    return state;
            }

            return new State();
        }
    }
}
