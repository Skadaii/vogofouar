using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using TMPro;
using UnityEngine;

public class DecimalDisplayer : ParameterDisplayer
{
    protected TypeConverter m_converter = null;

    [SerializeField] private TMP_InputField m_inputField = null;
    [SerializeField] private TMP_Text m_placeHolder = null;

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
        m_converter = TypeDescriptor.GetConverter(m_field.FieldType);

        m_inputField.text = m_placeHolder.text = m_defaultValue.ToString();
    }

    void OnValueChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            object result = m_converter.ConvertFromString(value);

            if (result != null)
                m_field.SetValue(m_instance, result);
        }
        else
            m_field.SetValue(m_instance, m_defaultValue);

        m_onSetted?.Invoke();
    }
}
