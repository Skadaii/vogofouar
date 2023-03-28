using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIPlanner.GOAP
{
    [System.Serializable]
    public class Action : Method
    {
        [HideInInspector] public string name;

        [SerializeField] private List<StateId> m_stateEffects = new List<StateId>();
        [SerializeField] private List<Precondition> m_preconditions = new List<Precondition>();
        public List<Precondition> Preconditions => m_preconditions;


        [System.Serializable]
        public class Precondition
        {
            public List<StateId> states;
            public int cost;
        }

        public enum EActionState
        {
            Loading,
            Finished,
            Failed,
        }

        public static Action CreateSampleAction()
        {
            Action sampleAction = new Action();
            sampleAction.name = "Test";
            sampleAction.m_stateEffects.Add(new StateId() { id = 0, stateValue = new StateValue() { Value = new BoolType(true) } });
            sampleAction.m_stateEffects.Add(new StateId() { id = 1, stateValue = new StateValue() { Value = new BoolType(true) } });
            sampleAction.m_stateEffects.Add(new StateId() { id = 2, stateValue = new StateValue() { Value = new BoolType(true) } });
            sampleAction.m_stateEffects.Add(new StateId() { id = 3, stateValue = new StateValue() { Value = new BoolType(true) } });
            Precondition precondition = new Precondition();
            precondition.states = new List<StateId>();
            precondition.cost = 100;

            sampleAction.m_preconditions.Add(precondition);
            return sampleAction;
        }

        public WorldState ApplyEffects(WorldState WorldState)
        {
            WorldState NewWorldStates = new WorldState();
            NewWorldStates.states = WorldState.states.ToList();

            foreach (StateId stateEffects in m_stateEffects)
            {
                State state = NewWorldStates.states[stateEffects.id];
                state.stateValue = stateEffects.stateValue;
                NewWorldStates.states[stateEffects.id] = state;
            }

            return NewWorldStates;
        }

        public bool CheckIsValidPrecondition(in WorldState InWorldState, int preconditionId)
        {
            Precondition precondition = m_preconditions[preconditionId];

            foreach (StateId stateId in precondition.states)
            {
                if (InWorldState.states[stateId.id].stateValue.HashValue != stateId.stateValue.HashValue)
                    return false;
            }

            return true;
        }

        public int IsValid(in WorldState InWorldState)
        {
            for (int i = 0; i < m_preconditions.Count; ++i)
            {
                if (CheckIsValidPrecondition(InWorldState, i))
                    return i;
            }

            return -1;
        }

        public int GetCost(int preconditionId) => m_preconditions[preconditionId].cost;

        public override void Initialize(GameObject GameObject)
        {
            base.Initialize(GameObject);

            for (int i = 0; i < m_stateEffects.Count; ++i)
            {
                StateId stateId = m_stateEffects[i];
                stateId.stateValue.ComputeHashValue();
                m_stateEffects[i] = stateId;
            }

            foreach (Precondition precondition in m_preconditions)
            {
                for (int i = 0; i < precondition.states.Count; ++i)
                {
                    StateId stateId = precondition.states[i];
                    stateId.stateValue.ComputeHashValue();
                    precondition.states[i] = stateId;
                }
            }
        }

        public bool Start(in WorldState InWorldState, int preconditionId)
        {
            if (!CheckIsValidPrecondition(in InWorldState, preconditionId))
                return false;

            return true;
        }

        public void Update(in WorldState InWorldState, out EActionState OutActionState)
        {
            OutActionState = (EActionState)Invoke(InWorldState);
        }
    }
}