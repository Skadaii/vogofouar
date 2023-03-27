using System;
using System.Collections.Generic;
using UnityEngine;

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

    protected List<Unit> m_selectedUnitList = new List<Unit>();
    protected List<Factory> m_factoryList = new List<Factory>();
    protected Factory m_selectedFactory = null;

    // events
    protected Action m_onBuildPointsUpdated;
    protected Action m_onCaptureTarget;


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
        m_selectedUnitList.Clear();
    }
    protected void SelectAllUnits()
    {
        foreach (Unit unit in UnitList)
            unit.SetSelected(true);

        m_selectedUnitList.Clear();
        m_selectedUnitList.AddRange(UnitList);
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
    }
    protected void SelectUnitList(List<Unit> units)
    {
        foreach (Unit unit in units)
            unit.SetSelected(true);
        m_selectedUnitList.AddRange(units);
    }
    protected void SelectUnitList(Unit [] units)
    {
        foreach (Unit unit in units)
            unit.SetSelected(true);
        m_selectedUnitList.AddRange(units);
    }
    protected void SelectUnit(Unit unit)
    {
        unit.SetSelected(true);
        m_selectedUnitList.Add(unit);
    }
    protected void UnselectUnit(Unit unit)
    {
        unit.SetSelected(false);
        m_selectedUnitList.Remove(unit);
    }
    virtual public void AddUnit(Unit unit)
    {
        unit.onDeadEvent += () =>
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
    #endregion

    #region Factory methods
    void AddFactory(Factory factory)
    {
        if (factory == null)
        {
            Debug.LogWarning("Trying to add null factory");
            return;
        }

        factory.onDeadEvent += () =>
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
