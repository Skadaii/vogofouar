using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FormationEditor : MonoBehaviour
{
    [SerializeField] private GameObject m_formationSelectorPrefab = null;

    private FormationRule[] m_rules = null;
    private FormationRule m_currRule = null;

    // Start is called before the first frame update
    void Start()
    {
        Transform ContentTransform = transform.Find("FormationPreset_Panel").Find("Scroll View").Find("Viewport").Find("Content");

        m_rules = Resources.FindObjectsOfTypeAll(typeof(FormationRule)) as FormationRule[];

        foreach (FormationRule rule in m_rules)
        {
            GameObject buttonGO = Instantiate(m_formationSelectorPrefab, ContentTransform);
            Button buttonComp = buttonGO.GetComponent<Button>();
            buttonComp.onClick.AddListener(() => SetSelectedFormation(rule));

            TextMeshProUGUI textComp = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            textComp.text = rule.name;
        }
    }

    void SetSelectedFormation(FormationRule newFormation)
    {
        Debug.Log(newFormation.name);
        m_currRule = newFormation;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
