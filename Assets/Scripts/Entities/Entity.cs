using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EntityVisibility))]
public abstract class Entity : MonoBehaviour, ISelectable, IDamageable, IRepairable//, ICapturable
{
    //  Variables
    //  ---------

    [SerializeField]
    protected ETeam m_team;

    protected EntityVisibility m_visibility;

    protected bool m_isInitialized = false;
    protected GameObject m_selectedSprite = null;
    protected UnityEngine.UI.Image m_minimapImage;

    [SerializeField]
    protected GameObject m_GFX;
    protected EntityHUD m_hud;

    //  Damageable variables

    protected int m_HP = 0;
    protected int m_maxHP = 100;
    //protected Text m_HPText = null;
    protected Action m_onHpUpdated;
    public Action onDeathEvent;


    //  Properties
    //  ----------

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

        m_selectedSprite = transform.Find("SelectedSprite")?.gameObject;
        m_selectedSprite?.SetActive(false);

        //m_HPText = transform.Find("Canvas/HPText")?.GetComponent<Text>();

        m_onHpUpdated += UpdateHpUI;
    }

    protected virtual void Start()
    {
        Init(Team);
        UpdateHpUI();

        IsAlive = true;
    }

    protected virtual void Update() {}

    #endregion

    virtual public void Init(ETeam _team)
    {
        if (m_isInitialized)
            return;

        m_team = _team;

        if (Visibility) { Visibility.Team = _team; }

        Transform minimapTransform = transform.Find("MinimapCanvas");
        if (minimapTransform != null)
        {
            m_minimapImage = minimapTransform.GetComponentInChildren<UnityEngine.UI.Image>();
            m_minimapImage.color = GameServices.GetTeamColor(m_team);
        }

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

    #region ISelectable
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        m_selectedSprite?.SetActive(IsSelected);
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
