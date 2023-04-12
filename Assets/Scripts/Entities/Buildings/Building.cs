using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
    public Action OnBuildCanceled;

    private static List<Command> m_buildingCommands;

    //  Properties
    //  ----------

    public new static Command[] Commands => m_buildingCommands.ToArray().Concat(Entity.Commands) as Command[];
    public override Command[] TypeCommands => Commands;

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

        //  Initialize building commands
        m_buildingCommands ??= new List<Command>
        {
        };
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

    public override void Repair(float amount)
    {
        if(m_isCompleted)
        {
            base.Repair(amount);
        }
        else
        {
            ReceiveResources(amount);
        }
    }

    public override void FullRepair() => Repair(BuildingData.maxHP);

    #endregion

    protected void StartSelfConstruction()
    {
        m_isActive = m_isCompleted = false;
        m_endBuildDate = Time.time + BuildingData.buildDuration;
    }

    public float ReceiveResources(float resourceValue)
    {
        float remainingCost = BuildingData.constructionCost - m_buildResources;
        float change = Mathf.Max(0f, resourceValue - remainingCost);

        m_buildResources = Mathf.Min(m_buildResources + resourceValue - change, BuildingData.constructionCost);

        float percent = m_buildResources / BuildingData.constructionCost;
        HealthPoint = MaxHealthPoints * percent;

        if (m_hud != null)
        {
            m_hud.Progression = percent;
        }

        if (percent >= 1f)
        {
            ConstructionCompleted();
        }

        return change;
    }

    protected void ConstructionCompleted()
    {
        m_isActive = true;

        m_isCompleted = true;
        m_buildResources = BuildingData.constructionCost;
        HealthPoint = MaxHealthPoints;
    }
}
