using UnityEngine;

/// <summary>
/// Fighters can only destroy enemies
/// </summary>
public class Fighter : Unit
{
    //  Variables
    //  ---------

    [SerializeField]
    private FighterDataScriptable m_fighterData = null;

    private Transform m_bulletSlot;

    //  Properties
    //  ----------

    public override UnitDataScriptable UnitData => m_fighterData;

    //  Functions
    //  ---------

    #region MonoBehaviour methods

    protected virtual new void Awake()
    {
        base.Awake();

        m_bulletSlot = transform.Find("BulletSlot");
    }

    protected virtual new void Update()
    {
        base.Update();

        if (m_target != null)
        {
            if (m_target.Team != Team && m_target.Team != ETeam.Neutral)
            {
                if(m_target is not StaticBuilding) ComputeAttack(m_target);
            }
        }
    }

    #endregion


    // Targetting Task - attack
    public void SetAttackTarget(Entity target)
    {
        if (target.Team == Team) return;

        m_target = target;

        if (CanAttack(target)) return;

        if (m_navMeshAgent)
        {
            m_navMeshAgent.SetDestination(target.transform.position);
            m_navMeshAgent.isStopped = false;
        }
    }

    public bool CanAttack(Entity target)
    {
        if (target == null)
            return false;

        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > m_fighterData.attackDistanceMax * m_fighterData.attackDistanceMax)
            return false;

        return true;
    }

    public void ComputeAttack(Entity target)
    {
        if (CanAttack(target) == false)
            return;

        if (m_navMeshAgent)
            m_navMeshAgent.isStopped = true;

        transform.LookAt(target.transform);

        // only keep Y axis
        transform.eulerAngles = transform.eulerAngles.y * Vector3.up;

        if ((Time.time - m_lastActionDate) > m_fighterData.attackFrequency)
        {
            m_lastActionDate = Time.time;

            // visual only ?
            if (m_fighterData.bulletPrefab)
            {
                GameObject newBullet = Instantiate(m_fighterData.bulletPrefab, m_bulletSlot);
                newBullet.transform.parent = null;
                newBullet.GetComponent<Bullet>().ShootToward(target.transform.position - transform.position, this);
            }

            // apply damages
            int damages = Mathf.FloorToInt(m_fighterData.dps * m_fighterData.attackFrequency);
            target.AddDamage(damages);
        }
    }

    #region Commands

    public static void Command_Attack(Entity entity, Entity target)
    {
        if(entity is Fighter)
        {
            (entity as Fighter)?.Squad.m_leaderComponent.SetTarget(target, ETargetType.Attack);
        }
    }

    public static bool Command_CanAttackTarget(Entity entity, Entity target)
    {
        return entity && target && (target.Team != entity.Team) && target is not StaticBuilding;
    }


    #endregion
}
