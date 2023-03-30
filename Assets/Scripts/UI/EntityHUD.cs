using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EntityHUD : MonoBehaviour
{
    public Transform camTransform;

    [SerializeField]
    private Slider m_healthBar;

    [SerializeField]
    private Slider m_progressBar;

    [SerializeField]
    private float m_healthDurationUpdate = 0.05f;
    private float m_healthValueSaved = 0f;

    Quaternion originalRotation;

    private float m_healthTargetValue = 0f;
    private float m_lastHealthUpdate = 0f;

    public float Health
    {
        set
        {
            m_healthTargetValue = Mathf.Clamp(value, 0f, 1f);
            m_lastHealthUpdate = Time.time;
            m_healthValueSaved = m_healthBar.value;
        }
    }

    public float Progression
    {
        set
        {
            m_progressBar.value = Mathf.Clamp(value, 0f, 1f);

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

    void Start()
    {
        camTransform = Camera.main.transform;
        originalRotation = transform.rotation;
    }

    void Update()
    {
        if(camTransform != null)
        {
            transform.rotation = camTransform.rotation * originalRotation;
        }

        if(m_healthBar.isActiveAndEnabled)
        {
            float delta = Mathf.Min((Time.time - m_lastHealthUpdate)/ m_healthDurationUpdate, 1f);
            m_healthBar.value = Mathf.Lerp(m_healthValueSaved, m_healthTargetValue, delta);
        }
    }
}
