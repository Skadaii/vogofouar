using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;
using static UnityEditor.PlayerSettings;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
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

    private List<Vector3> m_patrolPoint = new List<Vector3>();
    private int m_patrolIndex = 0;

    private static List<Command> m_unitCommands;

    //  Properties
    //  ----------

    public new static Command[] Commands => m_unitCommands.ToArray().Concat(Entity.Commands).ToArray();
    public override Command[] TypeCommands => Commands;

    public abstract UnitDataScriptable UnitData { get; }
    public override EntityDataScriptable EntityData => UnitData;

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

    private void OnValidate()
    {
        InitializeNavMeshAgent();
    }

    protected virtual new void Awake()
    {
        base.Awake();

        InitializeNavMeshAgent();

        //  Initialize unit commands
        m_unitCommands ??= new List<Command>
        {
            new LocationCommand(newActionName: "Move", newMethod:"MoveTo", icon: Resources.Load<Sprite>("Textures/T_Move")),
            new TargetCommand(newActionName: "Move", newMethod:"MoveTo", icon: Resources.Load<Sprite>("Textures/T_Move")),
            new LocationCommand(newActionName: "Patrol", newMethod:"AddPatrolPoint", icon: Resources.Load<Sprite>("Textures/T_Move"))
        };

    }

    protected virtual new void Update()
    {
        base.Update();

        if(m_patrolPoint.Count > 0 && m_navMeshAgent)
        {
            if(m_navMeshAgent.remainingDistance <= m_navMeshAgent.stoppingDistance)
            {
                m_patrolIndex = (m_patrolIndex +1)% m_patrolPoint.Count;
            }
            m_navMeshAgent.SetDestination(m_patrolPoint[m_patrolIndex]);
        }
    }

    #endregion

    public override void Init(ETeam _team)
    {
        if (m_isInitialized)
            return;

        base.Init(_team);
        
        HealthPoint = MaxHealthPoints = UnitData.maxHP;
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


    private void InitializeNavMeshAgent()
    {
        m_navMeshAgent = GetComponent<NavMeshAgent>();

        if(m_navMeshAgent != null && UnitData != null)
        {
            m_navMeshAgent.speed = UnitData.speed;
            m_navMeshAgent.angularSpeed = UnitData.angularSpeed;
            m_navMeshAgent.acceleration = UnitData.acceleration;
        }
    }

    #region IRepairable

    override public bool NeedsRepairing() => HealthPoint < UnitData.maxHP;
  
    override public void Repair(float amount)
    {
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
        Stop();
        m_target = null;

        //if (m_target != null)
        //  StopCapture();

        if (m_navMeshAgent)
        {
            m_navMeshAgent.SetDestination(pos);
            m_navMeshAgent.isStopped = false;
        }
    }

    public void AddPatrolPoint(Vector3 pos)
    {
        m_patrolPoint.Add(pos);
    }

    public override void Stop()
    {
        base.Stop();

        m_patrolPoint.Clear();
    }

    public virtual void MoveTo(Transform target) => MoveTo(target.position);
    public virtual void MoveTo(Entity target) => MoveTo(target.transform.position);

    public virtual void MoveToward(Vector3 velocity) => m_navMeshAgent.Move(velocity);
}
