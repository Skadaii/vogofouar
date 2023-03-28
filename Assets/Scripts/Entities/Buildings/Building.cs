using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Building : Entity
{
    //  Variables
    //  ---------

    protected Image m_buildGaugeImage;
    protected float m_currentBuildDuration = 0f;
    protected float m_endBuildDate = 0f;

    protected bool m_isActive;
    protected bool m_isCompleted;

    public Action<Building> OnBuildingBuilt;
    public Action OnBuildCanceled;

    //  Properties
    //  ----------

    public abstract BuildingDataScriptable BuildingData { get; }
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

        m_buildGaugeImage = transform.Find("Canvas/ProgressImage").GetComponent<Image>();

        if (m_buildGaugeImage)
        {
            m_buildGaugeImage.fillAmount = 0f;
            m_buildGaugeImage.color = GameServices.GetTeamColor(Team);
        }

        m_HP = BuildingData.maxHP;
        OnDestructionEvent += Building_OnDestruction;
    }

    protected virtual new void Start()
    {
        base.Start();
        GameServices.GameState.IncreaseTeamScore(m_team);
        //m_controller = GameServices.GetControllerByTeam(m_team);
    }

    protected virtual new void Update()
    {
        if(IsUnderConstruction)
        {
            // $$$ TODO : improve construction progress rendering
            if (Time.time > m_endBuildDate)
            {
                m_isCompleted = true;
                m_isActive = true;
                m_buildGaugeImage.fillAmount = 0f;
            }
            else if (m_buildGaugeImage)
                m_buildGaugeImage.fillAmount = 1f - (m_endBuildDate - Time.time) / BuildingData.buildDuration;
        }
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
    public override bool NeedsRepairing() => m_HP < BuildingData.maxHP;

    public override void Repair(int amount)
    {
        m_HP = Mathf.Min(m_HP + amount, BuildingData.maxHP);
        base.Repair(amount);
    }

    public override void FullRepair() => Repair(BuildingData.maxHP);

    #endregion

    protected void StartSelfConstruction()
    {
        m_isActive = m_isCompleted = false;
        m_endBuildDate = Time.time + BuildingData.buildDuration;
    }
}
