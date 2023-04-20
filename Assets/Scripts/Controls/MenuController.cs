using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using TMPro;

public class MenuController : MonoBehaviour
{
    [SerializeField]
    private string[] m_instancedFormationDirectories = null;

    private List<FormationRule> m_instancedRules = new List<FormationRule>();

    [SerializeField] private Transform m_playerUI = null;

    private UnitController m_controller = null;
    private GameObject m_factoryMenuPanel = null;
    private Text m_buildPointsText = null;
    private Text m_capturedTargetsText = null;
    private Button[] m_buildUnitButtons = null;

    [SerializeField] private FormationEditor m_formationEditor = null;

    //  Formations

    [Header("Formations")]
    [SerializeField] private GameObject m_buttonFormationSelectionPrefab = null;

    private Dictionary<FormationRule, Button> m_formationButtons = new Dictionary<FormationRule, Button>();
    private Transform m_formationContent = null;

    public GraphicRaycaster BuildMenuRaycaster { get; private set; }


    //  Functions
    //  ---------

    #region MonoBehaviour methods

    void Awake()
    {
        if (m_playerUI == null)
        {
            Debug.LogWarning("Player UI not assigned in inspector");
            return;
        }

        m_controller = GetComponent<UnitController>();

        Transform FactoryMenuPanelTransform = m_playerUI.Find("FactoryMenu_Panel");
        if (FactoryMenuPanelTransform)
        {
            m_factoryMenuPanel = FactoryMenuPanelTransform.gameObject;
            m_factoryMenuPanel.SetActive(false);
        }
        BuildMenuRaycaster = m_playerUI.GetComponent<GraphicRaycaster>();
        Transform BuildPointsTextTransform = m_playerUI.Find("BuildPointsText");
        if (BuildPointsTextTransform)
        {
            m_buildPointsText = BuildPointsTextTransform.GetComponent<Text>();
        }
        Transform CapturedTargetsTextTransform = m_playerUI.Find("CapturedTargetsText");
        if (CapturedTargetsTextTransform)
        {
            m_capturedTargetsText = CapturedTargetsTextTransform.GetComponent<Text>();
        }
    }

    void Start()
    {
        m_buildUnitButtons = m_factoryMenuPanel.transform.Find("BuildUnitMenu_Panel").GetComponentsInChildren<Button>();
        m_formationContent = m_playerUI.Find("FormationMenu_Panel").Find("Scroll View").Find("Viewport").Find("Content");

        LoadAvailableRules();
        InitializeInstanceViewport();

        m_formationEditor.OnNewRuleCreated.AddListener(OnNewRule);
        m_formationEditor.OnRuleDeleted.AddListener(OnRuleDeleted);

    }

    #endregion

    public void UpdateBuildPointsUI()
    {
        if (m_buildPointsText != null)
            m_buildPointsText.text = "Build Points : " + m_controller.CurrentResources;
    }

    public void UpdateCapturedTargetsUI()
    {
        if (m_capturedTargetsText != null)
            m_capturedTargetsText.text = "Captured Targets : " + m_controller.CapturedTargets;
    }

    public void UnregisterFormationButtons()
    {
        foreach (var (rule, button) in m_formationButtons)
            button.onClick.RemoveAllListeners();
    }

    public void UpdateFormationMenu(List<Unit> selectedUnit, Action<List<Unit>, FormationRule> setSquadMethod)
    {
        Unit firstUnit = selectedUnit.First();

        bool isMixed = selectedUnit.Any(u => u.Squad != firstUnit.Squad);

        int i = 0;

        foreach (var (rule, button) in m_formationButtons)
            button.onClick.AddListener(() => setSquadMethod(selectedUnit, rule));
    }

    void LoadAvailableRules()
    {
        foreach (string formationDir in m_instancedFormationDirectories)
        {
            List<FormationRule> instancedRulesRange = FormationEditor.LoadInstancedRules(formationDir);

            if (instancedRulesRange is not null)
                m_instancedRules.AddRange(instancedRulesRange);
        }
    }

    void OnNewRule(FormationRule newRule)
    {
        CreateRuleButton(newRule, m_formationContent);
    }

    void OnRuleDeleted(FormationRule newRule)
    {
        var formationPairs = m_formationButtons.Where(pair => pair.Key.name == newRule.name).ToList();

        foreach (var (rule, button) in formationPairs)
        {
            Destroy(button.gameObject);
            m_formationButtons.Remove(rule);
        }

        m_instancedRules.RemoveAll((rule) => rule.name == newRule.name);
    }

    private void InitializeInstanceViewport()
    {
        for (int i = 0; i < m_instancedRules.Count; i++)
            CreateRuleButton(m_instancedRules[i], m_formationContent);
    }

    private void CreateRuleButton(FormationRule rule, Transform contentListTransform)
    {
        GameObject buttonGO = Instantiate(m_buttonFormationSelectionPrefab, contentListTransform);

        Button buttonComp = buttonGO.GetComponent<Button>();
        m_formationButtons.Add(rule, buttonComp);

        TextMeshProUGUI textComp = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
        textComp.text = rule.name;
    }
}

        
