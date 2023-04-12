using TMPro;
using UnityEngine;

public class VectorDisplayer : ParameterDisplayer
{
    [SerializeField] private TMP_Text m_xPlaceHolder = null;
    [SerializeField] private TMP_InputField m_xInputField = null;

    [SerializeField] private TMP_Text m_yPlaceHolder = null;
    [SerializeField] private TMP_InputField m_yInputField = null;

    [SerializeField] private TMP_Text m_zPlaceHolder = null;
    [SerializeField] private TMP_InputField m_zInputField = null;

    private void OnEnable()
    {
        m_xInputField.onValueChanged.AddListener(OnValueChanged);
        m_yInputField.onValueChanged.AddListener(OnValueChanged);
        m_zInputField.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDisable()
    {
        m_xInputField.onValueChanged.RemoveListener(OnValueChanged);
        m_yInputField.onValueChanged.RemoveListener(OnValueChanged);
        m_zInputField.onValueChanged.RemoveListener(OnValueChanged);
    }

    public override void OnHandleSetted()
    {
        Vector3 defaultVector = (Vector3)m_defaultValue;

        m_xInputField.text = m_xPlaceHolder.text = defaultVector.x.ToString();
        m_yInputField.text = m_yPlaceHolder.text = defaultVector.y.ToString();
        m_zInputField.text = m_zPlaceHolder.text = defaultVector.z.ToString();
    }

    void OnValueChanged(string value)
    {
        if (!float.TryParse(m_xInputField.text, out float xResult))
            return;

        if (!float.TryParse(m_yInputField.text, out float yResult))
            return;

        if (!float.TryParse(m_zInputField.text, out float zResult))
            return;

        Vector3 vectorResult = new Vector3(xResult, yResult, zResult);

        m_field.SetValue(m_instance, vectorResult);

        m_onSetted?.Invoke();
    }
}
