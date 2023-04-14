using UnityEngine;
using UnityEngine.UI;

public class BooleanDisplayer : ParameterDisplayer
{
    [SerializeField] private Toggle m_toggle = null;

    private void OnEnable()
    {
        m_toggle.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDisable()
    {
        m_toggle.onValueChanged.RemoveListener(OnValueChanged);
    }

    public override void OnHandleSetted()
    {
        m_toggle.isOn = (bool)m_defaultValue;
    }

    void OnValueChanged(bool value)
    {
        m_field.SetValue(m_instance, value);
        m_onSetted?.Invoke();
    }
}
