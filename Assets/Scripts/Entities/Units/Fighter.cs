using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            if (m_target.Team != Team && m_target.Team != ETeam.Neutral) ComputeAttack();
        }
    }

    #endregion


    // Targetting Task - attack
    public void SetAttackTarget(Entity target)
    {
        if (CanAttack(target) && target.Team != Team)
            StartAttacking(target);
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

    // Attack Task
    public void StartAttacking(Entity target) =>  m_target = target;

    public void ComputeAttack()
    {
        if (CanAttack(m_target) == false)
            return;

        if (m_navMeshAgent)
            m_navMeshAgent.isStopped = true;

        transform.LookAt(m_target.transform);

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
                newBullet.GetComponent<Bullet>().ShootToward(m_target.transform.position - transform.position, this);
            }

            // apply damages
            int damages = Mathf.FloorToInt(m_fighterData.dps * m_fighterData.attackFrequency);
            m_target.AddDamage(damages);
        }
    }
}
