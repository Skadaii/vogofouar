using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace AIPlanner.GOAP
{
    [System.Serializable]
    public struct StateValue
    {
        [SerializeReference] private object m_value;

        private Hash128 m_hashValue;

        public Hash128 HashValue => m_hashValue;
        public object Value
        {
            get => m_value;
            set
            {
                m_value = value;
                //ComputeHashValue();
            }
        }

        public void ComputeHashValue()
        {
            string str = JsonUtility.ToJson(m_value);
            byte[] bytes = Encoding.ASCII.GetBytes(str);

            m_hashValue = new Hash128();

            HashUtilities.ComputeHash128(bytes, ref m_hashValue);
        }
    }

    [System.Serializable]
    public struct State
    {
        public string Name;
        public StateValue StateValue;

        [SerializeField] private Method m_stateMethod;

        public void Initialize(GameObject GameObject)
        {
            m_stateMethod.Initialize(GameObject);
            StateValue.ComputeHashValue();
        }

        public bool Update()
        {
            if (!m_stateMethod.IsValid())
                return false;

            StateValue.Value = m_stateMethod.Invoke();

            return true;
        }
    }

    [System.Serializable]
    public struct StateId
    {
        public int Id;
        public StateValue StateValue;
    }
}
