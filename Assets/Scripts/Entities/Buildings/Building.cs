using System;
using UnityEngine;

public abstract class Building : Entity
{
    //  Variables
    //  ---------

    private float m_buildResources = 0f;

    //protected Image m_buildGaugeImage;
    protected float m_currentBuildDuration = 0f;
    protected float m_endBuildDate = 0f;

    protected bool m_isActive;
    [SerializeField] protected bool m_isCompleted;

    public Action<Building> OnBuildingBuilt;

    //  Properties
    //  ----------
    public abstract BuildingDataScriptable BuildingData { get; }
    public override EntityDataScriptable EntityData => BuildingData;

    public int Cost { get { return BuildingData.cost; } }
    public bool IsUnderConstruction { get { return !m_isCompleted; } }


    //  Functions
    //  ---------

    #region MonoBehaviour methods

    protected virtual new void Awake()
    {
        base.Awake();

        if (BuildingData == null)
        {
            Debug.LogWarning("Missing Building data in " + gameObject.name);
        }

        if (!m_isCompleted) HealthPoint = 0f;

        if (m_hud != null) m_hud.Progression = 0f;
        
        onDeathEvent += Building_OnDestruction;
    }

    protected virtual new void Start()
    {
        base.Start();
        GameServices.GameState.IncreaseTeamScore(m_team);
        //m_controller = GameServices.GetControllerByTeam(m_team);
    }

    protected virtual new void Update()
    {
        base.Update();
    }

    #endregion


    private void Building_OnDestruction()
    {
        if (BuildingData.deathFXPrefab)
        {
            GameObject fx = Instantiate(BuildingData.deathFXPrefab, transform);
            fx.transform.parent = null;
        }

        GameServices.GameState.DecreaseTeamScore(m_team);
        Destroy(gameObject);
    }

    #region IRepairable

    public override bool NeedsRepairing() => HealthPoint < MaxHealthPoints;

    public override float Repair(float amount)
    {
        if(m_isCompleted)
        {
            return base.Repair(amount);
        }
        else
        {
            return ReceiveResources(amount);
        }
    }

    public override void FullRepair() => Repair(BuildingData.maxHP);

    #endregion

    public float ReceiveResources(float resourceValue)
    {
        float remainingCost = BuildingData.cost - m_buildResources;
        float extra = Mathf.Max(0f, resourceValue - remainingCost);

        m_buildResources = Mathf.Min(m_buildResources + resourceValue, BuildingData.cost);

        float percent = m_buildResources / BuildingData.cost;
        HealthPoint = MaxHealthPoints * percent;

        if (m_hud != null)
        {
            m_hud.Progression = percent;
        }

        if (percent >= 1f)
        {
            ConstructionCompleted();
        }

        return extra;
    }

    protected void ConstructionCompleted()
    {
        m_isActive = true;

        m_isCompleted = true;
        m_buildResources = BuildingData.cost;
        HealthPoint = MaxHealthPoints;
    }
}
