using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FloatDisplayer : ParameterDisplayer
{
    [SerializeField] private TMP_InputField m_inputField = null;

    private void Awake()
    {
        m_inputField = GetComponent<TMP_InputField>();
    }

    private void OnEnable()
    {
        m_inputField.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDisable()
    {
        m_inputField.onValueChanged.RemoveListener(OnValueChanged);
    }

    public override void OnHandleSetted()
    {
        m_inputField.text = m_field.GetValue(m_instance).ToString();
    }

    void OnValueChanged(string value)
    {
        if (!float.TryParse(value, out float result))
            return;

        m_field.SetValue(m_instance, result);
        m_onSetted?.Invoke();
    }
}
