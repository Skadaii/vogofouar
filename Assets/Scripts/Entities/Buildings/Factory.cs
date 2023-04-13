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

    //private int m_requestedEntityBuildIndex = -1;
    private Unit m_requestedUnit = null;
    private int m_spawnCount = 0;
    /* !! max available unit count in menu is set to 9, available factories count to 3 !! */
    private const int MAX_AVAILABLE_UNITS = 9;

    //private const int MaxAvailableFactories = 3;

    private UnitController m_controller = null;

    [SerializeField]
    private int m_maxBuildingQueueSize = 5;
    //private Queue<int> m_buildingQueue = new Queue<int>();
    private Queue<Unit> m_unitQueue = new Queue<Unit>();

    public Action<Unit> OnUnitFormed;
    private bool m_isWorking = false;

    private static List<Command> m_factoryCommands;

    //  Properties
    //  ----------

    public new static Command[] Commands => Building.Commands.Concat(m_factoryCommands).ToArray();
    public override Command[] TypeCommands => Commands;

    public override BuildingDataScriptable BuildingData => m_factoryData;
    public FactoryDataScriptable FactoryData { get { return m_factoryData; } }
    public int AvailableUnitsCount { get { return Mathf.Min(MAX_AVAILABLE_UNITS, m_factoryData.availableUnits.Length); } }
    //public int AvailableFactoriesCount { get { return Mathf.Min(MaxAvailableFactories, m_factoryData.availableFactories.Length); } }

    //  Functions
    //  ---------

    #region MonoBehaviour methods

    protected override void Awake()
    {
        base.Awake();

        //  Initialize factory commands

        if (m_factoryCommands == null)
        {
            m_factoryCommands = new List<Command>();

            foreach (GameObject unitPrefabs in m_factoryData.availableUnits)
            {
                if (unitPrefabs.TryGetComponent(out Unit unit))
                {
                    m_factoryCommands.Add(new BuildCommand(unitPrefabs.name, newMethod: "RequestUnitBuild", icon: unit.UnitData.icon, toBuild: unitPrefabs));
                }
            }
        }
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
                if (m_unitQueue.Count != 0)
                {
                    Unit unit = m_unitQueue.Dequeue();
                    StartBuildUnit(unit);
                }
            }
            else if (m_hud != null)
                m_hud.Progression = 1f - (m_endBuildDate - Time.time) / m_currentBuildDuration;
        }
    }

    #endregion

    #region Unit building methods

    public bool RequestUnitBuild(GameObject unitPrefab)
    {
        Unit unit = unitPrefab.GetComponent<Unit>();
        if (unit == null) return false;

        int cost = unit.Cost;
        if (m_controller.TotalBuildPoints < cost || m_unitQueue.Count >= m_maxBuildingQueueSize)
            return false;

        m_controller.TotalBuildPoints -= cost;

        StartBuildUnit(unit);

        return true;
    }

    private void StartBuildUnit(Unit unit)
    {
        // Build queue
        if (m_isWorking)
        {
            if (m_unitQueue.Count < m_maxBuildingQueueSize)
                m_unitQueue.Enqueue(unit);
            return;
        }

        m_requestedUnit = unit;
        m_currentBuildDuration = unit.UnitData.buildDuration;

        m_isWorking = true;
        m_endBuildDate = Time.time + m_currentBuildDuration;

        OnUnitFormed += (Unit unit) =>
        {
            if (unit != null)
            {
                m_controller.AddUnit(unit);
                //(m_controller as PlayerController)?.UpdateFactoryBuildQueueUI(m_requestedEntityBuildIndex);
            }
        };
    }
    // Finally spawn requested unit
    private Unit BuildUnit()
    {
        //if (IsUnitIndexValid(m_requestedEntityBuildIndex) == false)
        //    return null;

        m_isWorking = false;

        //GameObject unitPrefab = m_factoryData.availableUnits[m_requestedEntityBuildIndex];

        if (m_hud != null)
            m_hud.Progression = 0f;

        int slotIndex = m_spawnCount % m_factoryData.nbSpawnSlots;
        // compute simple spawn position around the factory
        float angle = 2f * Mathf.PI / m_factoryData.nbSpawnSlots * slotIndex;
        int offsetIndex = Mathf.FloorToInt(m_spawnCount / m_factoryData.nbSpawnSlots);
        float radius = m_factoryData.spawnRadius + offsetIndex * m_factoryData.radiusOffset;
        Vector3 spawnPos = transform.position + new Vector3(radius * Mathf.Cos(angle), 0f, radius * Mathf.Sin(angle));

        // !! Flying units require a specific layer to be spawned on !!
        bool isFlyingUnit = m_requestedUnit.UnitData.isFlying;
        int layer = isFlyingUnit ? LayerMask.NameToLayer("FlyingZone") : LayerMask.NameToLayer("Floor");

        // cast position on ground
        RaycastHit raycastInfo;
        Ray ray = new Ray(spawnPos, Vector3.down);
        if (Physics.Raycast(ray, out raycastInfo, 10f, 1 << layer))
            spawnPos = raycastInfo.point;

        Transform teamRoot = GameServices.GetControllerByTeam(Team)?.TeamRoot;
        GameObject unitInst = Instantiate(m_requestedUnit.gameObject, spawnPos, Quaternion.identity, teamRoot);
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
        m_controller.TotalBuildPoints += m_requestedUnit.Cost;
        //foreach(int unitIndex in m_buildingQueue)
        //{
        //    m_controller.TotalBuildPoints += GetUnitCost(unitIndex);
        //}
        //m_buildingQueue.Clear();

        //WIP
        foreach (Unit unit in m_unitQueue)
        {
            m_controller.TotalBuildPoints += unit.Cost;
        }
        m_unitQueue.Clear();

        if (m_hud != null) m_hud.Progression = 0f;

        m_currentBuildDuration = 0f;
        m_requestedUnit = null;

        OnBuildCanceled?.Invoke();
        OnBuildCanceled = null;
    }
    #endregion

}
