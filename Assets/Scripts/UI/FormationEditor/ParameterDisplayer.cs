using AIPlanner.GOAP;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public abstract class ParameterDisplayer : MonoBehaviour
{
    protected object m_instance = null;
    protected FieldInfo m_field = null;

    protected System.Action m_onSetted = null;

    public void SetHandle(object instance, FieldInfo field, System.Action onUpdated = null)
    {
        m_instance = instance;
        m_field = field;
        m_onSetted = onUpdated;

        OnHandleSetted();
    }

    public abstract void OnHandleSetted();
}
