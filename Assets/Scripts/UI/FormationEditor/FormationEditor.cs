using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

public class FormationEditor : MonoBehaviour
{
    [System.Serializable]
    public class TypedPrefab
    {
        public List<string> typeNames = null;
        public GameObject prefab = null;
    }

    [SerializeField] private string loadingDirectory = "SavedFormations";
    [SerializeField] private TMP_InputField m_filenameField = null;

    [SerializeField] private float m_zoomMultiplier = 1f;
    [SerializeField] private GameObject m_formationSelectorPrefab = null;
    [SerializeField] private GameObject m_unitIconPrefab = null;

    [SerializeField] private TypedPrefab[] m_placeholderPrefabs = null;

    private FormationRule[] m_presetRules = null;
    private List<FormationRule> m_instancedRules = new List<FormationRule>();
    private FormationRule m_currRule = null;
    private List<GameObject> m_virtualUnits = new List<GameObject>();

    private Transform m_formationTile = null;
    private Transform m_formationPresetContent = null;
    private Transform m_formationInstanceContent = null;
    private Transform m_formationParamContent = null;

    private List<GameObject> m_paramHolders = new List<GameObject>();

    private float m_zoom = 0f;

    // Start is called before the first frame update
    private void Start()
    {
        Transform layoutTransform = transform.Find("FormationPreset_Panel").Find("Editor Layout");

        m_formationTile = layoutTransform.Find("Formation Displayer").Find("Formation Tile");

        m_formationPresetContent = layoutTransform.Find("Preset Displayer").Find("Viewport").Find("Content");
        m_formationInstanceContent = layoutTransform.Find("Instance Displayer").Find("Viewport").Find("Content");
        m_formationParamContent = layoutTransform.Find("Parameter Displayer").Find("Viewport").Find("Content");

        LoadPreset();
        InitializePresetViewport();

        LoadInstancedRules();
        InitializeInstanceViewport();
    }

    private void InitializePresetViewport()
    {
        foreach (FormationRule rule in m_presetRules)
        {
            GameObject buttonGO = Instantiate(m_formationSelectorPrefab, m_formationPresetContent);
            Button buttonComp = buttonGO.GetComponent<Button>();
            buttonComp.onClick.AddListener(() => SetSelectedPresetFormation(rule));

            TextMeshProUGUI textComp = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            textComp.text = rule.name;
        }
    }

    private void InitializeInstanceViewport()
    {
        foreach (FormationRule rule in m_instancedRules)
        {
            GameObject buttonGO = Instantiate(m_formationSelectorPrefab, m_formationInstanceContent);
            Button buttonComp = buttonGO.GetComponent<Button>();
            buttonComp.onClick.AddListener(() => SetSelectedInstanceFormation(rule));

            TextMeshProUGUI textComp = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            textComp.text = rule.name;
        }
    }

    private void LoadPreset()
    {
        m_presetRules = Resources.FindObjectsOfTypeAll(typeof(FormationRule)) as FormationRule[];
    }

    private void LoadInstancedRules()
    {
        string dirpath = loadingDirectory;

        if (!Directory.Exists(dirpath))
            return;

        string[] filePathes = Directory.GetFiles(dirpath, "*.json");

        foreach (string filepath in filePathes)
        {
            string ruleStr = File.ReadAllText(filepath);

            FormationRule loadedRule = JsonConvert.DeserializeObject<LineFormation>(ruleStr);
            loadedRule.name = Path.GetFileNameWithoutExtension(filepath);

            if (loadedRule is not null)
                m_instancedRules.Add(loadedRule);
        }
    }

    private void SetSelectedPresetFormation(FormationRule newFormation)
    {
        FormationRule presetInstance = ScriptableObject.CreateInstance(newFormation.GetType()) as FormationRule;
        presetInstance.name = newFormation.name;

        SetSelectedInstanceFormation(presetInstance);
    }

    private void SetSelectedInstanceFormation(FormationRule newFormation)
    {
        m_currRule = newFormation;

        DisplayUnits();
        DisplayParams();

        m_filenameField.text = m_currRule.name;
    }

    private void DisplayUnits()
    {
        if (m_currRule is null)
            return;

        foreach (GameObject virtualUnit in m_virtualUnits)
            Destroy(virtualUnit);

        for (int i = 0; i < 10; i++)
        {
            Vector3 worldPosition = m_currRule.ComputePosition(Vector3.zero, m_formationTile.rotation, i);

            Vector3 virtualUnitPosition = new Vector3(worldPosition.x, worldPosition.z, 0f) * m_zoom + m_formationTile.position;

            GameObject virtualUnitGO = Instantiate(m_unitIconPrefab, virtualUnitPosition, Quaternion.identity, m_formationTile);
            virtualUnitGO.transform.localScale = Vector3.one / m_zoom;
            m_virtualUnits.Add(virtualUnitGO);
        }
    }

    private void DisplayParams()
    {
        foreach (GameObject paramHolder in m_paramHolders)
            Destroy(paramHolder);

        FieldInfo[] fields = m_currRule.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (FieldInfo field in fields)
        {
            TypedPrefab prefabPair = m_placeholderPrefabs.FirstOrDefault(pair => pair.typeNames.Contains(field.FieldType.Name));

            if (prefabPair is null) continue;

            GameObject paramHolderGO = Instantiate(prefabPair.prefab, Vector3.zero, Quaternion.identity, m_formationParamContent);

            paramHolderGO.GetComponent<ParameterDisplayer>()?.SetHandle(m_currRule, field, () => DisplayUnits());

            m_paramHolders.Add(paramHolderGO);
        }
    }

    public void SaveCurrentRule()
    {
        string dirpath = loadingDirectory;

        if (!Directory.Exists(dirpath))
            Directory.CreateDirectory(dirpath);

        string ruleAsJSON = JsonConvert.SerializeObject(m_currRule);

        string fileName = m_filenameField.text;

        string filePath = dirpath + '/' + fileName + ".json";

        File.WriteAllText(filePath, ruleAsJSON);

        if (!m_instancedRules.Contains(m_currRule))
            m_instancedRules.Add(m_currRule);
    }

    public void OnZoom(float zoom)
    {
        m_zoom = 1f + zoom * m_zoomMultiplier;

        if (m_formationTile is not null)
            m_formationTile.localScale = Vector3.one * m_zoom;

        DisplayUnits();
    }
}
