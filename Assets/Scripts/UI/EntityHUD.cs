using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EntityHUD : MonoBehaviour
{
    [Header("Health")]
    [SerializeField]
    private Slider m_healthBar;
    [SerializeField]
    private Image m_healthFill;
    [SerializeField]
    private Color m_highHealthColor;
    [SerializeField]
    private Color m_lowHealthColor;
    [SerializeField]
    private float m_healthDurationUpdate = 0.05f;

    private float m_healthValueSaved = 0f;
    private float m_healthTargetValue = 0f;
    private float m_elapsedTimeSinceUpdate = 0f;

    [Header("ProgressBar")]
    [SerializeField]
    private Slider m_progressBar;
    [SerializeField]
    private Image m_progressFill;
    [SerializeField]
    private Color m_highProgressColor;
    [SerializeField]
    private Color m_lowProgressColor;

    public float Health
    {
        get => m_healthTargetValue;
        set
        {
            m_healthTargetValue = Mathf.Clamp(value, 0f, 1f);
            m_elapsedTimeSinceUpdate = Time.time;
            m_healthValueSaved = m_healthBar.value;
        }
    }

    public float Progression
    {
        set
        {
            m_progressBar.value = Mathf.Clamp(value, 0f, 1f);
            SetProgressBarColor();

            if (m_progressFill != null)
            {
                SetProgressBarColor();
            }

            if ( m_progressBar.value == 1f ||  m_progressBar.value == 0f)
            {
                m_progressBar.gameObject.SetActive(false);
            }
            else
            {
                m_progressBar.gameObject.SetActive(true);
            }
        }
    }


    protected void Start()
    {
        if (m_healthFill != null)
        {
            SetHealthBarColor();
        }
    }

    protected void Update()
    {
        if(m_healthBar.isActiveAndEnabled && m_elapsedTimeSinceUpdate >= 0)
        {
            m_elapsedTimeSinceUpdate += Time.deltaTime;
            float delta = Mathf.Min(m_elapsedTimeSinceUpdate / m_healthDurationUpdate, 1f);
            m_healthBar.value = Mathf.Lerp(m_healthValueSaved, m_healthTargetValue, delta);

            if (delta >= 1f) m_elapsedTimeSinceUpdate = -1f;

            if (m_healthFill != null)
            {
                SetHealthBarColor();
            }
        }
    }

    private void SetHealthBarColor() => m_healthFill.color = Color.Lerp(m_lowHealthColor, m_highHealthColor, m_healthBar.value / m_healthBar.maxValue);

    private void SetProgressBarColor() => m_progressFill.color = Color.Lerp(m_lowProgressColor, m_highProgressColor, m_progressBar.value / m_progressBar.maxValue);
}
