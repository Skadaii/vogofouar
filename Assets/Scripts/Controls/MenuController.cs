﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using System.Linq;
using System.Collections.ObjectModel;
using TMPro;

public class MenuController : MonoBehaviour
{
    [SerializeField]
    private string[] m_instancedFormationDirectories = null;

    private List<FormationRule> m_instancedRules = new List<FormationRule>();

    [SerializeField]
    private GameObject m_buttonFormationSelectionPrefab = null;
    [SerializeField]
    private Transform m_factoryMenuCanvas = null;

    private UnitController m_controller = null;
    private GameObject m_factoryMenuPanel = null;
    private Text m_buildPointsText = null;
    private Text m_capturedTargetsText = null;
    private Button[] m_buildUnitButtons = null;
    private Button[] m_buildFactoryButtons = null;
    private Button m_cancelBuildButton = null;
    private Text[] m_buildQueueTexts = null;
    private Button[] m_formationButtons = null;

    public GraphicRaycaster BuildMenuRaycaster { get; private set; }

    public void HideFactoryMenu()
    {
        if (m_factoryMenuPanel)
            m_factoryMenuPanel.SetActive(false);
    }
    public void ShowFactoryMenu()
    {
        if (m_factoryMenuPanel)
            m_factoryMenuPanel.SetActive(true);
    }
    public void UpdateBuildPointsUI()
    {
        if (m_buildPointsText != null)
            m_buildPointsText.text = "Build Points : " + m_controller.TotalBuildPoints;
    }
    public void UpdateCapturedTargetsUI()
    {
        if (m_capturedTargetsText != null)
            m_capturedTargetsText.text = "Captured Targets : " + m_controller.CapturedTargets;
    }
    public void UpdateFactoryBuildQueueUI(int i, Factory selectedFactory)
    {
        if (selectedFactory == null)
            return;
        int queueCount = selectedFactory.GetQueuedCount(i);
        if (queueCount > 0)
        {
            m_buildQueueTexts[i].text = "+" + queueCount;
            m_buildQueueTexts[i].enabled = true;
        }
        else
        {
            m_buildQueueTexts[i].enabled = false;
        }
    }
    public void HideAllFactoryBuildQueue()
    {
        foreach (Text text in m_buildQueueTexts)
        {
            if (text)
                text.enabled = false;
        }
    }
    public void UnregisterBuildButtons(int availableUnitsCount, int availableFactoriesCount)
    {
        // unregister build buttons
        for (int i = 0; i < availableUnitsCount; i++)
        {
            m_buildUnitButtons[i].onClick.RemoveAllListeners();
        }
        for (int i = 0; i < availableFactoriesCount; i++)
        {
            m_buildFactoryButtons[i].onClick.RemoveAllListeners();
        }
    }

    public void UpdateFactoryMenu(Factory selectedFactory, Func<int, bool> requestUnitBuildMethod, Action<int> enterFactoryBuildModeMethod)
    {
        ShowFactoryMenu();

        // Unit build buttons
        // register available buttons
        int i = 0;
        for (; i < selectedFactory.AvailableUnitsCount; i++)
        {
            m_buildUnitButtons[i].gameObject.SetActive(true);

            int index = i; // capture index value for event closure
            m_buildUnitButtons[i].onClick.AddListener(() =>
            {
                if (requestUnitBuildMethod(index))
                    UpdateFactoryBuildQueueUI(index, selectedFactory);
            });

            Text[] buttonTextArray = m_buildUnitButtons[i].GetComponentsInChildren<Text>();
            Text buttonText = buttonTextArray[0];//BuildUnitButtons[i].GetComponentInChildren<Text>();
            UnitDataScriptable data = selectedFactory.GetBuildableUnitData(i);
            buttonText.text = data.caption + "(" + data.cost + ")";

            // Update queue count UI
            m_buildQueueTexts[i] = buttonTextArray[1];
            UpdateFactoryBuildQueueUI(i, selectedFactory);
        }
        // hide remaining buttons
        for (; i < m_buildUnitButtons.Length; i++)
        {
            m_buildUnitButtons[i].gameObject.SetActive(false);
        }

        // activate Cancel button
        m_cancelBuildButton.onClick.AddListener(  () =>
                                                {
                                                    selectedFactory?.CancelCurrentBuild();
                                                    HideAllFactoryBuildQueue();
                                                });

        // Factory build buttons
        // register available buttons
        i = 0;
        for (; i < selectedFactory.AvailableFactoriesCount; i++)
        {
            m_buildFactoryButtons[i].gameObject.SetActive(true);

            int index = i; // capture index value for event closure
            m_buildFactoryButtons[i].onClick.AddListener(() =>
            {
                enterFactoryBuildModeMethod(index);
            });

            Text buttonText = m_buildFactoryButtons[i].GetComponentInChildren<Text>();
            FactoryDataScriptable data = selectedFactory.GetBuildableFactoryData(i);
            buttonText.text = data.caption + "(" + data.cost + ")";
        }
        // hide remaining buttons
        for (; i < m_buildFactoryButtons.Length; i++)
        {
            m_buildFactoryButtons[i].gameObject.SetActive(false);
        }
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

    void Awake()
    {
        if (m_factoryMenuCanvas == null)
        {
            Debug.LogWarning("FactoryMenuCanvas not assigned in inspector");
            return;
        }

        m_controller = GetComponent<UnitController>();

        Transform FactoryMenuPanelTransform = m_factoryMenuCanvas.Find("FactoryMenu_Panel");
        if (FactoryMenuPanelTransform)
        {
            m_factoryMenuPanel = FactoryMenuPanelTransform.gameObject;
            m_factoryMenuPanel.SetActive(false);
        }
        BuildMenuRaycaster = m_factoryMenuCanvas.GetComponent<GraphicRaycaster>();
        Transform BuildPointsTextTransform = m_factoryMenuCanvas.Find("BuildPointsText");
        if (BuildPointsTextTransform)
        {
            m_buildPointsText = BuildPointsTextTransform.GetComponent<Text>();
        }
        Transform CapturedTargetsTextTransform = m_factoryMenuCanvas.Find("CapturedTargetsText");
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
        Transform ContentTransform = m_factoryMenuCanvas.Find("FormationMenu_Panel").Find("Scroll View").Find("Viewport").Find("Content");

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

        
