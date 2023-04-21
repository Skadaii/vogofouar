using System;
using System.Collections.Generic;
using UnityEngine;

public class Factory : Building
{
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

    //private const int MAX_AVAILABLE_FACTORIES = 3;

    [SerializeField]
    private int m_maxBuildingQueueSize = 5;
    //private Queue<int> m_buildingQueue = new Queue<int>();
    private Queue<Unit> m_unitQueue = new Queue<Unit>();

    public Action<Unit> OnUnitFormed;
    private bool m_isWorking = false;

    private float m_currentUsedResources = 0f;

    //  Properties
    //  ----------
    public override BuildingDataScriptable BuildingData => m_factoryData;
    public FactoryDataScriptable FactoryData { get { return m_factoryData; } }
    public int AvailableUnitsCount { get { return Mathf.Min(MAX_AVAILABLE_UNITS, m_factoryData.availableUnits.Length); } }
    //public int AvailableFactoriesCount { get { return Mathf.Min(MAX_AVAILABLE_FACTORIES, m_factoryData.availableFactories.Length); } }

    //  Functions
    //  ---------

    #region MonoBehaviour methods

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();

        if (!m_isCompleted) return;

        if(m_isWorking)
        {
            //if (Time.time > m_endBuildDate)

            if(m_currentUsedResources >= m_requestedUnit.UnitData.cost)
            {
                OnUnitFormed?.Invoke(ProduceUnit());
                OnUnitFormed = null; // remove registered methods
                m_isWorking = false;

                m_requestedUnit = null;
                // manage build queue : chain with new unit build if necessary
                if (m_unitQueue.Count != 0)
                {
                    Unit unit = m_unitQueue.Dequeue();
                    StartUnitProduction(unit);
                }
            }
            else
            {
                float resourceCosumption = Mathf.Min(FactoryData.resourceConsumptionPerSecond * Time.deltaTime, TeamController.CurrentResources);
                TeamController.CurrentResources -= resourceCosumption;

                m_currentUsedResources += resourceCosumption;

                if (m_hud != null) 
                    m_hud.Progression = m_currentUsedResources / m_requestedUnit.UnitData.cost;
            }
        }
    }

    #endregion

    #region Unit production methods

    public bool CancelUnitProduction(GameObject unitPrefab)
    {
        Unit unit = unitPrefab.GetComponent<Unit>();
        if (unit == null) return false;
        
        Queue<Unit> tempQueue = new Queue<Unit>();
        bool canceled = false;

        foreach (Unit u in m_unitQueue)
        {
            if (unit == u && !canceled)
            {
                canceled = true;
            }
            else
            {
                tempQueue.Enqueue(u);
            }
        }

        m_unitQueue = tempQueue;

        if (!canceled && m_requestedUnit == unit)
        {
            CancelCurrentUnitProduction();
            return true;
        }

        return true;
    }

    public bool RequestUnitProduction(GameObject unitPrefab)
    {
        Unit unit = unitPrefab.GetComponent<Unit>();
        if (unit == null) return false;

        StartUnitProduction(unit);

        return true;
    }

    private void StartUnitProduction(Unit unit)
    {
        // Build queue
        if (m_isWorking)
        {
            if (m_unitQueue.Count < m_maxBuildingQueueSize)
                m_unitQueue.Enqueue(unit);
            return;
        }

        m_currentUsedResources = 0f;

        m_requestedUnit = unit;
        //m_currentBuildDuration = unit.UnitData.buildDuration;

        m_isWorking = true;
        //m_endBuildDate = Time.time + m_currentBuildDuration;

        OnUnitFormed += (Unit unit) =>
        {
            if (unit != null)
            {
                TeamController.AddUnit(unit);
                //(TeamController as PlayerController)?.UpdateFactoryBuildQueueUI(m_requestedEntityBuildIndex);
            }
        };
    }

    // Finally spawn requested unit
    private Unit ProduceUnit()
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

        return newUnit;
    }


    public void CancelCurrentUnitProduction()
    {
        if (!m_isWorking) return;

        m_isWorking = false;

        if (m_hud != null) m_hud.Progression = 0f;

        // refund build points
        TeamController.CurrentResources += m_currentUsedResources;

        m_requestedUnit = null;

        if (m_unitQueue.Count != 0)
        {
            Unit unit = m_unitQueue.Dequeue();
            StartUnitProduction(unit);
        }
    }

    public void CancelAllProduction()
    {
        if (!m_isWorking) return;

        m_unitQueue.Clear();

        CancelCurrentUnitProduction();
    }

    #endregion


    public override void Stop()
    {
        base.Stop();

        if (m_isWorking) CancelAllProduction();
    }

    #region Commands

    public static void Command_RequestUnitProduction(Entity entity, GameObject unitPrefab)
    {
        ((Factory)entity)?.RequestUnitProduction(unitPrefab);
    }

    public static void Command_CancelUnitProduction(Entity entity, GameObject unitPrefab)
    {
        ((Factory)entity)?.CancelUnitProduction(unitPrefab);
    }

    public static int Command_GetQueueCountOfUnit(Entity entity, GameObject unitPrefab)
    {
        Factory factory = entity as Factory;
        Unit unit = unitPrefab.GetComponent<Unit>();

        int result = 0;

        if (factory != null && unit != null)
        {
            if (factory.m_requestedUnit == unit) result++;

            foreach(Unit u in factory.m_unitQueue)
            {
                if (u == unit) result++;
            }
        }

        return result;
    }

    #endregion
}
