using UnityEngine;

public class UnitLeader : Unit
{
    private Vector3? m_targetPosition = null;
    private Transform m_targetTransform = null;

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
            
                if (m_targetTransform is not null)
                    return m_targetTransform.position;
            }

            return transform.position;
        }
    }

    public override void MoveTo(Vector3 target)
    {
        m_targetPosition = target;

        base.MoveTo(target);
    }

    public override void MoveTo(Transform target)
    {
        m_targetTransform = target;

        base.MoveTo(target);
    }

    public override void MoveToward(Vector3 velocity)
    {
        m_targetTransform = null;
        m_targetPosition = null;

        base.MoveToward(velocity);
    }

    public bool HasReachedPos(float epsilon)
    {
        return m_navMeshAgent.remainingDistance - m_navMeshAgent.stoppingDistance <= epsilon;
    }

    private new void Update()
    { 
        if (!HasReachedPos(m_targetDistanceEpsilon))
            m_squad.UpdatePositions();
    }
}
