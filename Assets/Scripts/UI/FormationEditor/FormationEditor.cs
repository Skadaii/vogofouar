using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Data;
using UnityEngine.Events;
using AIPlanner.GOAP;

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

    private Dictionary<FormationRule, GameObject> m_buttonGOs = new Dictionary<FormationRule, GameObject>();

    private FormationRule[] m_presetRules = null;
    private List<FormationRule> m_instancedRules = new List<FormationRule>();
    private FormationRule m_currRule = null;
    private List<GameObject> m_virtualUnits = new List<GameObject>();

    private Transform m_formationTile = null;
    private Transform m_formationPresetContent = null;
    private Transform m_formationInstanceContent = null;
    private Transform m_formationParamContent = null;

    private List<GameObject> m_paramHolders = new List<GameObject>();

    public UnityEvent<FormationRule> OnNewRuleCreated = new UnityEvent<FormationRule>();
    public UnityEvent<FormationRule> OnRuleDeleted = new UnityEvent<FormationRule>();

    private float m_zoom = 1f;

    static private JsonSerializerSettings m_serializationSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

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

        m_instancedRules = LoadInstancedRules(loadingDirectory);
        InitializeInstanceViewport();
    }

    private GameObject CreatePresetRuleButton(FormationRule rule) => CreateRuleButton(rule, m_formationPresetContent, () => SetSelectedPresetFormation(rule));
    private GameObject CreateInstanceRuleButton(FormationRule rule) => CreateRuleButton(rule, m_formationInstanceContent, () => SetSelectedInstanceFormation(rule));

    private GameObject CreateRuleButton(FormationRule rule, Transform contentListTransform, UnityAction action)
    {
        GameObject buttonGO = Instantiate(m_formationSelectorPrefab, contentListTransform);
        Button buttonComp = buttonGO.GetComponent<Button>();
        buttonComp.onClick.AddListener(action);

        TextMeshProUGUI textComp = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
        textComp.text = rule.name;

        return buttonGO;
    }

    private void InitializePresetViewport()
    {
        foreach (FormationRule rule in m_presetRules)
            CreatePresetRuleButton(rule);
    }

    private void InitializeInstanceViewport()
    {
        foreach (FormationRule rule in m_instancedRules)
        {
            GameObject GO = CreateInstanceRuleButton(rule);
            m_buttonGOs.Add(rule, GO);
        }
    }

    private void LoadPreset()
    {
        m_presetRules = Resources.LoadAll<FormationRule>("ScriptableObjects/Formations");
    }

    static public List<FormationRule> LoadInstancedRules(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            return null;

        string[] filePathes = Directory.GetFiles(dirPath, "*.json");

        List<FormationRule> instancedRules = new List<FormationRule>();

        foreach (string filepath in filePathes)
        {
            string ruleStr = File.ReadAllText(filepath);

            FormationRule loadedRule = JsonConvert.DeserializeObject<FormationRule>(ruleStr, m_serializationSettings);
            loadedRule.name = Path.GetFileNameWithoutExtension(filepath);

            if (loadedRule is not null)
                instancedRules.Add(loadedRule);
        }

        return instancedRules;
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

        string filename = m_currRule.name = m_filenameField.text;

        string ruleAsJSON = JsonConvert.SerializeObject(m_currRule, m_serializationSettings);

        string filePath = dirpath + '/' + filename + ".json";

        File.WriteAllText(filePath, ruleAsJSON);

        if (!m_instancedRules.Contains(m_currRule))
        {
            m_instancedRules.Add(m_currRule);
            GameObject buttonGO = CreateInstanceRuleButton(m_currRule);
            m_buttonGOs.Add(m_currRule, buttonGO);
            OnNewRuleCreated.Invoke(m_currRule);
        }
    }

    public void DeleteCurrentRule()
    {
        string dirpath = loadingDirectory;

        if (!Directory.Exists(dirpath) || m_currRule is null)
            return;

        string filename = m_currRule.name;

        string filePath = dirpath + '/' + filename + ".json";

        if (!File.Exists(filePath))
            return;

        OnRuleDeleted.Invoke(m_currRule);

        Destroy(m_buttonGOs[m_currRule]);
        m_buttonGOs.Remove(m_currRule);

        File.Delete(filePath);

        m_instancedRules.Remove(m_currRule);

        m_currRule = m_instancedRules.FirstOrDefault();
    }

    public void OnZoom(float zoom)
    {
        m_zoom = 1f + zoom * m_zoomMultiplier;

        if (m_formationTile)
            m_formationTile.localScale = Vector3.one * m_zoom;

        DisplayUnits();
    }
}
