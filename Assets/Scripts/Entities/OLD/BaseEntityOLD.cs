using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public abstract class BaseEntityOLD : MonoBehaviour//, ISelectable, IDamageable, IRepairable
{
    //  Variables
    //  ---------

    [SerializeField]
    protected ETeam m_team;

    protected EntityVisibility m_visibility;

    protected int m_HP = 0;
    protected Action m_onHpUpdated;
    protected GameObject m_selectedSprite = null;
    protected Text m_HPText = null;
    protected bool m_isInitialized = false;
    protected UnityEngine.UI.Image m_minimapImage;

    public Action onDeadEvent;

    //  Properties
    //  ----------

    public bool IsSelected { get; protected set; }
    public bool IsAlive { get; protected set; }

    public EntityVisibility Visibility
    {
        get
        {
            if (m_visibility == null)
            {
                m_visibility = GetComponent<EntityVisibility>();
            }
            return m_visibility;
        }
    }

    //  Functions
    //  ---------

    //virtual public void Init(ETeam _team)
    //{
    //    if (m_isInitialized)
    //        return;

    //    m_team = _team;

    //    if (Visibility) { Visibility.team = _team; }

    //    Transform minimapTransform = transform.Find("MinimapCanvas");
    //    if (minimapTransform != null)
    //    {
    //        m_minimapImage = minimapTransform.GetComponentInChildren<UnityEngine.UI.Image>();
    //        m_minimapImage.color = GameServices.GetTeamColor(m_team);
    //    }

    //    m_isInitialized = true;
    //}
    //public Color GetColor() => GameServices.GetTeamColor(Team);
    
    //void UpdateHpUI()
    //{
    //    if (m_HPText != null)
    //        m_HPText.text = "HP : " + m_HP.ToString();
    //}

    //#region ISelectable
    //public void SetSelected(bool selected)
    //{
    //    IsSelected = selected;
    //    m_selectedSprite?.SetActive(IsSelected);
    //}
    //public ETeam Team => m_team;
    
    //#endregion

    //#region IDamageable
    //public void AddDamage(int damageAmount)
    //{
    //    if (IsAlive == false)
    //        return;

    //    m_HP -= damageAmount;

    //    m_onHpUpdated?.Invoke();

    //    if (m_HP <= 0)
    //    {
    //        IsAlive = false;
    //        onDeadEvent?.Invoke();
    //        Debug.Log("Entity " + gameObject.name + " died");
    //    }
    //}
    //public void Destroy()
    //{
    //    AddDamage(m_HP);
    //}
    //#endregion

    //#region IRepairable
    //virtual public bool NeedsRepairing()
    //{
    //    return true;
    //}
    //virtual public void Repair(int amount)
    //{
    //    m_onHpUpdated?.Invoke();
    //}
    //virtual public void FullRepair()
    //{
    //}
    //#endregion

    //#region MonoBehaviour methods
    //virtual protected void Awake()
    //{
    //    IsAlive = true;

    //    m_selectedSprite = transform.Find("SelectedSprite")?.gameObject;
    //    m_selectedSprite?.SetActive(false);

    //    Transform hpTransform = transform.Find("Canvas/HPText");
    //    if (hpTransform)
    //        m_HPText = hpTransform.GetComponent<Text>();

    //    m_onHpUpdated += UpdateHpUI;
    //}
    //virtual protected void Start()
    //{
    //    Init(Team);
    //    UpdateHpUI();
    //}
    //virtual protected void Update()
    //{
    //}
    //#endregion
}
