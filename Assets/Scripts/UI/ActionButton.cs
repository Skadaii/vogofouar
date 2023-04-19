using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ActionButton : MonoBehaviour
{

    [SerializeField] private TMPro.TMP_Text m_text;
    [SerializeField] private UnityEngine.UI.Image m_image;
    [SerializeField] private UnityEngine.UI.Button m_button;

    private Sprite _icon = null;
    private int _count = 0;

    public int Count
    {
        get => _count;

        set
        {
            _count = value;
            m_text.text = _count.ToString();
            m_text.enabled = _count > 0;
        }
    }

    public Sprite Icon
    {
        get => _icon;

        set
        {
            m_image.sprite = _icon = value;
        }
    }

    public void Select() => m_button?.Select();
    public void SetSize(float size)
    {
        RectTransform rt = gameObject.GetComponent(typeof(RectTransform)) as RectTransform;
        if (rt != null) rt.sizeDelta = new Vector2(size, size);

        RectTransform rt2 = m_text.gameObject.GetComponent(typeof(RectTransform)) as RectTransform; 
        if (rt2 != null) rt2.sizeDelta = new Vector2(size, size);

        m_text.fontSize = size * 0.5f;
    }
}
