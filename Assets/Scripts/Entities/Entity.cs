using UnityEngine;

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

    private float m_healthPoints = 100f;
    private float m_maxHealthPoints = 100f;

    public System.Action onDeathEvent;

    //  Properties
    //  ----------
    public abstract EntityDataScriptable EntityData { get; }
    public Command[] Commands => EntityData.Commands;

    public GameObject GFX => m_GFX;
    public bool IsAlive { get; protected set; }

    public bool IsSelected { get; protected set; }

    public EntityVisibility Visibility => m_visibility;

    public UnitController TeamController => GameServices.GetControllerByTeam(Team);

    public float HealthPoint
    {
        get => m_healthPoints;
        protected set
        {
            m_healthPoints = Mathf.Clamp(value, 0f, m_maxHealthPoints);

            if (m_hud != null)
            {
                m_hud.Health = HealthPercent;
            }
        }
    }

    public float MaxHealthPoints
    {
        get => m_maxHealthPoints;
        
        protected set
        {
            m_maxHealthPoints = value;

            if (m_hud != null) m_hud.Health = HealthPercent;
        }
    }

    public float HealthPercent => m_healthPoints / m_maxHealthPoints;
    //  Functions
    //  ---------

    #region MonoBehaviour methods

    private void OnValidate()
    {
        SetTeamColor();
    }

    protected virtual void Awake()
    {
        SetTeamColor();

        m_visibility = GetComponent<EntityVisibility>();
        m_hud = transform.GetComponentInChildren<EntityHUD>();
        if(m_selectedSprite != null) m_selectedSprite.SetActive(false);

        HealthPoint = MaxHealthPoints = EntityData.maxHP;
    }

    protected virtual void Start()
    {
        Init(Team);
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

    #region IDamageable

    public void AddDamage(float damageAmount)
    {
        if (IsAlive == false)
            return;

        HealthPoint -= damageAmount;

        if (HealthPoint <= 0) Destroy();
    }

    public void Destroy()
    {
        HealthPoint = 0;
        IsAlive = false;
        onDeathEvent?.Invoke();

        Debug.Log("Entity " + gameObject.name + " died");
    }
    
    #endregion

    #region IRepairable

    virtual public bool NeedsRepairing()
    {
        return HealthPoint != MaxHealthPoints;
    }

    virtual public float Repair(float amount)
    {
        float remainingMissingHealth = MaxHealthPoints - HealthPoint;
        float extra = Mathf.Max(0f, amount - remainingMissingHealth);

        HealthPoint = Mathf.Min(HealthPoint + amount, MaxHealthPoints);

        return extra;
    }

    virtual public void FullRepair()
    {
    }

    #endregion

    #region Commands

    public static void Command_Stop(Entity entity)
    {
        entity.Stop();
    }

    #endregion
}
