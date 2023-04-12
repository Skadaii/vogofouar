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

    [SerializeField] private float m_zoomMultiplier = 5f;
    [SerializeField] private GameObject m_formationSelectorPrefab = null;
    [SerializeField] private GameObject m_unitIconPrefab = null;

    [SerializeField] private TypedPrefab[] m_placeholderPrefabs = null;

    private FormationRule[] m_presetRules = null;
    private Dictionary<string, FormationRule> m_instancedRules = new Dictionary<string, FormationRule>();
    private FormationRule m_currRule = null;
    private List<GameObject> m_virtualUnits = new List<GameObject>();

    private Transform m_formationDisplayer = null;
    private Transform m_formationPresetContent = null;
    private Transform m_formationInstanceContent = null;
    private Transform m_formationParamContent = null;

    private List<GameObject> m_paramHolders = new List<GameObject>();


    // Start is called before the first frame update
    void Start()
    {
        Transform layoutTransform = transform.Find("FormationPreset_Panel").Find("Layout");

        m_formationDisplayer = layoutTransform.Find("Formation Displayer");

        m_formationPresetContent = layoutTransform.Find("Preset Displayer").Find("Viewport").Find("Content");
        m_formationInstanceContent = layoutTransform.Find("Instance Displayer").Find("Viewport").Find("Content");
        m_formationParamContent = layoutTransform.Find("Parameter Displayer").Find("Viewport").Find("Content");

        LoadPreset();
        InitializePresetViewport();

        LoadInstancedRules();
        InitializeInstanceViewport();
    }

    void InitializePresetViewport()
    {
        foreach (FormationRule rule in m_presetRules)
        {
            GameObject buttonGO = Instantiate(m_formationSelectorPrefab, m_formationPresetContent);
            Button buttonComp = buttonGO.GetComponent<Button>();
            buttonComp.onClick.AddListener(() => SetSelectedFormation(rule));

            TextMeshProUGUI textComp = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            textComp.text = rule.name;
        }
    }

    void InitializeInstanceViewport()
    {
        foreach (var (ruleName, rule) in m_instancedRules)
        {
            GameObject buttonGO = Instantiate(m_formationSelectorPrefab, m_formationInstanceContent);
            Button buttonComp = buttonGO.GetComponent<Button>();
            buttonComp.onClick.AddListener(() => SetSelectedFormation(rule));

            TextMeshProUGUI textComp = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            textComp.text = ruleName;
        }
    }

    void LoadPreset()
    {
        m_presetRules = Resources.FindObjectsOfTypeAll(typeof(FormationRule)) as FormationRule[];
    }

    void LoadInstancedRules()
    {
        string dirpath = loadingDirectory;

        if (!Directory.Exists(dirpath))
            return;

        string[] filePathes = Directory.GetFiles(dirpath, "*.json");

        foreach (string filepath in filePathes)
        {
            string ruleStr = File.ReadAllText(filepath);

            FormationRule loadedRule = JsonConvert.DeserializeObject<LineFormation>(ruleStr);

            if (loadedRule is not null)
                m_instancedRules.Add(Path.GetFileNameWithoutExtension(filepath), loadedRule);
        }
    }

    void SetSelectedFormation(FormationRule newFormation)
    {
        m_currRule = ScriptableObject.CreateInstance(newFormation.GetType()) as FormationRule;

        DisplayUnits();
        DisplayParams();
    }

    void DisplayUnits()
    {
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

    void DisplayParams()
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

        if (!m_instancedRules.ContainsKey(fileName))
            m_instancedRules.Add(fileName, m_currRule);
    }
}
