using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// points system for units creation (Ex : light units = 1 pt, medium = 2pts, heavy = 3 pts)
// max points can be increased by capturing TargetBuilding entities
public class UnitController : MonoBehaviour
{
    [SerializeField]
    protected ETeam m_team;

    [SerializeField]
    protected int m_startingBuildPoints = 15;

    protected int m_totalBuildPoints = 0;

    protected int m_capturedTargets = 0;
    protected Transform m_teamRoot = null;

    protected List<UnitSquad> m_squadList = new List<UnitSquad>();

    protected List<Unit> m_selectedUnitList = new List<Unit>();
    protected List<Factory> m_factoryList = new List<Factory>();
    protected Factory m_selectedFactory = null;

    // events
    protected Action m_onBuildPointsUpdated;
    protected Action m_onCaptureTarget;

    [SerializeField] private GameObject m_virtualLeaderPrefab = null;

    public FormationRule m_currentFormation = null;

    //  Properties

    public int TotalBuildPoints
    {
        get { return m_totalBuildPoints; }
        set
        {
            Debug.Log("TotalBuildPoints updated");
            m_totalBuildPoints = value;
            m_onBuildPointsUpdated?.Invoke();
        }
    }

    public ETeam Team => m_team;
    public Transform TeamRoot => m_teamRoot;

    public int CapturedTargets
    {
        get { return m_capturedTargets; }
        set
        {
            m_capturedTargets = value;
            m_onCaptureTarget?.Invoke();
        }
    }

    public List<Factory> GetFactoryList { get { return m_factoryList; } }
    public List<Unit> UnitList
    {
        get;
        protected set;
    }

    #region Unit methods
    protected void UnselectAllUnits()
    {

        foreach (Unit unit in m_selectedUnitList)
            unit.SetSelected(false);

        if (m_selectedUnitList.Any())
            OnUnitUnselected();

        m_selectedUnitList.Clear();
    }
    protected void SelectAllUnits()
    {
        foreach (Unit unit in UnitList)
            unit.SetSelected(true);

        m_selectedUnitList.Clear();
        m_selectedUnitList.AddRange(UnitList);

        OnUnitSelected();
    }
    protected void SelectAllUnitsByTypeId(int typeId)
    {
        UnselectCurrentFactory();
        UnselectAllUnits();
        m_selectedUnitList = UnitList.FindAll(delegate (Unit unit)
            {
                return unit.TypeId == typeId;
            }
        );
        foreach (Unit unit in m_selectedUnitList)
        {
            unit.SetSelected(true);
        }

        OnUnitSelected();
    }
    protected void SelectUnitList(List<Unit> units)
    {
        foreach (Unit unit in units)
            unit.SetSelected(true);
        m_selectedUnitList.AddRange(units);

        OnUnitSelected();
    }
    protected void SelectUnitList(Unit[] units)
    {
        foreach (Unit unit in units)
            unit.SetSelected(true);
        m_selectedUnitList.AddRange(units);

        OnUnitSelected();
    }
    protected void SelectUnit(Unit unit)
    {
        unit.SetSelected(true);
        m_selectedUnitList.Add(unit);

        OnUnitSelected();
    }
    protected void UnselectUnit(Unit unit)
    {
        unit.SetSelected(false);
        m_selectedUnitList.Remove(unit);

        OnUnitUnselected();
    }
    virtual public void AddUnit(Unit unit)
    {
        unit.onDeathEvent += () =>
        {
            TotalBuildPoints += unit.Cost;
            if (unit.IsSelected)
                m_selectedUnitList.Remove(unit);
            UnitList.Remove(unit);
        };
        UnitList.Add(unit);
    }
    public void CaptureTarget(int points)
    {
        Debug.Log("CaptureTarget");
        TotalBuildPoints += points;
        CapturedTargets++;
    }
    public void LoseTarget(int points)
    {
        TotalBuildPoints -= points;
        CapturedTargets--;
    }

    protected virtual void OnUnitSelected() { }
    protected virtual void OnUnitUnselected() { }

    #endregion

    #region Squad methods
    public void SetSquadFormation(List<Unit> units, FormationRule formation)
    {
        UnitSquad unitSquad = CreateDynamicSquad(units);

        unitSquad.Formation = formation;
    }

    protected UnitSquad CreateDynamicSquad(List<Unit> squadUnits)
    {
        Vector3 averagePosition = squadUnits.Select(unit => unit.transform.position).Aggregate((a, b) => a + b) / squadUnits.Count;

        UnitSquad newSquad = new UnitSquad();

        foreach (Unit unit in squadUnits)
        {
            UnitSquad lastSquad = unit.Squad;

            // If the two list are equals, there is no need to create a new squad nor a new leader
            if (lastSquad is not null && lastSquad.Units.SequenceEqual(squadUnits))
                return unit.Squad;

            unit.Squad = newSquad;

            if (lastSquad is null)
                continue;

            lastSquad.Units.Remove(unit);

            // Destroy the leader if there is one or less unit left in the last squad
            if (lastSquad.Units.Count > 1)
                continue;

            lastSquad.Destroy();
            m_squadList.Remove(lastSquad);
        }

        newSquad.InitializeLeader(m_virtualLeaderPrefab, averagePosition);
        newSquad.Formation = m_currentFormation;

        m_squadList.Add(newSquad);

        return newSquad;
    }

    #endregion

    #region Factory methods
    void AddFactory(Factory factory)
    {
        if (factory == null)
        {
            Debug.LogWarning("Trying to add null factory");
            return;
        }

        factory.onDeathEvent += () =>
        {
            TotalBuildPoints += factory.Cost;
            if (factory.IsSelected)
                m_selectedFactory = null;
            m_factoryList.Remove(factory);
        };
        m_factoryList.Add(factory);
    }
    virtual protected void SelectFactory(Factory factory)
    {
        if (factory == null || factory.IsUnderConstruction)
            return;

        m_selectedFactory = factory;
        m_selectedFactory.SetSelected(true);
        UnselectAllUnits();
    }
    virtual protected void UnselectCurrentFactory()
    {
        if (m_selectedFactory != null)
            m_selectedFactory.SetSelected(false);
        m_selectedFactory = null;
    }
    protected bool RequestUnitBuild(int unitMenuIndex)
    {
        if (m_selectedFactory == null)
            return false;

        return m_selectedFactory.RequestUnitBuild(unitMenuIndex);
    }
    protected bool RequestFactoryBuild(int factoryIndex, Vector3 buildPos)
    {
        if (m_selectedFactory == null)
            return false;

        int cost = m_selectedFactory.GetFactoryCost(factoryIndex);
        if (TotalBuildPoints < cost)
            return false;

        // Check if positon is valid
        if (m_selectedFactory.CanPositionFactory(factoryIndex, buildPos) == false)
            return false;

        Factory newFactory = m_selectedFactory.StartBuildFactory(factoryIndex, buildPos);
        if (newFactory != null)
        {
            AddFactory(newFactory);
            TotalBuildPoints -= cost;

            return true;
        }
        return false;
    }


    private static bool CanPlaceBuilding(GameObject buildingPrefab, Vector3 position)
    {
        if (GameServices.IsPosInPlayableBounds(position) == false)
            return false;

        Vector3 extent = buildingPrefab.GetComponent<BoxCollider>().size / 2f;

        float overlapYOffset = 0.1f;
        position += Vector3.up * (extent.y + overlapYOffset);

        if (Physics.CheckBox(position, extent))
        {
            return false;
        }

        return true;
    }

    protected bool RequestBuildingConstruction(GameObject buildingPrefab, Vector3 buildPos, Builder[] builders)
    {
        Building building = buildingPrefab.GetComponent<Building>();

        if(building == null) return false;

        int cost = building.Cost;
        if (TotalBuildPoints < cost) return false;

        // Check if positon is valid
        if (CanPlaceBuilding(buildingPrefab, buildPos) == false)
            return false;

        Transform teamRoot = GameServices.GetControllerByTeam(Team)?.TeamRoot;
        Building createdBuilding = Instantiate(buildingPrefab, buildPos, Quaternion.identity, teamRoot).GetComponent<Building>();
        createdBuilding.Init(Team);

        if (createdBuilding != null)
        {
            if(createdBuilding as Factory != null) AddFactory(createdBuilding as Factory);
            TotalBuildPoints -= cost;

            foreach(Builder builder in builders)
            {
                builder.Build(createdBuilding);
            }

            return true;
        }
        return false;
    }
    #endregion

    #region MonoBehaviour methods
    virtual protected void Awake()
    {
        UnitList = new List<Unit>();
        string rootName = m_team.ToString() + "Team";
        m_teamRoot = GameObject.Find(rootName)?.transform;
        if (m_teamRoot)
            Debug.LogFormat("TeamRoot {0} found !", rootName);
    }
    virtual protected void Start ()
    {
        CapturedTargets = 0;
        TotalBuildPoints = m_startingBuildPoints;

        // get all team factory already in scene
        Factory [] allFactories = FindObjectsOfType<Factory>();
        foreach(Factory factory in allFactories)
        {
            if (factory.Team == Team)
            {
                AddFactory(factory);
            }
        }

        Debug.Log("found " + m_factoryList.Count + " factory for team " + Team.ToString());
    }
    virtual protected void Update ()
    {
		
	}
    #endregion
}
