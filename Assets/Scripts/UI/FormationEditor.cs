using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FormationEditor : MonoBehaviour
{
    [SerializeField] private float m_zoomMultiplier = 5f;
    [SerializeField] private GameObject m_formationSelectorPrefab = null;
    [SerializeField] private GameObject m_unitIconPrefab = null;

    private FormationRule[] m_rules = null;
    private FormationRule m_currRule = null;
    private List<GameObject> m_virtualUnits = new List<GameObject>();

    private Transform m_formationDisplayer = null;

    // Start is called before the first frame update
    void Start()
    {
        Transform layoutTransform = transform.Find("FormationPreset_Panel").Find("Layout");

        m_formationDisplayer = layoutTransform.Find("Formation Displayer");

        Transform contentTransform = layoutTransform.Find("Preset Displayer").Find("Viewport").Find("Content");

        m_rules = Resources.FindObjectsOfTypeAll(typeof(FormationRule)) as FormationRule[];

        foreach (FormationRule rule in m_rules)
        {
            GameObject buttonGO = Instantiate(m_formationSelectorPrefab, contentTransform);
            Button buttonComp = buttonGO.GetComponent<Button>();
            buttonComp.onClick.AddListener(() => SetSelectedFormation(rule));

            TextMeshProUGUI textComp = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            textComp.text = rule.name;
        }
    }

    void SetSelectedFormation(FormationRule newFormation)
    {
        m_currRule = ScriptableObject.CreateInstance(newFormation.GetType()) as FormationRule;

        foreach (GameObject virtualUnit in m_virtualUnits)
            Destroy(virtualUnit);

        for (int i = 0; i < 10; i++)
        {
            Vector3 worldPosition = m_currRule.ComputePosition(Vector3.zero, m_formationDisplayer.rotation, i);

            Vector3 virtualUnitPosition = new Vector3(worldPosition.x, worldPosition.z, 0f) * m_zoomMultiplier + m_formationDisplayer.position;

            GameObject virtualUnitGO = Instantiate(m_unitIconPrefab, virtualUnitPosition, Quaternion.identity, m_formationDisplayer);
            m_virtualUnits.Add(virtualUnitGO);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
