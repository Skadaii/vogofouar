using AIPlanner;
using AIPlanner.GOAP;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.CanvasScaler;

public enum ETargetType
{
    Move,
    Capture,
    Attack,

    None
}

[StateType]
public class TargetType : StateType<ETargetType>
{
    public TargetType(ETargetType Value) : base(Value) { }
    public TargetType() { }
}

public class UnitLeader : Unit
{
    private Vector3? m_targetPosition = null;
    private Vector3? m_currentTargetPosition = null;

    private Entity m_currentTarget;

    private bool m_isTargetChanged = false;
    private bool m_isEnemyTargetSetted = false;

    private int m_layerMask;

    [SerializeField] private float m_targetDistanceEpsilon = 0.1f;
    [SerializeField] private bool m_usePrediction = false;

    [SerializeField] private UnitDataScriptable m_leaderData = null;

    [SerializeField] private GOAP m_goap;

    public override UnitDataScriptable UnitData => m_leaderData;

    public override UnitSquad Squad
    {
        get => m_squad;
        set
        {
            if (m_squad is not null)
                m_squad.m_leaderComponent = null;

            m_squad = value;

            if (m_squad is not null)
                m_squad.m_leaderComponent = this;
        }
    }

    public Vector3 SquadTargetCenter
    {
        get
        {
            if (m_usePrediction)
            {
                if (m_targetPosition.HasValue)
                    return m_targetPosition.Value;
            }

            return transform.position;
        }
    }

    public void SetTargetPosition(Vector3? target)
    {
        m_targetPosition = target;

        if (target != null)
        {
            m_isTargetChanged = true;

            m_goap.WorldState.SetState("TargetType", new TargetType(ETargetType.Move));
        }
    }

    public void SetTarget(Entity target)
    {
        m_target = target;

        if (target == null)
            return;

        m_isTargetChanged = true;

        if (target as Unit || target as Factory)
        {
            m_goap.WorldState.SetState("TargetType", new TargetType(ETargetType.Attack));
        }
        else // (target as StaticBuilding)
        {
            m_goap.WorldState.SetState("TargetType", new TargetType(ETargetType.Capture));
        }

    }

    public override void MoveToward(Vector3 velocity)
    {
        m_targetPosition = null;

        base.MoveToward(velocity);
    }

    public bool HasReachedPos(float epsilon)
    {
        return m_navMeshAgent.remainingDistance - m_navMeshAgent.stoppingDistance <= epsilon;
    }

    protected override void Awake()
    {
        base.Awake();

        m_layerMask = LayerMask.GetMask("Building", "Unit");

        m_visibility = null;
    }

    protected virtual new void Update()
    {

    }

    [ConsiderationMethod]
    public float MoveToTargetGoal(WorldState worldState) => 
        ((TargetType)worldState.GetState("TargetType")).Value != ETargetType.None ? 1f : 0f;

    [StateMethod]
    public BoolType IsNearToCaptureTarget()
    {
        if (m_target == null || m_target is not StaticBuilding)
            return new BoolType(false);

        //TODO : remove hardcoded distance '10f'
        return new BoolType((m_target.transform.position - transform.position).sqrMagnitude <= 10f);
    }

    [StateMethod]
    public BoolType IsNearToAttackTarget()
    {
        if (m_target == null || (m_target is not Unit && m_target is not Factory))
            return new BoolType(false);

        //TODO : remove hardcoded distance '100f'
        return new BoolType((m_target.transform.position - transform.position).sqrMagnitude <= 100f);
    }

    [ConsiderationMethod]
    public float IsNearEnemiesGoal(WorldState worldState)
    {
        foreach (Unit unit in m_squad.Units)
        {
            if (unit as Fighter)
            {
                FighterDataScriptable fighterData = unit.UnitData as FighterDataScriptable;

                Collider[] colliders = Physics.OverlapSphere(unit.transform.position, fighterData.attackDistanceMax, m_layerMask);

                if (colliders != null)
                {
                    foreach (Collider collider in colliders)
                    {
                        Entity entity;
                        if (collider.gameObject.TryGetComponent(out entity) && entity.Team != unit.Team)
                        {
                            if (entity is not StaticBuilding)
                            {
                                return 1f;
                            }
                        }
                    }
                }
            }
        }

        return 0f;
    }

    [StateMethod]
    public BoolType ResetIsEnemiesHurted() => new BoolType(false);

    private Action.EActionState MoveToTarget()
    {
        if (!m_targetPosition.HasValue)
            return Action.EActionState.Failed;

        if (!m_currentTargetPosition.HasValue)
        {
            m_currentTargetPosition = m_targetPosition;
            MoveTo(m_targetPosition.Value);
            return Action.EActionState.Loading;
        }

        //If the targetPosition has changed by the controller
        if (m_currentTargetPosition != m_targetPosition)
        {
            m_currentTargetPosition = null;
            return Action.EActionState.Failed;
        }

        if (HasReachedPos(m_targetDistanceEpsilon))
        {
            m_targetPosition = null;
            m_currentTargetPosition = null;
            return Action.EActionState.Finished;
        }

        m_squad.UpdatePositions();

        return Action.EActionState.Loading;
    }

    [ActionMethod]
    public Action.EActionState MoveToTarget(WorldState worldState)
    {
        /*if (!m_currentTargetPosition.HasValue || m_currentTarget != null)
        {
            m_target = null;
            m_currentTarget = null;
            m_currentTargetPosition = null;
        }*/

        Action.EActionState actionState = MoveToTarget();

        return actionState;
    }

    private void ResetUnitsCaptureTarget()
    {
        m_currentTarget = null;

        foreach (Unit unit in m_squad.Units)
            if (unit != null)
                unit.SetCaptureTarget(null);
    }

    [ActionMethod]
    public Action.EActionState MoveToCaptureTarget(WorldState worldState)
    {
        StaticBuilding staticBuilding = m_target as StaticBuilding;

        if (staticBuilding == null)
        {
            if (((TargetType)m_goap.WorldState.GetState("TargetType")).Value == ETargetType.Capture)
                m_goap.WorldState.SetState("TargetType", new TargetType(ETargetType.None));

            m_currentTarget = null;

            return Action.EActionState.Failed;
        }

        if (m_currentTarget == null)
        {
            m_currentTargetPosition = null;
            m_targetPosition = m_target.transform.position;
            m_currentTarget = staticBuilding;
        }

        Action.EActionState actionState = MoveToTarget();

        if (m_currentTarget != staticBuilding || actionState == Action.EActionState.Failed)
        {
            m_currentTarget = null;
            m_currentTargetPosition = null;
            return Action.EActionState.Failed;
        }

        if (staticBuilding.Team == Team)
        {
            m_target = null;
            m_currentTarget = null;
            m_targetPosition = null;
            m_currentTargetPosition = null;
            return Action.EActionState.Finished;
        }

        if (actionState == Action.EActionState.Finished)
            m_currentTarget = null;

        return actionState;
    }

    [ActionMethod]
    public Action.EActionState CaptureTarget(WorldState worldState)
    {
        StaticBuilding staticBuilding = m_target as StaticBuilding;

        if (staticBuilding == null)
        {
            if (((TargetType)m_goap.WorldState.GetState("TargetType")).Value == ETargetType.Capture)
                m_goap.WorldState.SetState("TargetType", new TargetType(ETargetType.None));

            ResetUnitsCaptureTarget();
            return Action.EActionState.Failed;
        }

        if (m_currentTarget == null)
        {
            m_targetPosition = null;
            m_currentTargetPosition = null;

            m_currentTarget = staticBuilding;
            foreach (Unit unit in m_squad.Units)
                if (unit != null)
                    unit.SetCaptureTarget(staticBuilding);

            return Action.EActionState.Loading;
        }

        if (m_currentTarget != staticBuilding)
        {
            ResetUnitsCaptureTarget();
            return Action.EActionState.Failed;
        }

        if (staticBuilding.Team == Team)
        {
            m_target = null;

            ResetUnitsCaptureTarget();
            return Action.EActionState.Finished;
        }

        return Action.EActionState.Loading;
    }

    [ActionMethod]
    public Action.EActionState MoveToAttackTarget(WorldState worldState)
    {
        if ((m_target == null || (m_target is not Factory && m_target is not Unit)) && !m_isEnemyTargetSetted)
        {
            if (((TargetType)m_goap.WorldState.GetState("TargetType")).Value == ETargetType.Capture)
                m_goap.WorldState.SetState("TargetType", new TargetType(ETargetType.None));

            m_currentTarget = null;
            return Action.EActionState.Failed;
        }

        if (m_currentTarget == null && !m_isEnemyTargetSetted && m_isTargetChanged)
        {
            m_isEnemyTargetSetted = true;
            m_isTargetChanged = false;

            m_currentTargetPosition = null;
            m_targetPosition = m_target.transform.position;
            m_currentTarget = m_target;
        }

        Action.EActionState actionState = MoveToTarget();

        if (m_isTargetChanged || actionState == Action.EActionState.Failed)
        {
            m_currentTarget = null;
            m_currentTargetPosition = null;

            m_isEnemyTargetSetted = false;
            m_isTargetChanged = true;

            return Action.EActionState.Failed;
        }

        if (m_target == null && !m_isTargetChanged)
        {
            m_target = null;
            m_currentTarget = null;
            m_targetPosition = null;
            m_currentTargetPosition = null;

            m_isEnemyTargetSetted = false;
            return Action.EActionState.Finished;
        }

        if (actionState == Action.EActionState.Finished)
        {
            m_isEnemyTargetSetted = false;
            m_isTargetChanged = true;

            m_currentTarget = null;
        }

        return actionState;
    }

    [ActionMethod]
    public Action.EActionState AttackTarget(WorldState worldState)
    {
        if ((m_target == null || (m_target is not Factory && m_target is not Unit)) && !m_isEnemyTargetSetted)
        {
            if (((TargetType)m_goap.WorldState.GetState("TargetType")).Value == ETargetType.Attack)
                m_goap.WorldState.SetState("TargetType", new TargetType(ETargetType.None));

            ResetUnitsCaptureTarget();
            return Action.EActionState.Failed;
        }

        if (m_currentTarget == null && !m_isEnemyTargetSetted && m_isTargetChanged)
        {
            m_isEnemyTargetSetted = true;
            m_isTargetChanged = false;

            m_targetPosition = null;
            m_currentTargetPosition = null;

            m_currentTarget = m_target;

            foreach (Unit unit in m_squad.Units)
            {
                Fighter fighter = unit as Fighter;

                if (fighter != null)
                    fighter.SetAttackTarget(m_target);
            }

            return Action.EActionState.Loading;
        }

        if (m_isTargetChanged)
        {
            m_isEnemyTargetSetted = false;

            ResetUnitsCaptureTarget();
            return Action.EActionState.Failed;
        }

        if (m_target == null && !m_isTargetChanged)
        {
            m_isEnemyTargetSetted = false;
            return Action.EActionState.Finished;
        }

        return Action.EActionState.Loading;
    }

    [ActionMethod]
    public Action.EActionState AutoAttack(WorldState worldState)
    {
        foreach (Unit unit in m_squad.Units)
        {
            if (unit as Fighter)
            {
                FighterDataScriptable fighterData = unit.UnitData as FighterDataScriptable;

                Collider[] colliders = Physics.OverlapSphere(unit.transform.position, fighterData.attackDistanceMax, m_layerMask);

                if (colliders != null)
                {
                    Entity closestTarget = null;
                    float closestDistanceSqr = float.MaxValue;

                    foreach (Collider collider in colliders)
                    {
                        Entity entity;
                        if (collider.gameObject.TryGetComponent(out entity) && entity.Team != unit.Team)
                        {
                            float currentDistanceSqr = (entity.transform.position - unit.transform.position).sqrMagnitude;

                            if (entity is not StaticBuilding && currentDistanceSqr < closestDistanceSqr)
                            {
                                closestTarget = entity;
                                closestDistanceSqr = currentDistanceSqr;
                            }
                                
                        }
                    }

                    if (closestTarget)
                    {
                        (unit as Fighter).ComputeAttack(closestTarget);
                    }
                }
            }
        }

        return Action.EActionState.Finished;
    }

    /*[ActionMethod]
    public Action.EActionState AttackTarget(WorldState worldState)
    {
        if ((m_target as Unit) == null && (m_target as Factory) == null)
            return Action.EActionState.Failed;

        m_targetPosition = null;

        foreach (Unit unit in m_squad.Units)
        {
            Fighter fighter = unit as Fighter;

            if (fighter != null)
                fighter.SetAttackTarget(m_target);
        }

        return Action.EActionState.Finished;
    }

    [ActionMethod]
    public Action.EActionState CheckEnemyHasKilled(WorldState worldState)
    {
        if (m_target == null)
            return Action.EActionState.Finished;

        return Action.EActionState.Loading;
    }*/

    /*public void CheckUnits()
    {
        List<Entity> entities = new List<Entity>();

        foreach (Unit unit in m_squad.Units)
        {
            if (unit as Fighter)
            {
                FighterDataScriptable fighterData = unit.UnitData as FighterDataScriptable;
                //fighterData.attackDistanceMax

                Collider[] colliders = Physics.OverlapSphere(unit.transform.position, fighterData.attackDistanceMax);

                if (colliders is not null)
                {
                    foreach (Collider collider in colliders)
                    {
                        Entity entity;
                        if (collider.gameObject.TryGetComponent(out entity) && entity.Team != unit.Team)
                        {
                            //if (!entities.Contains(entity))
                            //    entities.Add(entity);

                            if (entity is not StaticBuilding)
                            {
                                (unit as Fighter).SetAttackTarget(entity);
                            }
                        }
                    }
                }
            }
        }
    }*/
}
