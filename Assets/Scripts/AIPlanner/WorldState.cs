using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIPlanner.GOAP
{
    [System.Serializable]
    public struct WorldState
    {
#if UNITY_EDITOR
        [HideInInspector] public bool show;
#endif

        public string stateName;
        public List<State> states;

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

#if UNITY_EDITOR
        //Serialization is necessary to avoid having a shared state pointer
        public static WorldState Clone(WorldState worldState)
        {
            string strWorldState = JsonUtility.ToJson(worldState);
            return JsonUtility.FromJson<WorldState>(strWorldState);
        }
#endif
    }
}
