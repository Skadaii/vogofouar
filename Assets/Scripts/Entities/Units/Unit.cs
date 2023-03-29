using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;

public abstract class Unit : Entity
{
    //  Variables
    //  ---------

    protected float m_lastActionDate = 0f;
    protected NavMeshAgent m_navMeshAgent;
    protected Entity m_target = null;

    //  Squad system variables

    protected UnitSquad m_squad = null;
    protected float m_lastAttackedTime = 0f;
    protected Unit m_agressor;

    //  Properties
    //  ----------

    public abstract UnitDataScriptable UnitData { get; }
    public int Cost => UnitData ? UnitData.cost : 0;
    public int TypeId => UnitData ? UnitData.typeId : -1;

    //  Squad system properties

    public virtual UnitSquad Squad
    {
        get => m_squad;
        set
        {
            if (m_squad is not null)
            {
                m_squad.Units.Remove(this);
                m_squad.UpdatePositions();
            }

            m_squad = value;

            if (m_squad is not null)
                m_squad.Units.Add(this);
        }
    }
    public Unit Agressor
    {
        get => m_agressor;

        set
        {
            m_agressor = value;

            if (m_agressor)
                m_lastAttackedTime = Time.time;
        }
    }

    //  Functions
    //  ---------

        #region MonoBehaviour methods

    protected virtual new void Awake()
    {
        base.Awake();

        m_navMeshAgent = GetComponent<NavMeshAgent>();

        // fill NavMeshAgent parameters
        m_navMeshAgent.speed = UnitData.speed;
        m_navMeshAgent.angularSpeed = UnitData.angularSpeed;
        m_navMeshAgent.acceleration = UnitData.acceleration;
    }

    protected virtual new void Update()
    {
        base.Update();
    }

    #endregion

    public override void Init(ETeam _team)
    {
        if (m_isInitialized)
            return;

        base.Init(_team);
        
        m_HP = m_maxHP = UnitData.maxHP;
        onDeathEvent += Unit_OnDeath;
    }

    void Unit_OnDeath()
    {
        if (UnitData.deathFXPrefab)
        {
            GameObject fx = Instantiate(UnitData.deathFXPrefab, transform);
            fx.transform.parent = null;
        }

        Destroy(gameObject);
    }


    #region IRepairable

    override public bool NeedsRepairing() => m_HP < UnitData.maxHP;
  
    override public void Repair(int amount)
    {
        m_HP = Mathf.Min(m_HP + amount, UnitData.maxHP);
        base.Repair(amount);
    }
    override public void FullRepair()
    {
        Repair(UnitData.maxHP);
    }

    #endregion


    #region Squad methods : Alert
    private void Alert()
    {
        m_squad.ReceiveAlert(this);
    }

    #endregion

    // Moving Task
    public virtual void MoveTo(Vector3 pos)
    {
        m_target = null;

        //if (m_target != null)
        //  StopCapture();

        if (m_navMeshAgent)
        {
            m_navMeshAgent.SetDestination(pos);
            m_navMeshAgent.isStopped = false;
        }
    }

    public virtual void MoveTo(Transform target) => MoveTo(target.position);

    public virtual void MoveToward(Vector3 velocity) => m_navMeshAgent.Move(velocity);
}
