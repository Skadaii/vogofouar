using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Unit : BaseEntity
{
    //  Variables
    //  ---------

    [SerializeField] private UnitDataScriptable m_unitData = null;
     
    private Transform m_bulletSlot;
    private float m_lastActionDate = 0f;
    protected BaseEntity m_entityTarget = null;
    protected TargetBuilding m_captureTarget = null;
    protected NavMeshAgent m_navMeshAgent;

    protected UnitSquad m_squad = null;
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

    //  Properties
    //  ----------

    public UnitDataScriptable UnitData => m_unitData;
    public int Cost => m_unitData.cost;
    public int TypeId => m_unitData.typeId;

    protected float m_lastAttackedTime = 0f;
    protected Unit m_agressor;
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

    override public void Init(ETeam _team)
    {
        if (m_isInitialized)
            return;

        base.Init(_team);

        m_HP = m_unitData.maxHP;
        onDeadEvent += Unit_OnDead;
    }

    void Unit_OnDead()
    {
        if (IsCapturing())
            StopCapture();

        if (UnitData.deathFXPrefab)
        {
            GameObject fx = Instantiate(UnitData.deathFXPrefab, transform);
            fx.transform.parent = null;
        }

        Destroy(gameObject);
    }

    #region MonoBehaviour methods
    override protected void Awake()
    {
        base.Awake();

        m_navMeshAgent = GetComponent<NavMeshAgent>();
        m_bulletSlot = transform.Find("BulletSlot");

        // fill NavMeshAgent parameters
        m_navMeshAgent.speed = UnitData.speed;
        m_navMeshAgent.angularSpeed = UnitData.angularSpeed;
        m_navMeshAgent.acceleration = UnitData.acceleration;
    }

    override protected void Start()
    {
        // Needed for non factory spawned units (debug)
        if (!m_isInitialized)
            Init(m_team);

        base.Start();
    }

    override protected void Update()
    {
        // Attack / repair task debug test $$$ to be removed for AI implementation
        if (m_entityTarget != null)
        {
            if (m_entityTarget.Team != Team)
                ComputeAttack();
            else
                ComputeRepairing();
        }
	}
    #endregion

    #region IRepairable
    override public bool NeedsRepairing()
    {
        return m_HP < UnitData.maxHP;
    }
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

    #region Tasks methods : Moving, Capturing, Targeting, Attacking, Repairing ...

    // $$$ To be updated for AI implementation $$$

    // Moving Task
    public virtual void MoveTo(Vector3 pos)
    {
        m_entityTarget = null;

        if (m_captureTarget != null)
            StopCapture();

        if (m_navMeshAgent)
        {
            m_navMeshAgent.SetDestination(pos);
            m_navMeshAgent.isStopped = false;
        }
    }

    public virtual void MoveTo(Transform target) => MoveTo(target.position);

    public virtual void MoveToward(Vector3 velocity) => m_navMeshAgent.Move(velocity);

    // Targetting Task - attack
    public void SetAttackTarget(BaseEntity target)
    {
        if (CanAttack(target) == false)
            return;

        if (m_captureTarget != null)
            StopCapture();

        if (target.Team != Team)
            StartAttacking(target);
    }

    // Targetting Task - capture
    public void SetCaptureTarget(TargetBuilding target)
    {
        if (CanCapture(target) == false)
            return;

        if (m_entityTarget != null)
            m_entityTarget = null;

        if (IsCapturing())
            StopCapture();

        if (target.Team != Team)
            StartCapture(target);
    }

    // Targetting Task - repairing
    public void SetRepairTarget(BaseEntity entity)
    {
        if (CanRepair(entity) == false)
            return;

        if (m_captureTarget != null)
            StopCapture();

        if (entity.Team == Team)
            StartRepairing(entity);
    }
    public bool CanAttack(BaseEntity target)
    {
        if (target == null)
            return false;

        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > UnitData.attackDistanceMax * UnitData.attackDistanceMax)
            return false;

        return true;
    }

    // Attack Task
    public void StartAttacking(BaseEntity target)
    {
        m_entityTarget = target;
    }
    public void ComputeAttack()
    {
        if (CanAttack(m_entityTarget) == false)
            return;

        if (m_navMeshAgent)
            m_navMeshAgent.isStopped = true;

        transform.LookAt(m_entityTarget.transform);
        // only keep Y axis
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = 0f;
        eulerRotation.z = 0f;
        transform.eulerAngles = eulerRotation;

        if ((Time.time - m_lastActionDate) > m_unitData.attackFrequency)
        {
            m_lastActionDate = Time.time;
            // visual only ?
            if (m_unitData.bulletPrefab)
            {
                GameObject newBullet = Instantiate(m_unitData.bulletPrefab, m_bulletSlot);
                newBullet.transform.parent = null;
                newBullet.GetComponent<Bullet>().ShootToward(m_entityTarget.transform.position - transform.position, this);
            }
            // apply damages
            int damages = Mathf.FloorToInt(m_unitData.dps * m_unitData.attackFrequency);
            m_entityTarget.AddDamage(damages);
        }
    }
    public bool CanCapture(TargetBuilding target)
    {
        if (target == null)
            return false;

        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > UnitData.captureDistanceMax * UnitData.captureDistanceMax)
            return false;

        return true;
    }

    // Capture Task
    public void StartCapture(TargetBuilding target)
    {
        if (CanCapture(target) == false)
            return;

        if (m_navMeshAgent)
            m_navMeshAgent.isStopped = true;

        m_captureTarget = target;
        m_captureTarget.StartCapture(this);
    }
    public void StopCapture()
    {
        if (m_captureTarget == null)
            return;

        m_captureTarget.StopCapture(this);
        m_captureTarget = null;
    }

    public bool IsCapturing()
    {
        return m_captureTarget != null;
    }

    // Repairing Task
    public bool CanRepair(BaseEntity target)
    {
        if (UnitData.canRepair == false || target == null)
            return false;

        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > UnitData.repairDistanceMax * UnitData.repairDistanceMax)
            return false;

        return true;
    }
    public void StartRepairing(BaseEntity entity)
    {
        if (UnitData.canRepair)
        {
            m_entityTarget = entity;
        }
    }

    // $$$ TODO : add repairing visual feedback
    public void ComputeRepairing()
    {
        if (CanRepair(m_entityTarget) == false)
            return;

        if (m_navMeshAgent)
            m_navMeshAgent.isStopped = true;

        transform.LookAt(m_entityTarget.transform);
        // only keep Y axis
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = 0f;
        eulerRotation.z = 0f;
        transform.eulerAngles = eulerRotation;

        if ((Time.time - m_lastActionDate) > m_unitData.repairFrequency)
        {
            m_lastActionDate = Time.time;

            // apply reparing
            int amount = Mathf.FloorToInt(m_unitData.rps * m_unitData.repairFrequency);
            m_entityTarget.Repair(amount);
        }
    }
    #endregion

    #region Squad methods : Alert
    private void Alert()
    {
        m_squad.ReceiveAlert(this);
    }
    #endregion
}
