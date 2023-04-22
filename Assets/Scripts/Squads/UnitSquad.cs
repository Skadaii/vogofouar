using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UnitSquad
{
    #region Variables

    private FormationRule m_formation = null;
    public FormationRule Formation
    {
        get => m_formation;

        set
        {
            m_formation = value;
            UpdatePositions();
        }
    }

    public UnitLeader m_leaderComponent = null;

    private List<Unit> m_units = new List<Unit>();
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

    public void InitializeLeader(GameObject leaderPrefab, Vector3 squadMiddle)
    {
        // If leader is null, set a virtual one
        m_leaderComponent ??= CreateVirtuaLeader(leaderPrefab, squadMiddle, Quaternion.identity);

        m_leaderComponent.Squad = this;

        //m_leaderComponent.m_onMoveChange += UpdatePositions;
    }

    public void UpdatePositions()
    {
        if (m_units.Count == 1)
        {
            m_units.First().MoveTo(m_leaderComponent.SquadTargetCenter);
            return;
        }

        for (int i = 0; i < m_units.Count; i++)
        {
            Unit unit = m_units[i];

            if (unit && !unit.gameObject.activeInHierarchy)
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

    UnitLeader CreateVirtuaLeader(GameObject leaderPrefab, Vector3 leaderPosition, Quaternion leaderRotation)
    {
        GameObject leader = UnityEngine.Object.Instantiate(leaderPrefab, leaderPosition, leaderRotation);

        return leader.GetComponent<UnitLeader>();
    }
    #endregion
}
