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

    Quaternion originalRotation;


    public float Health
    {
        set
        {
            m_healthBar.value = Mathf.Clamp(value, 0f, 1f);
        }
    }

    public float Progression
    {
        set
        {
            m_progressBar.value = Mathf.Clamp(value, 0f, 1f);

            if (m_progressBar.value == 1f || m_progressBar.value == 0f)
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
    }
}
