using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitSquad
{
    #region Variables

    public FormationRule m_formation = null;

    public UnitLeader m_leaderComponent = null;

    private List<Unit> m_units = new List<Unit>();

    public float m_defendRange = 2f;
    public float m_alertMaxDistance = 10f;

    #endregion

    #region Properties
    public List<Unit> Units
    {
        get => m_units;
        set
        {
            m_units = value;

            foreach (Unit unit in m_units)
                unit.Squad = this;
        }
    }

    public void RemoveUnit(Unit unit)
    {
        if (m_units.Contains(unit))
            unit.Squad = null;
    }

    public void AddUnit(Unit unit)
    {
        if (!m_units.Contains(unit))
            unit.Squad = this;
    }

    #endregion

    #region Functions
    public void Destroy()
    {
        UnityEngine.Object.Destroy(m_leaderComponent.gameObject);
    }
    public void InitializeLeader(GameObject leaderPrefab)
    {
        // If leader is null, set a virtual one
        m_leaderComponent ??= CreateVirtuaLeader(leaderPrefab);

        m_leaderComponent.Squad = this;

        m_leaderComponent.m_onMoveChange += UpdatePositions;
    }

    public void ReceiveAlert(Unit fromUnit)
    {
        foreach (Unit unit in m_units)
        {
            //  One for all, all for one
            if (Vector3.SqrMagnitude(unit.transform.position - fromUnit.transform.position) <= m_alertMaxDistance * m_alertMaxDistance)
                unit.Agressor ??= fromUnit.Agressor;
        }
    }

    public void UpdatePositions()
    {
        for (int i = 0; i < m_units.Count; i++)
        {
            Unit unit = m_units[i];

            if (unit && (!unit.gameObject.activeInHierarchy))
                continue;

            Vector3 pos = ComputeUnitPosition(i);
            unit.MoveTo(pos);
        }
    }

    Vector3 ComputeUnitPosition(int index)
    {
        if (m_formation)
            return m_formation.ComputePosition(m_leaderComponent.SquadTargetCenter, m_leaderComponent.transform.rotation, index);

        return m_leaderComponent.transform.position;
    }

    UnitLeader CreateVirtuaLeader(GameObject leaderPrefab)
    {
        GameObject leader = UnityEngine.Object.Instantiate(leaderPrefab);

        return leader.GetComponent<UnitLeader>();
    }

    #endregion
}
