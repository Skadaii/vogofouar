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
    private Button[] m_buildFactoryButtons = null;
    private Button m_cancelBuildButton = null;
    private Text[] m_buildQueueTexts = null;

    //  Formations

    [Header("Formations")]
    [SerializeField] private FormationRule[] m_availableFormations = null;
    [SerializeField] private GameObject m_buttonFormationSelectionPrefab = null;

    private Button[] m_formationButtons = null;

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
        m_buildFactoryButtons = m_factoryMenuPanel.transform.Find("BuildFactoryMenu_Panel").GetComponentsInChildren<Button>();
        m_cancelBuildButton = m_factoryMenuPanel.transform.Find("Cancel_Button").GetComponent<Button>();
        m_buildQueueTexts = new Text[m_buildUnitButtons.Length];

        LoadAvailableRules();
        InitializeInstanceViewport();
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

        for (int i = 0; i < m_formationButtons.Length - 1; i++)
        {
            Button button = m_formationButtons[i];

            button.onClick.RemoveAllListeners();
        }
    }

    public void UpdateFormationMenu(List<Unit> selectedUnit, Action<List<Unit>, FormationRule> setSquadMethod)
    {
        Unit firstUnit = selectedUnit.First();

        bool isMixed = selectedUnit.Any(u => u.Squad != firstUnit.Squad);

        for (int i = 0; i < m_formationButtons.Length; i++)
        {
            Button button = m_formationButtons[i];
            FormationRule currentFormation = m_instancedRules[i];
            button.onClick.AddListener(() => setSquadMethod(selectedUnit, currentFormation));
        }
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

    private void InitializeInstanceViewport()
    {
        Transform ContentTransform = m_playerUI.Find("FormationMenu_Panel").Find("Scroll View").Find("Viewport").Find("Content");

        m_formationButtons = new Button[m_instancedRules.Count];
        for (int i = 0; i < m_instancedRules.Count; i++)
        {
            GameObject buttonGO = Instantiate(m_buttonFormationSelectionPrefab, ContentTransform);

            Button buttonComp = buttonGO.GetComponent<Button>();
            m_formationButtons[i] = buttonComp;

            TextMeshProUGUI textComp = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            textComp.text = m_instancedRules[i].name;
        }
    }
}

        
