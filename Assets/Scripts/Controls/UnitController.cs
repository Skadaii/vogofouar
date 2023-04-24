using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// points system for units creation (Ex : light units = 1 pt, medium = 2pts, heavy = 3 pts)
// max points can be increased by capturing TargetBuilding entities
public class UnitController : MonoBehaviour
{
    [SerializeField]
    protected ETeam m_team;

    [SerializeField]
    protected float m_startingResources = 15f;

    protected float m_currentResources = 0f;

    [SerializeField] protected bool m_lonerUseLeader = true;

    protected int m_capturedTargets = 0;
    protected Transform m_teamRoot = null;

    protected List<UnitSquad> m_squadList = new List<UnitSquad>();

    protected List<Unit> m_selectedUnitList = new List<Unit>();
    protected List<Factory> m_buildingList = new List<Factory>();
    protected Factory m_selectedBuildings = null;

    // events
    protected Action m_onResourceUpdated;
    protected Action m_onCaptureTarget;

    [SerializeField] private GameObject m_virtualLeaderPrefab = null;

    public FormationRule m_currentFormation = null;

    //  Properties

    public float CurrentResources
    {
        get { return m_currentResources; }
        set
        {
            m_currentResources = value;
            m_onResourceUpdated?.Invoke();
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

    public List<Factory> FactoryList { get { return m_buildingList; } }
    public List<Unit> UnitList
    {
        get;
        protected set;
    }

    public List<Entity> EntityList => m_buildingList.Cast<Entity>().Concat(UnitList.Cast<Entity>()).ToList();

    public bool HasSelectedUnits => m_selectedUnitList.Count > 0;
    public bool HasSelectedBuildings => m_selectedBuildings != null;

    public List<UnitSquad> SquadList => m_squadList;


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
    protected virtual void SelectUnit(Unit unit)
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
            //CurrentResources += unit.Cost;
            if (unit.IsSelected)
                m_selectedUnitList.Remove(unit);
            UnitList.Remove(unit);
        };
        UnitList.Add(unit);
        CreateDynamicSquad(unit);
    }
    public void CaptureTarget(int points)
    {
        Debug.Log("CaptureTarget");
        //CurrentResources += points;
        CapturedTargets++;
    }
    public void LoseTarget(int points)
    {
        //CurrentResources -= points;
        CapturedTargets--;
    }

    protected virtual void OnUnitSelected() { }
    protected virtual void OnUnitUnselected() { }

    #endregion

    #region Squad methods
    public void SetSquadFormation(List<Unit> units, FormationRule formation)
    {
        if (units.Any())
        {
            UnitSquad unitSquad = CreateDynamicSquad(units.ToArray());
            unitSquad.Formation = formation;
        }
    }

    public UnitSquad CreateDynamicSquad(params Unit[] squadUnits)
    {
        if (!squadUnits.Any()) return null;

        Vector3 averagePosition = squadUnits.Where(unit => unit != null).Select(unit => unit.transform.position).Aggregate((a, b) => a + b) / squadUnits.Length;

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
            if (lastSquad.Units.Count > (m_lonerUseLeader ? 0 : 1))
                continue;

            lastSquad.Destroy();
            m_squadList.Remove(lastSquad);
        }

        newSquad.InitializeLeader(m_virtualLeaderPrefab, averagePosition);
        newSquad.m_leaderComponent.Init(Team);
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
            //CurrentResources += factory.Cost;
            if (factory.IsSelected)
                m_selectedBuildings = null;
            m_buildingList.Remove(factory);
        };
        m_buildingList.Add(factory);
    }
    virtual protected void SelectFactory(Factory factory)
    {
        if (factory == null || factory.IsUnderConstruction)
            return;

        m_selectedBuildings = factory;
        m_selectedBuildings.SetSelected(true);
        UnselectAllUnits();
    }
    virtual protected void UnselectCurrentFactory()
    {
        if (m_selectedBuildings != null)
            m_selectedBuildings.SetSelected(false);
        m_selectedBuildings = null;
    }


    protected bool RequestUnitProduction(GameObject unit) => m_selectedBuildings != null ? m_selectedBuildings.RequestUnitProduction(unit) : false;

    protected virtual bool ConstructBuilding(GameObject building, Vector3 position)
    {
        UnitSquad squad = CreateDynamicSquad(m_selectedUnitList.ToArray());
        return RequestBuildingConstruction(building, position, squad);
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

    protected bool RequestBuildingConstruction(GameObject buildingPrefab, Vector3 buildPos, UnitSquad squad)
    {
        Building building = buildingPrefab.GetComponent<Building>();

        if(building == null) return false;

        int cost = building.Cost;
        //if (CurrentResources < cost) return false;

        // Check if positon is valid
        if (CanPlaceBuilding(buildingPrefab, buildPos) == false)
            return false;

        Transform teamRoot = GameServices.GetControllerByTeam(Team)?.TeamRoot;
        Building createdBuilding = Instantiate(buildingPrefab, buildPos, Quaternion.identity, teamRoot).GetComponent<Building>();
        createdBuilding.Init(Team);

        if (createdBuilding != null && squad != null)
        {
            if(createdBuilding as Factory != null) AddFactory(createdBuilding as Factory);

            squad.m_leaderComponent.SetTarget(createdBuilding, ETargetType.Build);

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
        CurrentResources = m_startingResources;

        // get all team factory already in scene
        Factory [] allFactories = FindObjectsOfType<Factory>();
        foreach(Factory factory in allFactories)
        {
            if (factory.Team == Team)
            {
                AddFactory(factory);
            }
        }

        Debug.Log("found " + m_buildingList.Count + " factory for team " + Team.ToString());
    }

    virtual protected void Update()
    {
        
        m_squadList = m_squadList.Where(squad =>
        {
            if (squad.Units.Count == 0)
            {
                squad.Destroy();
                return false;
            }

            return true;
        }).ToList();
    }
    #endregion
}
