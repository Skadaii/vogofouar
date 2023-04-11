using AIPlanner.GOAP;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EntityVisibility))]
public abstract partial class Entity : MonoBehaviour, ISelectable, IDamageable, IRepairable//, ICapturable
{
    //  Variables
    //  ---------

    [SerializeField]
    protected ETeam m_team;

    [SerializeField]
    protected GameObject m_selectedSprite = null;

    [SerializeField]
    protected SpriteRenderer m_icon;

    protected EntityVisibility m_visibility;

    protected bool m_isInitialized = false;

    [SerializeField]
    protected GameObject m_GFX;
    protected EntityHUD m_hud;

    //  Damageable variables

    protected int m_HP = 0;
    protected int m_maxHP = 100;
    //protected Text m_HPText = null;
    protected System.Action m_onHpUpdated;
    public System.Action onDeathEvent;

    private static List<Command> m_entityCommands;


    //  Properties
    //  ----------
    public static Command[] Commands => m_entityCommands.ToArray();
    public virtual Command[] TypeCommands => Commands;

    public GameObject GFX => m_GFX;
    public bool IsAlive { get; protected set; }

    public bool IsSelected { get; protected set; }

    public EntityVisibility Visibility => m_visibility;

    //  Functions
    //  ---------

    #region MonoBehaviour methods

    private void OnValidate()
    {
        SetTeamColor();
    }

    protected virtual void Awake()
    {
        m_visibility = GetComponent<EntityVisibility>();
        m_hud = transform.GetComponentInChildren<EntityHUD>();
        if(m_selectedSprite != null) m_selectedSprite.SetActive(false);

        //m_HPText = transform.Find("Canvas/HPText")?.GetComponent<Text>();

        m_onHpUpdated += UpdateHpUI;

        //  Initialize commands
        m_entityCommands ??= new List<Command>
        {
            new VoidCommand(newActionName: "Stop", newMethod:"Stop", icon: Resources.Load<Sprite>("Textures/T_cross"))
        };
    }

    protected virtual void Start()
    {
        Init(Team);

        UpdateHpUI();

        IsAlive = true;
    }

    protected virtual void Update() {}

    #endregion

    public virtual void Init(ETeam _team)
    {
        if (m_isInitialized)
            return;

        m_team = _team;

        if (Visibility) { Visibility.Team = _team; }

        if(m_icon != null) m_icon.color = GameServices.GetTeamColor(m_team);

        SetTeamColor();

        m_isInitialized = true;
    }

    public Color GetColor() => GameServices.GetTeamColor(Team);

    private void SetTeamColor()
    {
        if (m_GFX != null)
        {
            Material mat = GameServices.GetDefaultTeamMaterial(Team);
            if (m_GFX.TryGetComponent(out MeshRenderer renderer))
            {
                renderer.material = mat;
            }

            MeshRenderer[] childRenderers = m_GFX.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer childRenderer in childRenderers)
            {
                childRenderer.material = mat;
            }
        }
    }

    public virtual void Stop()
    {

    }

    #region ISelectable
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        m_selectedSprite?.SetActive(IsSelected);

        if(IsSelected)
        {
            m_icon.color = Color.white;
        }
        else
        {
            m_icon.color = GameServices.GetTeamColor(m_team);
        }
    }

    public ETeam Team => m_team;

    #endregion


    void UpdateHpUI()
    {
        if(m_hud != null)
        {
            m_hud.Health = (float)m_HP / (float)m_maxHP;
        }
    }

    #region IDamageable
    public void AddDamage(int damageAmount)
    {
        if (IsAlive == false)
            return;

        m_HP -= damageAmount;

        m_onHpUpdated?.Invoke();

        if (m_HP <= 0)
        {
            IsAlive = false;
            onDeathEvent?.Invoke();
            Debug.Log("Entity " + gameObject.name + " died");
        }
    }
    public void Destroy()
    {
        AddDamage(m_HP);
    }
    #endregion

    #region IRepairable
    virtual public bool NeedsRepairing()
    {
        return true;
    }
    virtual public void Repair(int amount)
    {
        m_onHpUpdated?.Invoke();
    }
    virtual public void FullRepair()
    {
    }

    #endregion
}
