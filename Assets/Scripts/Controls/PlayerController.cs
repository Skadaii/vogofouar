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
        Orders,
        FactoryPositioning
    }

    [SerializeField]
    private GameObject m_targetCursorPrefab = null;
    [SerializeField]
    private float m_targetCursorFloorOffset = 0.2f;
    [SerializeField]
    private EventSystem m_sceneEventSystem = null;

    [SerializeField, Range(0f, 1f)]
    private float m_factoryPreviewTransparency = 0.3f;

    [SerializeField, Range(0.005f, 1)]
    private float m_selectionLineWidth = 0.1f;

    private PointerEventData m_menuPointerEventData = null;

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
    private LineRenderer m_selectionLineRenderer;
    private GameObject m_targetCursor = null;

    // Factory build
    private InputMode m_currentInputMode = InputMode.Orders;
    private int m_wantedFactoryId = 0;
    private GameObject m_wantedFactoryPreview = null;
    private Shader m_previewShader = null;

    // Mouse events
    private Action m_onMouseLeftPressed = null;
    private Action m_onMouseLeft = null;
    private Action m_onMouseLeftReleased = null;
    private Action m_onUnitActionStart = null;
    private Action m_onUnitActionEnd = null;
    private Action m_onCameraDragMoveStart = null;
    private Action m_onCameraDragMoveEnd = null;

    private Action<Vector3> m_onFactoryPositioned = null;
    private Action<float> m_onCameraZoom = null;
    private Action<float> m_onCameraMoveHorizontal = null;
    private Action<float> m_onCameraMoveVertical = null;

    // Keyboard events
    private Action m_onFocusBasePressed = null;
    private Action m_onCancelBuildPressed = null;
    private Action m_onDestroyEntityPressed = null;
    private Action m_onCancelFactoryPositioning = null;
    private Action m_onSelectAllPressed = null;
    private Action[] m_onCategoryPressed = new Action[9];

    private GameObject GetTargetCursor()
    {
        if (m_targetCursor == null)
        {
            m_targetCursor = Instantiate(m_targetCursorPrefab);
            m_targetCursor.name = m_targetCursor.name.Replace("(Clone)", "");
        }
        return m_targetCursor;
    }
    private void SetTargetCursorPosition(Vector3 pos)
    {
        SetTargetCursorVisible(true);
        pos.y += m_targetCursorFloorOffset;
        GetTargetCursor().transform.position = pos;
    }
    private void SetTargetCursorVisible(bool isVisible)
    {
        GetTargetCursor().SetActive(isVisible);
    }
    private void SetCameraFocusOnMainFactory()
    {
        if (m_factoryList.Count > 0)
        {
            m_cameraPlayer.FocusEntity(m_factoryList[0]);
        }
    }
    private void CancelCurrentBuild()
    {
        m_selectedFactory?.CancelCurrentBuild();
        m_playerMenuController.HideAllFactoryBuildQueue();
    }


    #region MonoBehaviour methods
    
    protected override void Awake()
    {
        base.Awake();

        m_playerMenuController = GetComponent<MenuController>();
        if (m_playerMenuController == null)
            Debug.LogWarning("could not find MenuController component !");

        m_onBuildPointsUpdated += m_playerMenuController.UpdateBuildPointsUI;
        m_onCaptureTarget += m_playerMenuController.UpdateCapturedTargetsUI;

        m_cameraPlayer = Camera.main.GetComponent<PlayerCamera>();

        m_selectionLineRenderer = GetComponent<LineRenderer>();
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

        m_previewShader = Shader.Find("Legacy Shaders/Transparent/Diffuse");

        // left click : selection
        m_onMouseLeftPressed += StartSelection;
        m_onMouseLeft += UpdateSelection;
        m_onMouseLeftReleased += EndSelection;

        // right click : Unit actions (move / attack / capture ...)
        m_onUnitActionEnd += ComputeUnitsAction;

        // Camera movement
        // middle click : camera movement
        m_onCameraDragMoveStart += StartMoveCamera;
        m_onCameraDragMoveEnd += StopMoveCamera;

        m_onCameraZoom += m_cameraPlayer.Zoom;
        m_onCameraMoveHorizontal += m_cameraPlayer.KeyboardMoveHorizontal;
        m_onCameraMoveVertical += m_cameraPlayer.KeyboardMoveVertical;

        // Gameplay shortcuts
        m_onFocusBasePressed += SetCameraFocusOnMainFactory;
        m_onCancelBuildPressed += CancelCurrentBuild;

        m_onCancelFactoryPositioning += ExitFactoryBuildMode;

        m_onFactoryPositioned += (floorPos) =>
        {
            if (RequestFactoryBuild(m_wantedFactoryId, floorPos))
            {
                ExitFactoryBuildMode();
            }
        };

        // Destroy selected unit command
        m_onDestroyEntityPressed += () =>
        {
            Unit[] unitsToBeDestroyed = m_selectedUnitList.ToArray();
            foreach (Unit unit in unitsToBeDestroyed)
            {
                (unit as IDamageable).Destroy();
            }

            if (m_selectedFactory)
            {
                Factory factoryRef = m_selectedFactory;
                UnselectCurrentFactory();
                factoryRef.Destroy();
            }
        };

        // Selection shortcuts
        m_onSelectAllPressed += SelectAllUnits;

        for(int i = 0; i < m_onCategoryPressed.Length; i++)
        {
            // store typeId value for event closure
            int typeId = i;
            m_onCategoryPressed[i] += () =>
            {
                SelectAllUnitsByTypeId(typeId);
            };
        }
    }

    protected override void Update()
    {
        switch (m_currentInputMode)
        {
            case InputMode.FactoryPositioning:
                UpdateFactoryPositioningInput();
                break;
            case InputMode.Orders:
                UpdateSelectionInput();
                UpdateActionInput();
                break;
        }

        UpdateCameraInput();

        // Apply camera movement
        UpdateMoveCamera();
    }
    
    #endregion


    #region Update methods
    private void UpdateFactoryPositioningInput()
    {
        Vector3 floorPos = ProjectFactoryPreviewOnFloor();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            m_onCancelFactoryPositioning?.Invoke();
        }
        if (Input.GetMouseButtonDown(0))
        {
            m_onFactoryPositioned?.Invoke(floorPos);
        }
    }
    private void UpdateSelectionInput()
    {
        // Update keyboard inputs

        if (Input.GetKeyDown(KeyCode.A))
            m_onSelectAllPressed?.Invoke();

        for (int i = 0; i < m_onCategoryPressed.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Keypad1 + i) || Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                m_onCategoryPressed[i]?.Invoke();
                break;
            }
        }

        // Update mouse inputs
#if UNITY_EDITOR
        if (EditorWindow.focusedWindow != EditorWindow.mouseOverWindow)
            return;
#endif
        if (Input.GetMouseButtonDown(0))
            m_onMouseLeftPressed?.Invoke();
        if (Input.GetMouseButton(0))
            m_onMouseLeft?.Invoke();
        if (Input.GetMouseButtonUp(0))
            m_onMouseLeftReleased?.Invoke();

    }
    private void UpdateActionInput()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
            m_onDestroyEntityPressed?.Invoke();

        // cancel build
        if (Input.GetKeyDown(KeyCode.C))
            m_onCancelBuildPressed?.Invoke();

        // Contextual unit actions (attack / capture ...)
        if (Input.GetMouseButtonDown(1))
            m_onUnitActionStart?.Invoke();
        if (Input.GetMouseButtonUp(1))
            m_onUnitActionEnd?.Invoke();
    }
    private void UpdateCameraInput()
    {
        // Camera focus

        if (Input.GetKeyDown(KeyCode.F))
            m_onFocusBasePressed?.Invoke();

        // Camera movement inputs

        // keyboard move (arrows)
        float hValue = Input.GetAxis("Horizontal");
        if (hValue != 0)
            m_onCameraMoveHorizontal?.Invoke(hValue);
        float vValue = Input.GetAxis("Vertical");
        if (vValue != 0)
            m_onCameraMoveVertical?.Invoke(vValue);

        // zoom in / out (ScrollWheel)
        float scrollValue = Input.GetAxis("Mouse ScrollWheel");
        if (scrollValue != 0)
            m_onCameraZoom?.Invoke(scrollValue);

        // drag move (mouse button)
        if (Input.GetMouseButtonDown(2))
            m_onCameraDragMoveStart?.Invoke();
        if (Input.GetMouseButtonUp(2))
            m_onCameraDragMoveEnd?.Invoke();
    }
    #endregion


    #region Unit selection methods
    private void StartSelection()
    {
        // Hide target cursor
        SetTargetCursorVisible(false);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        int factoryMask = 1 << LayerMask.NameToLayer("Factory");
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

        RaycastHit raycastInfo;
        // factory selection
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, factoryMask))
        {
            Factory factory = raycastInfo.transform.GetComponent<Factory>();
            if (factory != null)
            {
                if (factory.Team == m_team && m_selectedFactory != factory)
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
            m_selectionStart.y = 0.0f;//raycastInfo.point.y + 1f;
            m_selectionStart.z = raycastInfo.point.z;
        }
    }

    /*
     * Multi selection methods
     */
    private void UpdateSelection()
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

        m_selectionLineRenderer.SetPosition(0, new Vector3(m_selectionStart.x, m_selectionStart.y, m_selectionStart.z));
        m_selectionLineRenderer.SetPosition(1, new Vector3(m_selectionStart.x, m_selectionStart.y, m_selectionEnd.z));
        m_selectionLineRenderer.SetPosition(2, new Vector3(m_selectionEnd.x, m_selectionStart.y, m_selectionEnd.z));
        m_selectionLineRenderer.SetPosition(3, new Vector3(m_selectionEnd.x, m_selectionStart.y, m_selectionStart.z));
    }
    private void EndSelection()
    {
        if (m_selectionStarted == false)
            return;

        UpdateSelection();
        m_selectionLineRenderer.enabled = false;
        Vector3 center = (m_selectionStart + m_selectionEnd) / 2f;
        Vector3 size = Vector3.up * m_selectionBoxHeight + m_selectionEnd - m_selectionStart;
        size.x = Mathf.Abs(size.x);
        size.y = Mathf.Abs(size.y);
        size.z = Mathf.Abs(size.z);

        UnselectAllUnits();
        UnselectCurrentFactory();

        int unitLayerMask = 1 << LayerMask.NameToLayer("Unit");
        int factoryLayerMask = 1 << LayerMask.NameToLayer("Factory");
        Collider[] colliders = Physics.OverlapBox(center, size / 2f, Quaternion.identity, unitLayerMask | factoryLayerMask, QueryTriggerInteraction.Ignore);
        foreach (Collider col in colliders)
        {
            //Debug.Log("collider name = " + col.gameObject.name);
            ISelectable selectedEntity = col.transform.GetComponent<ISelectable>();
            if (selectedEntity.Team == Team)
            {
                if (selectedEntity is Unit)
                {
                    SelectUnit((selectedEntity as Unit));
                }
                else if (selectedEntity is Factory)
                {
                    // Select only one factory at a time
                    if (m_selectedFactory == null)
                        SelectFactory(selectedEntity as Factory);
                }
            }
        }

        m_selectionStarted = false;
        m_selectionStart = Vector3.zero;
        m_selectionEnd = Vector3.zero;
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


    #region Factory / build methods
    public void UpdateFactoryBuildQueueUI(int entityIndex)
    {
        m_playerMenuController.UpdateFactoryBuildQueueUI(entityIndex, m_selectedFactory);
    }
    protected override void SelectFactory(Factory factory)
    {
        if (factory == null || factory.IsUnderConstruction)
            return;

        base.SelectFactory(factory);

        m_playerMenuController.UpdateFactoryMenu(m_selectedFactory, RequestUnitBuild, EnterFactoryBuildMode);
    }
    protected override void UnselectCurrentFactory()
    {
        //Debug.Log("UnselectCurrentFactory");

        if (m_selectedFactory)
        {
            m_playerMenuController.UnregisterBuildButtons(m_selectedFactory.AvailableUnitsCount, m_selectedFactory.AvailableFactoriesCount);
        }

        m_playerMenuController.HideFactoryMenu();

        base.UnselectCurrentFactory();
    }
    private void EnterFactoryBuildMode(int factoryId)
    {
        if (m_selectedFactory.GetFactoryCost(factoryId) > TotalBuildPoints)
            return;

        //Debug.Log("EnterFactoryBuildMode");

        m_currentInputMode = InputMode.FactoryPositioning;

        m_wantedFactoryId = factoryId;

        // Create factory preview

        // Load factory prefab for preview
        GameObject factoryPrefab = m_selectedFactory.GetFactoryPrefab(factoryId);
        if (factoryPrefab == null)
        {
            Debug.LogWarning("Invalid factory prefab for factoryId " + factoryId);
        }
        m_wantedFactoryPreview = Instantiate(factoryPrefab.transform.GetChild(0).gameObject); // Quick and dirty access to mesh GameObject
        m_wantedFactoryPreview.name = m_wantedFactoryPreview.name.Replace("(Clone)", "_Preview");
        // Set transparency on materials
        foreach(Renderer rend in m_wantedFactoryPreview.GetComponentsInChildren<MeshRenderer>())
        {
            Material mat = rend.material;
            mat.shader = m_previewShader;
            Color col = mat.color;
            col.a = m_factoryPreviewTransparency;
            mat.color = col;
        }

        // Project mouse position on ground to position factory preview
        ProjectFactoryPreviewOnFloor();
    }

    private void ExitFactoryBuildMode()
    {
        m_currentInputMode = InputMode.Orders;
        Destroy(m_wantedFactoryPreview);
    }

    private Vector3 ProjectFactoryPreviewOnFloor()
    {
        if (m_currentInputMode == InputMode.Orders)
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
            m_wantedFactoryPreview.transform.position = floorPos;
        }
        return floorPos;
    }

    #endregion


    #region Entity targetting (attack / capture) and movement methods
    private void ComputeUnitsAction()
    {
        if (m_selectedUnitList.Count == 0)
            return;

        int damageableMask = (1 << LayerMask.NameToLayer("Unit")) | (1 << LayerMask.NameToLayer("Factory"));
        int targetMask = 1 << LayerMask.NameToLayer("Target");
        int floorMask = 1 << LayerMask.NameToLayer("Floor");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit raycastInfo;

        // Set unit / factory attack target
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, damageableMask))
        {
            Entity other = raycastInfo.transform.GetComponent<Entity>();
            if (other != null)
            {
                if (other.Team != Team)
                {
                    // Direct call to attacking task $$$ to be improved by AI behaviour
                    foreach (Fighter unit in m_selectedUnitList)
                        if(unit != null)
                            unit.SetAttackTarget(other);
                }
                else if (other.NeedsRepairing())
                {
                    // Direct call to reparing task $$$ to be improved by AI behaviour
                    foreach (Builder unit in m_selectedUnitList)
                        if (unit != null)
                            unit.SetRepairTarget(other);
                }
            }
        }
        // Set capturing target
        else if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, targetMask))
        {
            StaticBuilding target = raycastInfo.transform.GetComponent<StaticBuilding>();
            if (target != null && target.Team != Team)
            {
                // Direct call to capturing task $$$ to be improved by AI behaviour
                foreach (Builder unit in m_selectedUnitList)
                    if (unit != null)
                        unit.SetCaptureTarget(target);
            }
        }
        // Set unit move target
        else if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorMask))
        {
            Vector3 newPos = raycastInfo.point;
            SetTargetCursorPosition(newPos);

            MoveUnits(m_selectedUnitList, newPos);
        }
    }
    private void MoveUnits(List<Unit> units, Vector3 squadTarget)
    {
        if (m_selectedUnitList.Count == 1)
        {
            Unit unitToMove = m_selectedUnitList.First();
            unitToMove.Squad = null;
            unitToMove.MoveTo(squadTarget);
            return;
        }

        UnitSquad newSquad = CreateDynamicSquad(m_selectedUnitList);

        newSquad.m_leaderComponent.MoveTo(squadTarget);
    }

    #endregion


    #region Camera methods

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
