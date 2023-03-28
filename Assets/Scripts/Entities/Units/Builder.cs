using System.Collections;
using System.Collections.Generic;
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

    //  Properties
    //  ----------

    public override UnitDataScriptable UnitData => m_builderData;

    //  Functions
    //  ---------

    // Targetting Task - capture
    public void SetCaptureTarget(Entity target)
    {
        if (CanCapture(target) == false)
            return;

        if (m_target != null)
            m_target = null;

        //if (IsCapturing())
        //    StopCapture();

        //if (target.Team != Team)
        //    StartCapture(target);
    }

    // Targetting Task - repairing
    public void SetRepairTarget(Entity entity)
    {
        if (CanRepair(entity) == false)
            return;

        //if (m_captureTarget != null)
        //    StopCapture();

        if (entity.Team == Team)
            StartRepairing(entity);
    }

    public bool CanCapture(Entity target)
    {
        if (target == null)
            return false;

        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > m_builderData.captureDistanceMax * m_builderData.captureDistanceMax)
            return false;

        return true;
    }

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


    //  Capture Task
    //public void StartCapture(Entity target)
    //{
    //    if (CanCapture(target) == false)
    //        return;

    //    if (m_navMeshAgent)
    //        m_navMeshAgent.isStopped = true;

    //    m_target = target;
    //    m_target.StartCapture(this);
    //}
    //public void StopCapture()
    //{
    //    if (m_target == null)
    //        return;

    //    m_target.StopCapture(this);
    //    m_target = null;
    //}

    //public bool IsCapturing()
    //{
    //    return m_captureTarget != null;
    //}
}
