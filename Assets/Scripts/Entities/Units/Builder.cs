using UnityEngine;


/// <summary>
/// The builder can repair, build and capture other entities, but is completely inoffensive
/// </summary>
public class Builder : Unit
{
    //  Variables
    //  ---------

    [SerializeField]
    private BuilderDataScriptable m_builderData = null;

    private Building m_buildingTarget;

    //  Properties
    //  ----------
    public override UnitDataScriptable UnitData => m_builderData;

    //  Functions
    //  ---------


    #region MonoBehaviour methods

    protected virtual new void Awake()
    {
        base.Awake();
    }

    protected virtual new void Update()
    {
        base.Update();

        if(m_buildingTarget != null)
        {
            if(Vector3.SqrMagnitude(m_buildingTarget.transform.position - transform.position) <= m_builderData.buildingDistanceMax * m_builderData.buildingDistanceMax)
            {
                if (m_navMeshAgent) m_navMeshAgent.isStopped = true;

                float resource = Mathf.Min(m_builderData.bps * Time.deltaTime, TeamController.CurrentResources);

                TeamController.CurrentResources -= resource;

                float extra = m_buildingTarget.Repair(resource);

                if(extra > 0)
                {
                    TeamController.CurrentResources += extra;
                    m_buildingTarget = null;
                }
            }
        }
    }

    #endregion

    // Repairing Task
    public bool CanRepair(Entity target)
    {
        if (m_builderData.canRepair == false || target == null)
            return false;

        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > m_builderData.repairDistanceMax * m_builderData.repairDistanceMax)
            return false;

        return true;
    }
    public void StartRepairing(Entity entity)
    {
        if (m_builderData.canRepair)
        {
            m_target = entity;
        }
    }

    public void Build(Entity target)
    {
        MoveTo(target);
        m_buildingTarget = target as Building;
    }
    

    public void RequestBuild(GameObject building)
    {
        PlayerController pc = TeamController as PlayerController;

        if(pc != null)
        {
            pc.BuildPreview(building);
        }
    }

    public void ComputeRepairing()
    {
        if (CanRepair(m_target) == false)
            return;

        if (m_navMeshAgent)
            m_navMeshAgent.isStopped = true;

        transform.LookAt(m_target.transform);
        // only keep Y axis
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = 0f;
        eulerRotation.z = 0f;
        transform.eulerAngles = eulerRotation;

        if ((Time.time - m_lastActionDate) > m_builderData.repairFrequency)
        {
            m_lastActionDate = Time.time;

            // apply reparing
            int amount = Mathf.FloorToInt(m_builderData.rps * m_builderData.repairFrequency);
            m_target.Repair(amount);
        }
    }

    #region Commands

    public static void Command_RequestBuild(Entity entity, GameObject building)
    {
        ((Builder)entity)?.RequestBuild(building);
    }

    public static void Command_Build(Entity entity, Entity target)
    {
        ((Builder)entity)?.Build(target);
    }
    #endregion
}
