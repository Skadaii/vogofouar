using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.CanvasScaler;

[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public abstract class Unit : Entity
{
    //  Variables
    //  ---------

    protected float m_lastActionDate = 0f;
    protected NavMeshAgent m_navMeshAgent;
    protected Entity m_target = null;
    //  Squad system variables

    protected UnitSquad m_squad = null;

    private List<Vector3> m_patrolPoint = new List<Vector3>();
    private int m_patrolIndex = 0;

    //  Properties
    //  ----------
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
            {
                onDeathEvent += () => m_squad.RemoveUnit(this);
                m_squad.Units.Add(this);
            }
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

        if (m_target != null)
        {
            if (m_target.Team != Team)
            {
                if (m_target is StaticBuilding && CanCapture(m_target)) ComputeCapture();
            }
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

    public virtual void ResetTarget() => m_target = null; 

    #region IRepairable

    override public bool NeedsRepairing() => HealthPoint < UnitData.maxHP;
  
    override public float Repair(float amount)
    {
        return base.Repair(amount);
    }
    override public void FullRepair()
    {
        Repair(UnitData.maxHP);
    }

    #endregion

    #region Capture methods
    
    //  Capture Task
    public virtual void SetCaptureTarget(StaticBuilding target)
    {
        m_target = target;

        if (target == null) return;

        if (CanCapture(target)) return;

        if (m_navMeshAgent)
        {
            m_navMeshAgent.SetDestination(target.transform.position);
            m_navMeshAgent.isStopped = false;
        }
    }

    private void ComputeCapture()
    {
        StaticBuilding buildingToCapture = m_target as StaticBuilding;
        if (buildingToCapture == null) return;

        if (m_navMeshAgent) m_navMeshAgent.isStopped = true;

        buildingToCapture.ComputeCapture(this);
    }

    public bool CanCapture(Entity target)
    {
        if (target == null || target is not StaticBuilding) return false;

        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > UnitData.captureDistance * UnitData.captureDistance)
            return false;

        return true;
    }


    public bool IsCapturing()
    {
        StaticBuilding buildingToCapture = m_target as StaticBuilding;
        return buildingToCapture != null;
    }

    #endregion


    #region Squad methods : Alert
  
    #endregion

    // Moving Task
    public virtual void MoveTo(Vector3 pos)
    {
        Stop();

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

    #region Commands

    public static void Command_MoveTo(Entity entity, Entity target)
    {
        ((Unit)entity)?.Squad.m_leaderComponent.SetTargetPosition(target.transform.position);
    }

    public static void Command_MoveTo(Entity entity, Vector3 pos)
    {
        ((Unit)entity)?.Squad.m_leaderComponent.SetTargetPosition(pos);
    }

    public static void Command_AddPatrolPoint(Entity entity, Vector3 pos)
    {
        //TODO:
        ((Unit)entity)?.AddPatrolPoint(pos);
    }

    public static void Command_Capture(Entity entity, Entity target)
    {
        ((Unit)entity)?.Squad.m_leaderComponent.SetTarget(target, ETargetType.Capture);
    }

    public static bool Command_CanCaptureTarget(Entity entity, Entity target)
    {
        return entity && target && (target.Team != entity.Team) && target is StaticBuilding;
    }

    #endregion
}
