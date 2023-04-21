using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
public sealed class PlayerController : UnitController
{
    public enum InputMode
    {
        None,
        UnitSelected,
        BuildingSelected,
        BuildMode
    }

    [SerializeField] private GameObject m_targetCursorPrefab = null;
    [SerializeField] private float m_targetCursorFloorOffset = 0.2f;
    [SerializeField] private EventSystem m_sceneEventSystem = null;
    [SerializeField] private float m_wheelMenuMinDelay = 0.5f;
    [SerializeField] private WheelMenu m_wheelMenu = null;

    [SerializeField, Range(0f, 1f)] private float m_factoryPreviewTransparency = 0.3f;

    [SerializeField, Range(1f, 25f)] private float m_selectionLineWidth = 3.5f;

    private PointerEventData m_menuPointerEventData = null;

    private float m_rightMouseButtonDownElapsedTime = 0f;

    // Build Menu UI
    private MenuController m_playerMenuController;

    // Camera
    private PlayerCamera m_cameraPlayer = null;
    private bool m_canMoveCamera = false;
    private Vector2 m_cameraInputPos = Vector2.zero;
    private Vector2 m_cameraPrevInputPos = Vector2.zero;
    private Vector2 m_cameraFrameMove = Vector2.zero;

    // Selection
    private Vector3 m_selectionStart = Vector3.zero;
    private Vector3 m_selectionEnd = Vector3.zero;
    private bool m_selectionStarted = false;
    private float m_selectionBoxHeight = 50f;
    private GameObject m_targetCursor = null;

    private LineRenderer m_selectionLineRenderer;

    // Factory build
    private InputMode m_currentInputMode = InputMode.None;
    private int m_wantedFactoryId = 0;
    private GameObject m_wantedBuildingPreview = null;
    private GameObject m_wantedbuilding = null;
    private Shader m_previewShader = null;


    //  Properties

    private GameObject TargetCursor
    {
        get
        {
            if (m_targetCursor == null)
            {
                m_targetCursor = Instantiate(m_targetCursorPrefab);
                m_targetCursor.name = m_targetCursor.name.Replace("(Clone)", "");
            }
            return m_targetCursor;
        }
    }

    #region MonoBehaviour methods
    
    protected override void Awake()
    {
        base.Awake();

        m_playerMenuController = GetComponent<MenuController>();
        if (m_playerMenuController == null)
            Debug.LogWarning("could not find MenuController component !");

        m_onResourceUpdated += m_playerMenuController.UpdateBuildPointsUI;
        m_onCaptureTarget += m_playerMenuController.UpdateCapturedTargetsUI;

        m_cameraPlayer = Camera.main.GetComponent<PlayerCamera>();

        m_selectionLineRenderer = GetComponentInChildren<LineRenderer>();
        m_selectionLineRenderer.startWidth = m_selectionLineRenderer.endWidth = m_selectionLineWidth;

        m_playerMenuController = GetComponent<MenuController>();
       
        if (m_sceneEventSystem == null)
        {
            Debug.LogWarning("EventSystem not assigned in PlayerController, searching in current scene...");
            m_sceneEventSystem = FindObjectOfType<EventSystem>();
        }
        // Set up the new Pointer Event
        m_menuPointerEventData = new PointerEventData(m_sceneEventSystem);
    }

    protected override void Start()
    {
        base.Start();

        SetCameraFocusOnMainFactory(false);

        m_previewShader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
    }

    protected override void Update()
    {
        CheckInputModeStates();

        switch (m_currentInputMode)
        {
            case InputMode.BuildMode:        BuildModeUpdate(); break;
            case InputMode.UnitSelected:     UnitSelectedUpdate(); break;
            case InputMode.BuildingSelected: BuildingSelectedUpdate(); break;
            default:    DefaultUpdate(); break;
        }

        //  Global updates

        CameraUpdate();
    }

    #endregion

    #region Mode Functions

    private void ChangeMode(InputMode newMode)
    {
        if (m_currentInputMode == newMode) return;

        switch (m_currentInputMode)
        {
            case InputMode.BuildMode:        ExitBuildMode(); break;
            case InputMode.UnitSelected:     ExitUnitSelectedMode(); break;
            case InputMode.BuildingSelected: ExitBuildingSelectedMode(); break;

            default: break;
        }

        switch (newMode)
        {
            case InputMode.BuildMode:        EnterBuildMode(); break;
            case InputMode.UnitSelected:     EnterUnitSelectedMode(); break;
            case InputMode.BuildingSelected: EnterBuildingSelectedMode(); break;
            default: m_currentInputMode = InputMode.None; break;
        }
    }


    private void CheckInputModeStates()
    {
        if(m_wantedbuilding != null && HasSelectedUnits)
            ChangeMode(InputMode.BuildMode);
        else if (HasSelectedUnits)
            ChangeMode(InputMode.UnitSelected);
        else if (HasSelectedBuildings)
            ChangeMode(InputMode.BuildingSelected);
        else
            ChangeMode(InputMode.None);
    }

    #endregion

    private void DefaultUpdate()
    {
        SelectionUpdate();
        UpdateActions();
    }

    private void UpdateActions()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            Unit[] unitsToBeDestroyed = m_selectedUnitList.ToArray();
            foreach (Unit unit in unitsToBeDestroyed)
            {
                (unit as IDamageable).Destroy();
            }

            if (m_selectedBuildings)
            {
                Factory factoryRef = m_selectedBuildings;
                UnselectCurrentFactory();
                factoryRef.Destroy();
            }
        }

        // cancel build
        //if (Input.GetKeyDown(KeyCode.C)) CancelCurrentBuild();
    }

    private void SetTargetCursorPosition(Vector3 pos)
    {
        SetTargetCursorVisible(true);
        pos.y += m_targetCursorFloorOffset;
        TargetCursor.transform.position = pos;
    }
    private void SetTargetCursorVisible(bool isVisible)
    {
        TargetCursor.SetActive(isVisible);
    }
    private void SetCameraFocusOnMainFactory(bool smooth = true)
    {
        if (m_buildingList.Count > 0)
        {
            m_cameraPlayer.FocusEntity(m_buildingList[0], smooth);
        }
    }
    //private void CancelCurrentBuild()
    //{
    //    m_selectedBuildings?.CancelCurrentUnitProduction();
    //    //m_playerMenuController.HideAllFactoryBuildQueue();
    //}


    #region Selection methods

    private void SelectionUpdate()
    {
        // Update keyboard inputs

        if (Input.GetKeyDown(KeyCode.A)) SelectAllUnits();

        // Update mouse inputs
#if UNITY_EDITOR
        if (EditorWindow.focusedWindow != EditorWindow.mouseOverWindow) return;
#endif

        if (Input.GetMouseButtonDown(0)) StartSelection();
        if (Input.GetMouseButtonUp(0)) EndSelection();
        if (Input.GetMouseButton(0)) UpdateSelectionRect();
    }

    private void StartSelection()
    {
        // Hide target cursor
        SetTargetCursorVisible(false);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        int uiMask = 1 << LayerMask.NameToLayer("UI");
        int buildingMask = 1 << LayerMask.NameToLayer("Building");
        int unitMask = 1 << LayerMask.NameToLayer("Unit");
        int floorMask = 1 << LayerMask.NameToLayer("Floor");

        // *** Ignore Unit selection when clicking on UI ***
        // Set the Pointer Event Position to that of the mouse position
        m_menuPointerEventData.position = Input.mousePosition;

        //Create a list of Raycast Results
        List<RaycastResult> results = new List<RaycastResult>();
        m_playerMenuController.BuildMenuRaycaster.Raycast(m_menuPointerEventData, results);
        if (results.Count > 0)
            return;

        if (EventSystem.current.IsPointerOverGameObject()) return;

        RaycastHit raycastInfo;
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, uiMask)) return;

            // factory selection
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, buildingMask))
        {
            Factory factory = raycastInfo.transform.GetComponent<Factory>();
            if (factory != null)
            {
                if (factory.Team == m_team && m_selectedBuildings != factory)
                {
                    UnselectCurrentFactory();
                    SelectFactory(factory);
                }
            }
        }
        // unit selection / unselection
        else if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, unitMask))
        {
            bool isShiftBtPressed = Input.GetKey(KeyCode.LeftShift);
            bool isCtrlBtPressed = Input.GetKey(KeyCode.LeftControl);

            UnselectCurrentFactory();

            Unit selectedUnit = raycastInfo.transform.GetComponent<Unit>();
            if (selectedUnit != null && selectedUnit.Team == m_team)
            {
                if (isShiftBtPressed)
                {
                    UnselectUnit(selectedUnit);
                }
                else if (isCtrlBtPressed)
                {
                    SelectUnit(selectedUnit);
                }
                else
                {
                    UnselectAllUnits();
                    SelectUnit(selectedUnit);
                }
            }
        }
        else if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorMask))
        {
            UnselectCurrentFactory();
            m_selectionLineRenderer.enabled = true;

            m_selectionStarted = true;

            m_selectionStart.x = raycastInfo.point.x;
            m_selectionStart.y = 0.0f;
            m_selectionStart.z = raycastInfo.point.z;
        }
    }

    private void EndSelection()
    {
        if (m_selectionStarted == false)
            return;

        UpdateSelectionRect();
        m_selectionLineRenderer.enabled = false;
        Vector3 center = (m_selectionStart + m_selectionEnd) / 2f;
        Vector3 size = Vector3.up * m_selectionBoxHeight + m_selectionEnd - m_selectionStart;
        size.x = Mathf.Abs(size.x);
        size.y = Mathf.Abs(size.y);
        size.z = Mathf.Abs(size.z);

        UnselectAllUnits();
        UnselectCurrentFactory();

        int unitLayerMask = 1 << LayerMask.NameToLayer("Unit");
        int factoryLayerMask = 1 << LayerMask.NameToLayer("Building");
        Collider[] colliders = Physics.OverlapBox(center, size / 2f, Quaternion.identity, unitLayerMask | factoryLayerMask, QueryTriggerInteraction.Ignore);

        List<Unit> multiSelectedUnits = new List<Unit>();
        foreach (Collider col in colliders)
        {
            //Debug.Log("collider name = " + col.gameObject.name);
            ISelectable selectedEntity = col.transform.GetComponent<ISelectable>();
            if (selectedEntity.Team == Team)
            {
                if (selectedEntity is Unit)
                {
                    multiSelectedUnits.Add(selectedEntity as Unit);
                }

                //  Make it impossible to select buildings and units at the same time (for now)
                else if (selectedEntity is Factory && multiSelectedUnits.Count == 0)
                {
                    // Select only one factory at a time
                    if (m_selectedBuildings == null)
                        SelectFactory(selectedEntity as Factory);
                }
            }
        }

        if(multiSelectedUnits.Count > 0)
        {
            SelectUnitList(multiSelectedUnits);
            UnselectCurrentFactory();
        }

        m_selectionStarted = false;
        m_selectionStart = Vector3.zero;
        m_selectionEnd = Vector3.zero;
    }

    private void UpdateSelectionRect()
    {
        if (m_selectionStarted == false)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int floorMask = 1 << LayerMask.NameToLayer("Floor");

        RaycastHit raycastInfo;
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorMask))
        {
            m_selectionEnd = raycastInfo.point;
        }

        RenderSelectionRect();
    }

    private void RenderSelectionRect()
    {
        float width = 2f * m_cameraPlayer.Distance * Mathf.Tan(m_cameraPlayer.FOV * Mathf.Deg2Rad * 0.5f) * (m_selectionLineWidth / Screen.width);
        m_selectionLineRenderer.startWidth = m_selectionLineRenderer.endWidth = width;
        m_selectionLineRenderer.SetPosition(0, new Vector3(m_selectionStart.x, m_selectionStart.y, m_selectionStart.z));
        m_selectionLineRenderer.SetPosition(1, new Vector3(m_selectionStart.x, m_selectionStart.y, m_selectionEnd.z));
        m_selectionLineRenderer.SetPosition(2, new Vector3(m_selectionEnd.x, m_selectionStart.y, m_selectionEnd.z));
        m_selectionLineRenderer.SetPosition(3, new Vector3(m_selectionEnd.x, m_selectionStart.y, m_selectionStart.z));
    }

    protected override void OnUnitSelected()
    {
        m_playerMenuController.UpdateFormationMenu(m_selectedUnitList, SetSquadFormation);
    }

    protected override void OnUnitUnselected()
    {
        m_playerMenuController.UpdateFormationMenu(m_selectedUnitList, SetSquadFormation);
    }


    #endregion


    #region Factory Selected Mode

    private void BuildingSelectedUpdate()
    {
        DefaultUpdate();

        if (m_wheelMenu.isActiveAndEnabled && HasSelectedBuildings)
        {
            if (Input.GetMouseButtonDown(0)) ValidateBuildingCommandWheel();
        }
    }


    private void EnterBuildingSelectedMode()
    {
        m_currentInputMode = InputMode.BuildingSelected;
    }


    private void ExitBuildingSelectedMode()
    {
        if (m_wheelMenu.isActiveAndEnabled)
        {
            m_wheelMenu.Disappear();
        }
    }

    protected override void SelectFactory(Factory factory)
    {
        if (factory == null || factory.IsUnderConstruction)
            return;

        base.SelectFactory(factory);

        ChangeMode(InputMode.BuildingSelected);
        m_wheelMenu.SetBuildingWheel(factory);
    }

    protected override void UnselectCurrentFactory()
    {
        //if (m_selectedBuildings)
        //{
        //    m_playerMenuController.UnregisterBuildButtons(m_selectedBuildings.AvailableUnitsCount);
        //}

        //m_playerMenuController.HideFactoryMenu();

        base.UnselectCurrentFactory();
    }
    private void ValidateBuildingCommandWheel()
    {
        m_wheelMenu.ExecuteCommand();
    }

    #endregion


    #region Unit Selected Mode

    private void UnitSelectedUpdate()
    {
        DefaultUpdate();

        if (Input.GetMouseButtonUp(0) && m_wheelMenu.isActiveAndEnabled) ValidateUnitCommandWheel();

        //  Right mouse 
        if (Input.GetMouseButton(1)) HandleRightMouseOnUnitSelected();
        if (Input.GetMouseButtonUp(1)) HandleRightMouseReleaseOnUnitSelected();
    }


    private void EnterUnitSelectedMode()
    {
        m_currentInputMode = InputMode.UnitSelected;
    }

    private void ExitUnitSelectedMode()
    {
        if (m_wheelMenu.isActiveAndEnabled)
        {
            m_wheelMenu.Disappear();
        }
    }

    private void HandleRightMouseOnUnitSelected()
    {
        m_rightMouseButtonDownElapsedTime += Time.deltaTime;

        if (m_rightMouseButtonDownElapsedTime >= m_wheelMenuMinDelay && !m_wheelMenu.isActiveAndEnabled)
        {
            ShowUnitCommandWheel();
        }
    }

    private void HandleRightMouseReleaseOnUnitSelected()
    {
        if(m_rightMouseButtonDownElapsedTime < m_wheelMenuMinDelay)
        {
            ComputeUnitsAction();
        }
        else
        {
            ValidateUnitCommandWheel();
        }

        m_rightMouseButtonDownElapsedTime = 0f;
    }

    private void ShowUnitCommandWheel()
    {
        if (m_selectedUnitList.Count == 0)
            return;

        int entityMask = (1 << LayerMask.NameToLayer("Unit")) | (1 << LayerMask.NameToLayer("Building"));
        int floorMask = 1 << LayerMask.NameToLayer("Floor");

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit raycastInfo;

        // Set unit / factory attack target
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, entityMask))
        {
            if (raycastInfo.transform.TryGetComponent(out Entity other))
            {
                m_wheelMenu.SetUnitWheel(m_selectedUnitList, other);       
            }
        }
        // Set unit move target
        else if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorMask))
        {
            Vector3 newPos = raycastInfo.point;
            SetTargetCursorPosition(newPos);

            m_wheelMenu.SetUnitWheel(m_selectedUnitList, newPos);
        }
    }

    private void ValidateUnitCommandWheel()
    {
        m_wheelMenu.ExecuteCommand();
        m_wheelMenu.Disappear();
    }

    private void ComputeUnitsAction()
    {
        if (m_selectedUnitList.Count == 0)
            return;

        int entityMask = (1 << LayerMask.NameToLayer("Unit")) | (1 << LayerMask.NameToLayer("Building"));
        int floorMask = 1 << LayerMask.NameToLayer("Floor");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit raycastInfo;

        // Set unit / factory attack target
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, entityMask))
        {
            if (raycastInfo.transform.TryGetComponent(out Entity other))
            {
                if (other.Team != Team)
                {
                    UnitSquad newSquad = CreateDynamicSquad(m_selectedUnitList);
                    newSquad.m_leaderComponent.SetTargetPosition(null);
                    newSquad.m_leaderComponent.SetTarget(other);
                }
                else if (other.NeedsRepairing())
                {
                  // Direct call to reparing task $$$ to be improved by AI behaviour
                  foreach (Builder unit in m_selectedUnitList)
                      if (unit != null)
                          unit.Build(other);
                }

            }
        }
        // Set unit move target
        else if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorMask))
        {
            Vector3 newPos = raycastInfo.point;
            SetTargetCursorPosition(newPos);
            MoveUnits(newPos);
        }
    }

    private void MoveUnits(Vector3 squadTarget)
    {
        /*if (m_selectedUnitList.Count == 1)
        {
            Unit unitToMove = m_selectedUnitList.First();
            unitToMove.Squad = null;
            unitToMove.MoveTo(squadTarget);
            return;
        }*/

        UnitSquad newSquad = CreateDynamicSquad(m_selectedUnitList);

        newSquad.m_leaderComponent.SetTargetPosition(squadTarget);
        newSquad.m_leaderComponent.SetTarget(null);
    }

    #endregion


    #region Build Mode

    private void BuildModeUpdate()
    {
        Vector3 floorPos = ProjectBuildingPreview();

        if (Input.GetKeyDown(KeyCode.Escape)) ExitBuildMode();
        if (Input.GetMouseButtonDown(1)) ExitBuildMode();

        if (Input.GetMouseButtonDown(0)) ConstructBuilding(m_wantedbuilding, floorPos);
    }


    private void EnterBuildMode()
    {
        m_currentInputMode = InputMode.BuildMode;

        m_wantedBuildingPreview = Instantiate(m_wantedbuilding.GetComponent<Building>().GFX);
        m_wantedBuildingPreview.name = m_wantedBuildingPreview.name.Replace("(Clone)", "_Preview");

        // Set transparency on materials
        foreach (Renderer rend in m_wantedBuildingPreview.GetComponentsInChildren<MeshRenderer>())
        {
            Material mat = rend.material;
            mat.shader = m_previewShader;
            Color col = mat.color;
            col.a = m_factoryPreviewTransparency;
            mat.color = col;
        }

        // Project mouse position on ground to position factory preview
        ProjectBuildingPreview();
    }

    private void ExitBuildMode()
    {
        m_wantedbuilding = null;

        if (m_wantedBuildingPreview != null)
            Destroy(m_wantedBuildingPreview);
    }

    protected override bool ConstructBuilding(GameObject building, Vector3 position)
    {
        if (base.ConstructBuilding(building, position))
        {
            ExitBuildMode();
            return true;
        }

        return false;
    }

    public void BuildPreview(GameObject building)
    {
        m_wantedbuilding = building;
        ChangeMode(InputMode.BuildMode);
    }

    private Vector3 ProjectBuildingPreview()
    {
        if (m_currentInputMode != InputMode.BuildMode)
        {
            Debug.LogWarning("Wrong call to ProjectFactoryPreviewOnFloor : CurrentInputMode = " + m_currentInputMode.ToString());
            return Vector3.zero;
        }

        Vector3 floorPos = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int floorMask = 1 << LayerMask.NameToLayer("Floor");
        RaycastHit raycastInfo;
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorMask))
        {
            floorPos = raycastInfo.point;
            m_wantedBuildingPreview.transform.position = floorPos;
        }
        return floorPos;
    }

    #endregion


    #region Camera methods

    private void CameraUpdate()
    {
        // Camera focus

        if (Input.GetKeyDown(KeyCode.F)) SetCameraFocusOnMainFactory();

        // Camera movement inputs

        // keyboard move (arrows)
        Vector2 keyboardMove = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        if (keyboardMove.x != 0) m_cameraPlayer?.KeyboardMoveHorizontal(keyboardMove.x);
        if (keyboardMove.y != 0) m_cameraPlayer?.KeyboardMoveVertical(keyboardMove.y);

        // zoom in / out (ScrollWheel)
        float scrollValue = Input.GetAxis("Mouse ScrollWheel");
        if (scrollValue != 0) m_cameraPlayer?.Zoom(scrollValue);

        // drag move (mouse button)
        if (Input.GetMouseButtonDown(2)) StartMoveCamera();
        if (Input.GetMouseButtonUp(2)) StopMoveCamera();

        // Apply camera movement
        UpdateMoveCamera();
    }

    private void StartMoveCamera()
    {
        m_canMoveCamera = true;
        m_cameraInputPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        m_cameraPrevInputPos = m_cameraInputPos;
    }
    private void StopMoveCamera()
    {
        m_canMoveCamera = false;
    }
    private void UpdateMoveCamera()
    {
        if (m_canMoveCamera)
        {
            m_cameraInputPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            m_cameraFrameMove = m_cameraPrevInputPos - m_cameraInputPos;
            m_cameraPlayer.MouseMove(m_cameraFrameMove);
            m_cameraPrevInputPos = m_cameraInputPos;
        }
    }

    #endregion
}
