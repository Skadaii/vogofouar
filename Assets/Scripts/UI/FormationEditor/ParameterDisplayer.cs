using System.Reflection;
using TMPro;
using UnityEngine;

public abstract class ParameterDisplayer : MonoBehaviour
{
    [SerializeField] private TMP_Text m_parameterNameField = null;


    protected object m_defaultValue = null;

    protected object m_instance = null;
    protected FieldInfo m_field = null;

    protected System.Action m_onSetted = null;

    public void SetHandle(object instance, FieldInfo field, System.Action onUpdated = null)
    {
        m_instance = instance;
        m_onSetted = onUpdated;

        m_field = field;
        m_parameterNameField.text = m_field.Name;

        m_defaultValue = m_field.GetValue(m_instance);

        OnHandleSetted();
    }

    public abstract void OnHandleSetted();
}
