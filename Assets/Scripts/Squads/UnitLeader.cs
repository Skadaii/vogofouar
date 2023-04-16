using AIPlanner;
using AIPlanner.GOAP;
using System.Collections.Generic;
using UnityEngine;

public class UnitLeader : Unit
{
    private Vector3? m_targetPosition = null;

    [SerializeField] private float m_targetDistanceEpsilon = 0.1f;
    [SerializeField] private bool m_usePrediction = false;

    [SerializeField]
    private UnitDataScriptable m_leaderData = null;

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

    [StateMethod]
    public BoolType HasTargetPosition() => new BoolType() { Value = m_targetPosition.HasValue };
    public void SetTargetPosition(Vector3 target) => m_targetPosition = target;

    [ConsiderationMethod]
    public float MoveToTargetGoal(WorldState worldState) => m_targetPosition.HasValue ? 1f : 0f;

    public override void MoveToward(Vector3 velocity)
    {
        m_targetPosition = null;

        base.MoveToward(velocity);
    }

    public bool HasReachedPos(float epsilon)
    {
        return m_navMeshAgent.remainingDistance - m_navMeshAgent.stoppingDistance <= epsilon;
    }

    [ActionMethod]
    public Action.EActionState CheckHasReachedTarget(WorldState worldState)
    {
        if (HasReachedPos(m_targetDistanceEpsilon))
        {
            m_targetPosition = null;
            return Action.EActionState.Finished;
        }

        return Action.EActionState.Loading;
    }

    [ActionMethod]
    public Action.EActionState MoveToTarget(WorldState worldState)
    {
        if (!m_targetPosition.HasValue)
            return Action.EActionState.Failed;

        MoveTo(m_targetPosition.Value);
        return Action.EActionState.Finished;
    }

    private new void Update()
    {
        if (!HasReachedPos(m_targetDistanceEpsilon))
            m_squad.UpdatePositions();
    }

    [StateMethod]
    public bool IsNearEnemies()
    {
        foreach (Unit unit in m_squad.Units)
        {
            if (unit as Fighter)
            {
                FighterDataScriptable fighterData = unit.UnitData as FighterDataScriptable;

                if (Physics.CheckSphere(unit.transform.position, fighterData.attackDistanceMax))
                    return true;
            }
        }

        return false;
    }

    [ActionMethod]
    public Action.EActionState Attack(WorldState worldState)
    {
        return Action.EActionState.Finished;
    }

    [ActionMethod]
    public Action.EActionState AttackTarget(WorldState worldState)
    {
        return Action.EActionState.Finished;
    }

    public void CheckUnits()
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
    }
}
