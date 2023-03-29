using System.Collections.Generic;
using UnityEngine;

namespace AIPlanner.GOAP
{
    [System.Serializable]
    public class Goal
    {

#if UNITY_EDITOR
        [HideInInspector] public string name;
        [HideInInspector] public bool show;
        [HideInInspector] public bool showStates;
#endif

        [SerializeField] private List<StateId> m_states = new List<StateId>();
        public List<StateId> States => m_states;

        [SerializeField] private AnimationCurve m_animationCurve = new AnimationCurve();
        public AnimationCurve Curve => m_animationCurve;

        [SerializeField] private Method m_considerationMethod = new Method();

        public void Initialize(GameObject GameObject)
        {
            for (int i = 0; i < m_states.Count; ++i)
            {
                StateId stateId = m_states[i];
                stateId.stateValue.ComputeHashValue();
                m_states[i] = stateId;
            }

            m_considerationMethod.Initialize(GameObject);
        }

        public float Evaluate(WorldState worldState)
        {
            if (!m_considerationMethod.IsValid())
            {
                Debug.LogError("Consideration method is invalid");
                return 0;
            }

            float value = (float)m_considerationMethod.Invoke(worldState);
            return m_animationCurve.Evaluate(value);
        }
    }
}
