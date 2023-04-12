using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Factory : Building
{
    //  Internal objects
    //  ----------------

    //  Variables
    //  ---------

    //  Factory variables

    [SerializeField]
    private FactoryDataScriptable m_factoryData = null;

    private int m_requestedEntityBuildIndex = -1;
    private int m_spawnCount = 0;
    /* !! max available unit count in menu is set to 9, available factories count to 3 !! */
    private const int MAX_AVAILABLE_UNITS = 9;

    private const int MaxAvailableFactories = 3;

    private UnitController m_controller = null;

    [SerializeField]
    private int m_maxBuildingQueueSize = 5;
    private Queue<int> m_buildingQueue = new Queue<int>();

    public Action<Unit> OnUnitFormed;
    private bool m_isWorking = false;

    private static List<Command> m_factoryCommands;

    //  Properties
    //  ----------

    public new static Command[] Commands => m_factoryCommands.ToArray().Concat(Entity.Commands) as Command[];
    public override Command[] TypeCommands => Commands;

    public override BuildingDataScriptable BuildingData => m_factoryData;
    public FactoryDataScriptable GetFactoryData { get { return m_factoryData; } }
    public int AvailableUnitsCount { get { return Mathf.Min(MAX_AVAILABLE_UNITS, m_factoryData.availableUnits.Length); } }
    public int AvailableFactoriesCount { get { return Mathf.Min(MaxAvailableFactories, m_factoryData.availableFactories.Length); } }

    //  Functions
    //  ---------

    #region MonoBehaviour methods

    protected override void Awake()
    {
        base.Awake();

        //  Initialize factory commands
        m_factoryCommands ??= new List<Command>
        {
        };

    }

    protected override void Start()
    {
        base.Start();
        m_controller = GameServices.GetControllerByTeam(m_team);
    }

    protected override void Update()
    {
        base.Update();

        if (!m_isCompleted) return;

        if(m_isWorking)
        {
            if (Time.time > m_endBuildDate)
            {
                OnUnitFormed?.Invoke(BuildUnit());
                OnUnitFormed = null; // remove registered methods
                m_isWorking = false;

                // manage build queue : chain with new unit build if necessary
                if (m_buildingQueue.Count != 0)
                {
                    int unitIndex = m_buildingQueue.Dequeue();
                    StartBuildUnit(unitIndex);
                }
            }
            else if (m_hud != null)
                m_hud.Progression = 1f - (m_endBuildDate - Time.time) / m_currentBuildDuration;
        }
    }

    #endregion

    #region Unit building methods
    private bool IsUnitIndexValid(int unitIndex)
    {
        if (unitIndex < 0 || unitIndex >= m_factoryData.availableUnits.Length)
        {
            Debug.LogWarning("Wrong unitIndex " + unitIndex);
            return false;
        }
        return true;
    }
    public UnitDataScriptable GetBuildableUnitData(int unitIndex)
    {
        if (IsUnitIndexValid(unitIndex) == false)
            return null;

        return m_factoryData.availableUnits[unitIndex].GetComponent<Unit>().UnitData;
    }
    public int GetUnitCost(int unitIndex)
    {
        UnitDataScriptable data = GetBuildableUnitData(unitIndex);
        if (data)
            return data.cost;

        return 0;
    }
    public int GetQueuedCount(int unitIndex)
    {
        int counter = 0;
        foreach(int id in m_buildingQueue)
        {
            if (id == unitIndex)
                counter++;
        }
        return counter;
    }
    public bool RequestUnitBuild(int unitMenuIndex)
    {
        int cost = GetUnitCost(unitMenuIndex);
        if (m_controller.TotalBuildPoints < cost || m_buildingQueue.Count >= m_maxBuildingQueueSize)
            return false;

        m_controller.TotalBuildPoints -= cost;

        StartBuildUnit(unitMenuIndex);

        return true;
    }
    private void StartBuildUnit(int unitMenuIndex)
    {
        if (!IsUnitIndexValid(unitMenuIndex))
            return;

        // Build queue
        if (m_isWorking)
        {
            if (m_buildingQueue.Count < m_maxBuildingQueueSize)
                m_buildingQueue.Enqueue(unitMenuIndex);
            return;
        }

        m_currentBuildDuration = GetBuildableUnitData(unitMenuIndex).buildDuration;
        //Debug.Log("currentBuildDuration " + CurrentBuildDuration);

        m_isWorking = true;
        m_endBuildDate = Time.time + m_currentBuildDuration;

        m_requestedEntityBuildIndex = unitMenuIndex;

        OnUnitFormed += (Unit unit) =>
        {
            if (unit != null)
            {
                m_controller.AddUnit(unit);
                (m_controller as PlayerController)?.UpdateFactoryBuildQueueUI(m_requestedEntityBuildIndex);
            }
        };
    }

    // Finally spawn requested unit
    private Unit BuildUnit()
    {
        if (IsUnitIndexValid(m_requestedEntityBuildIndex) == false)
            return null;

        m_isWorking = false;

        GameObject unitPrefab = m_factoryData.availableUnits[m_requestedEntityBuildIndex];

        if (m_hud != null)
            m_hud.Progression = 0f;

        int slotIndex = m_spawnCount % m_factoryData.nbSpawnSlots;
        // compute simple spawn position around the factory
        float angle = 2f * Mathf.PI / m_factoryData.nbSpawnSlots * slotIndex;
        int offsetIndex = Mathf.FloorToInt(m_spawnCount / m_factoryData.nbSpawnSlots);
        float radius = m_factoryData.spawnRadius + offsetIndex * m_factoryData.radiusOffset;
        Vector3 spawnPos = transform.position + new Vector3(radius * Mathf.Cos(angle), 0f, radius * Mathf.Sin(angle));

        // !! Flying units require a specific layer to be spawned on !!
        bool isFlyingUnit = unitPrefab.GetComponent<Unit>().UnitData.isFlying;
        int layer = isFlyingUnit ? LayerMask.NameToLayer("FlyingZone") : LayerMask.NameToLayer("Floor");

        // cast position on ground
        RaycastHit raycastInfo;
        Ray ray = new Ray(spawnPos, Vector3.down);
        if (Physics.Raycast(ray, out raycastInfo, 10f, 1 << layer))
            spawnPos = raycastInfo.point;

        Transform teamRoot = GameServices.GetControllerByTeam(Team)?.TeamRoot;
        GameObject unitInst = Instantiate(unitPrefab, spawnPos, Quaternion.identity, teamRoot);
        unitInst.name = unitInst.name.Replace("(Clone)", "_" + m_spawnCount.ToString());
        Unit newUnit = unitInst.GetComponent<Unit>();
        newUnit.Init(Team);

        m_spawnCount++;

        // disable build cancelling callback
        OnBuildCanceled = null;

        return newUnit;
    }
    public void CancelCurrentBuild()
    {
        if (!m_isWorking) return;

        m_isWorking = false;

        // refund build points
        m_controller.TotalBuildPoints += GetUnitCost(m_requestedEntityBuildIndex);
        foreach(int unitIndex in m_buildingQueue)
        {
            m_controller.TotalBuildPoints += GetUnitCost(unitIndex);
        }
        m_buildingQueue.Clear();

        if(m_hud != null) m_hud.Progression = 0f;

        m_currentBuildDuration = 0f;
        m_requestedEntityBuildIndex = -1;

        OnBuildCanceled?.Invoke();
        OnBuildCanceled = null;
    }
    #endregion

    #region Factory building methods
    public GameObject GetFactoryPrefab(int factoryIndex) => IsFactoryIndexValid(factoryIndex) ? m_factoryData.availableFactories[factoryIndex] : null;
    private bool IsFactoryIndexValid(int factoryIndex)
    {
        if (factoryIndex < 0 || factoryIndex >= m_factoryData.availableFactories.Length)
        {
            Debug.LogWarning("Wrong factoryIndex " + factoryIndex);
            return false;
        }
        return true;
    }
    public FactoryDataScriptable GetBuildableFactoryData(int factoryIndex)
    {
        if (IsFactoryIndexValid(factoryIndex) == false)
            return null;

        return m_factoryData.availableFactories[factoryIndex].GetComponent<Factory>().GetFactoryData;
    }
    public int GetFactoryCost(int factoryIndex)
    {
        FactoryDataScriptable data = GetBuildableFactoryData(factoryIndex);
        if (data)
            return data.cost;

        return 0;
    }
    public bool CanPositionFactory(int factoryIndex, Vector3 buildPos)
    {
        if (IsFactoryIndexValid(factoryIndex) == false)
            return false;

        if (GameServices.IsPosInPlayableBounds(buildPos) == false)
            return false;

        GameObject factoryPrefab = m_factoryData.availableFactories[factoryIndex];

        Vector3 extent = factoryPrefab.GetComponent<BoxCollider>().size / 2f;

        float overlapYOffset = 0.1f;
        buildPos += Vector3.up * (extent.y + overlapYOffset);

        if (Physics.CheckBox(buildPos, extent))
        //foreach(Collider col in Physics.OverlapBox(buildPos, halfExtent))
        {
            //Debug.Log("Overlap");
            return false;
        }

        return true;
    }
    public Factory StartBuildFactory(int factoryIndex, Vector3 buildPos)
    {
        if (IsFactoryIndexValid(factoryIndex) == false)
            return null;

        if (m_isWorking)
            return null;

        GameObject factoryPrefab = m_factoryData.availableFactories[factoryIndex];
        Transform teamRoot = GameServices.GetControllerByTeam(Team)?.TeamRoot;
        GameObject factoryInst = Instantiate(factoryPrefab, buildPos, Quaternion.identity, teamRoot);
        factoryInst.name = factoryInst.name.Replace("(Clone)", "_" + m_spawnCount.ToString());
        Factory newFactory = factoryInst.GetComponent<Factory>();
        newFactory.Init(Team);
        newFactory.StartSelfConstruction();

        return newFactory;
    }

    #endregion
}
