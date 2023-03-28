using UnityEngine;
using UnityEngine.AI;

public abstract class Unit : Entity
{
    //  Variables
    //  ---------

    protected float m_lastActionDate = 0f;
    protected NavMeshAgent m_navMeshAgent;
    protected Entity m_target = null;

    //  Properties
    //  ----------

    public abstract UnitDataScriptable UnitData { get; }
    public int Cost => UnitData.cost;
    public int TypeId => UnitData.typeId;

    //  Functions
    //  ---------

    #region MonoBehaviour methods

    protected override void Awake()
    {
        base.Awake();

        m_navMeshAgent = GetComponent<NavMeshAgent>();

        // fill NavMeshAgent parameters
        m_navMeshAgent.speed = UnitData.speed;
        m_navMeshAgent.angularSpeed = UnitData.angularSpeed;
        m_navMeshAgent.acceleration = UnitData.acceleration;
    }

    #endregion

    public override void Init(ETeam _team)
    {
        if (m_isInitialized)
            return;

        base.Init(_team);

        m_HP = UnitData.maxHP;
        OnDestructionEvent += Unit_OnDead;
    }

    void Unit_OnDead()
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

    // Moving Task
    public void SetTargetPos(Vector3 pos)
    {
        if (m_target != null)
            m_target = null;

        if (m_navMeshAgent)
        {
            m_navMeshAgent.SetDestination(pos);
            m_navMeshAgent.isStopped = false;
        }
    }
}
