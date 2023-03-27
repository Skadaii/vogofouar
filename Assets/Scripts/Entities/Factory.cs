using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public sealed class Factory : BaseEntity
{
    //  Internal objects
    //  ----------------

    public enum State
    {
        Available = 0,
        UnderConstruction,
        BuildingUnit,
    }

    //  Variables
    //  ---------

    [SerializeField]
    private FactoryDataScriptable m_factoryData = null;

    private GameObject[] m_unitPrefabs = null;
    private GameObject[] m_factoryPrefabs = null;
    private int m_requestedEntityBuildIndex = -1;
    private Image m_buildGaugeImage;
    private float m_currentBuildDuration = 0f;
    private float m_endBuildDate = 0f;
    private int m_spawnCount = 0;
    /* !! max available unit count in menu is set to 9, available factories count to 3 !! */
    private const int MAX_AVAILABLE_UNITS = 9;
    
    private const int MaxAvailableFactories = 3;

    private UnitController m_controller = null;

    [SerializeField]
    private int m_maxBuildingQueueSize = 5;
    private Queue<int> m_buildingQueue = new Queue<int>();

    public Action<Unit> OnUnitBuilt;
    public Action<Factory> OnFactoryBuilt;
    public Action OnBuildCanceled;

    //  Properties
    //  ----------

    public State CurrentState { get; private set; }
    public bool IsUnderConstruction { get { return CurrentState == State.UnderConstruction; } }
    public int Cost { get { return m_factoryData.cost; } }
    public FactoryDataScriptable GetFactoryData { get { return m_factoryData; } }
    public int AvailableUnitsCount { get { return Mathf.Min(MAX_AVAILABLE_UNITS, m_factoryData.availableUnits.Length); } }
    public int AvailableFactoriesCount { get { return Mathf.Min(MaxAvailableFactories, m_factoryData.availableFactories.Length); } }
    public bool IsBuildingUnit { get { return CurrentState == State.BuildingUnit; } }

    //  Functions
    //  ---------

    #region MonoBehaviour methods
    protected override void Awake()
    {
        base.Awake();

        m_buildGaugeImage = transform.Find("Canvas/BuildProgressImage").GetComponent<Image>();
        if (m_buildGaugeImage)
        {
            m_buildGaugeImage.fillAmount = 0f;
            m_buildGaugeImage.color = GameServices.GetTeamColor(Team);
        }

        if (m_factoryData == null)
        {
            Debug.LogWarning("Missing FactoryData in " + gameObject.name);
        }
        m_HP = m_factoryData.maxHP;
        onDeadEvent += Factory_OnDead;

        m_unitPrefabs = new GameObject[m_factoryData.availableUnits.Length];
        m_factoryPrefabs = new GameObject[m_factoryData.availableFactories.Length];

        // Load from resources actual Unit prefabs from template data
        for (int i = 0; i < m_factoryData.availableUnits.Length; i++)
        {
            GameObject templateUnitPrefab = m_factoryData.availableUnits[i];
            string path = "Prefabs/Units/" + templateUnitPrefab.name + "_" + m_team.ToString();
            m_unitPrefabs[i] = Resources.Load<GameObject>(path);
            if (m_unitPrefabs[i] == null)
                Debug.LogWarning("could not find Unit Prefab at " + path);
        }

        // Load from resources actual Factory prefabs from template data
        for (int i = 0; i < m_factoryData.availableFactories.Length; i++)
        {
            GameObject templateFactoryPrefab = m_factoryData.availableFactories[i];
            string path = "Prefabs/Factories/" + templateFactoryPrefab.name + "_" + m_team.ToString();
            m_factoryPrefabs[i] = Resources.Load<GameObject>(path);
        }
    }
    protected override void Start()
    {
        base.Start();
        GameServices.GameState.IncreaseTeamScore(m_team);
        m_controller = GameServices.GetControllerByTeam(m_team);
    }
    protected override void Update()
    {
        switch (CurrentState)
        {
            case State.Available:
                break;

            case State.UnderConstruction:
                // $$$ TODO : improve construction progress rendering
                if (Time.time > m_endBuildDate)
                {
                    CurrentState = State.Available;
                    m_buildGaugeImage.fillAmount = 0f;
                }
                else if (m_buildGaugeImage)
                    m_buildGaugeImage.fillAmount = 1f - (m_endBuildDate - Time.time) / m_factoryData.buildDuration;
                break;

            case State.BuildingUnit:
                if (Time.time > m_endBuildDate)
                {
                    OnUnitBuilt?.Invoke(BuildUnit());
                    OnUnitBuilt = null; // remove registered methods
                    CurrentState = State.Available;

                    // manage build queue : chain with new unit build if necessary
                    if (m_buildingQueue.Count != 0)
                    {
                        int unitIndex = m_buildingQueue.Dequeue();
                        StartBuildUnit(unitIndex);
                    }
                }
                else if (m_buildGaugeImage)
                    m_buildGaugeImage.fillAmount = 1f - (m_endBuildDate - Time.time) / m_currentBuildDuration;
                break;
        }
    }
    #endregion
    private void Factory_OnDead()
    {
        if (m_factoryData.deathFXPrefab)
        {
            GameObject fx = Instantiate(m_factoryData.deathFXPrefab, transform);
            fx.transform.parent = null;
        }

        GameServices.GameState.DecreaseTeamScore(m_team);
        Destroy(gameObject);
    }

    #region IRepairable
    public override bool NeedsRepairing() => m_HP < GetFactoryData.maxHP;

    public override void Repair(int amount)
    {
        m_HP = Mathf.Min(m_HP + amount, GetFactoryData.maxHP);
        base.Repair(amount);
    }

    public override void FullRepair() => Repair(GetFactoryData.maxHP);

    #endregion

    #region Unit building methods
    private bool IsUnitIndexValid(int unitIndex)
    {
        if (unitIndex < 0 || unitIndex >= m_unitPrefabs.Length)
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

        return m_unitPrefabs[unitIndex].GetComponent<Unit>().UnitData;
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
        if (IsUnitIndexValid(unitMenuIndex) == false)
            return;

        // Factory is being constucted
        if (CurrentState == State.UnderConstruction)
            return;

        // Build queue
        if (CurrentState == State.BuildingUnit)
        {
            if (m_buildingQueue.Count < m_maxBuildingQueueSize)
                m_buildingQueue.Enqueue(unitMenuIndex);
            return;
        }

        m_currentBuildDuration = GetBuildableUnitData(unitMenuIndex).buildDuration;
        //Debug.Log("currentBuildDuration " + CurrentBuildDuration);

        CurrentState = State.BuildingUnit;
        m_endBuildDate = Time.time + m_currentBuildDuration;

        m_requestedEntityBuildIndex = unitMenuIndex;

        OnUnitBuilt += (Unit unit) =>
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

        CurrentState = State.Available;

        GameObject unitPrefab = m_unitPrefabs[m_requestedEntityBuildIndex];

        if (m_buildGaugeImage)
            m_buildGaugeImage.fillAmount = 0f;

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
        if (CurrentState == State.UnderConstruction || CurrentState == State.Available)
            return;

        CurrentState = State.Available;

        // refund build points
        m_controller.TotalBuildPoints += GetUnitCost(m_requestedEntityBuildIndex);
        foreach(int unitIndex in m_buildingQueue)
        {
            m_controller.TotalBuildPoints += GetUnitCost(unitIndex);
        }
        m_buildingQueue.Clear();

        m_buildGaugeImage.fillAmount = 0f;
        m_currentBuildDuration = 0f;
        m_requestedEntityBuildIndex = -1;

        OnBuildCanceled?.Invoke();
        OnBuildCanceled = null;
    }
    #endregion

    #region Factory building methods
    public GameObject GetFactoryPrefab(int factoryIndex) => IsFactoryIndexValid(factoryIndex) ? m_factoryPrefabs[factoryIndex] : null;
    private bool IsFactoryIndexValid(int factoryIndex)
    {
        if (factoryIndex < 0 || factoryIndex >= m_factoryPrefabs.Length)
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

        return m_factoryPrefabs[factoryIndex].GetComponent<Factory>().GetFactoryData;
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

        GameObject factoryPrefab = m_factoryPrefabs[factoryIndex];

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

        if (CurrentState == State.BuildingUnit)
            return null;

        GameObject factoryPrefab = m_factoryPrefabs[factoryIndex];
        Transform teamRoot = GameServices.GetControllerByTeam(Team)?.TeamRoot;
        GameObject factoryInst = Instantiate(factoryPrefab, buildPos, Quaternion.identity, teamRoot);
        factoryInst.name = factoryInst.name.Replace("(Clone)", "_" + m_spawnCount.ToString());
        Factory newFactory = factoryInst.GetComponent<Factory>();
        newFactory.Init(Team);
        newFactory.StartSelfConstruction();

        return newFactory;
    }
    private void StartSelfConstruction()
    {
        CurrentState = State.UnderConstruction;

        m_endBuildDate = Time.time + m_factoryData.buildDuration;
    }

    #endregion
}
