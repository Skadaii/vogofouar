using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public abstract class ParameterDisplayer : MonoBehaviour
{
    protected object m_instance = null;
    protected FieldInfo m_field = null;

    public void SetHandle(object instance, FieldInfo field)
    {
        m_instance = instance;
        m_field = field;

        OnHandleSetted();
    }

    public abstract void OnHandleSetted();
}
