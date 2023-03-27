using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIPlanner.GOAP
{
    [System.Serializable]
    public class Action
    {
        [HideInInspector] public string name;

        [SerializeField] private Method m_actionMethod;
        [SerializeField] private List<StateId> m_stateEffects = new List<StateId>();
        [SerializeField] private List<Precondition> m_preconditions = new List<Precondition>();
        public List<Precondition> Preconditions => m_preconditions;


        [System.Serializable]
        public class Precondition
        {
            public List<StateId> States;
            public int Cost;
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
            sampleAction.Name = "Test";
            sampleAction.m_stateEffects.Add(new StateId() { id = 0, stateValue = new StateValue() { Value = new BoolType(true) } });
            sampleAction.m_stateEffects.Add(new StateId() { id = 1, stateValue = new StateValue() { Value = new BoolType(true) } });
            sampleAction.m_stateEffects.Add(new StateId() { id = 2, stateValue = new StateValue() { Value = new BoolType(true) } });
            sampleAction.m_stateEffects.Add(new StateId() { id = 3, stateValue = new StateValue() { Value = new BoolType(true) } });
            sampleAction.m_actionMethod = new Method();
            Precondition precondition = new Precondition();
            precondition.Cost = 1000;
            precondition.States = new List<StateId>();
            // precondition.States.Add(new StateId() { Id = 0, StateValue = new StateValue() { Value = new BoolType(false) } });
            //precondition.States.Add(new StateId() { Id = 1, StateValue = new StateValue() { Value = new BoolType(false) } });
            //precondition.States.Add(new StateId() { Id = 2, StateValue = new StateValue() { Value = new BoolType(false) } });
            //precondition.States.Add(new StateId() { Id = 3, StateValue = new StateValue() { Value = new BoolType(false) } });

            precondition.Cost = 100;

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

            foreach (StateId stateId in precondition.States)
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

        public int GetCost(int preconditionId) => m_preconditions[preconditionId].Cost;

        public void Initialize(GameObject GameObject)
        {
            for (int i = 0; i < m_stateEffects.Count; ++i)
            {
                StateId stateId = m_stateEffects[i];
                stateId.stateValue.ComputeHashValue();
                m_stateEffects[i] = stateId;
            }

            foreach (Precondition precondition in m_preconditions)
            {
                for (int i = 0; i < precondition.States.Count; ++i)
                {
                    StateId stateId = precondition.States[i];
                    stateId.stateValue.ComputeHashValue();
                    precondition.States[i] = stateId;
                }
            }

            m_actionMethod.Initialize(GameObject);
        }

        public bool Start(in WorldState InWorldState, int preconditionId)
        {
            if (!CheckIsValidPrecondition(in InWorldState, preconditionId))
                return false;

            return true;
        }


        public void Update(in WorldState InWorldState, out EActionState OutActionState)
        {
            OutActionState = (EActionState)m_actionMethod.Invoke(InWorldState);
        }
    }
}