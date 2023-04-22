using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Entity;

[CreateAssetMenu(fileName = "DefaultBuilding_data", menuName = "RTS/Buildings/Default building", order = 1)]
public class BuildingDataScriptable : EntityDataScriptable
{
    [Header("FX")]
    public GameObject deathFXPrefab = null;

    private List<Command> m_buildingCommands;
    public override Command[] Commands => base.Commands.Concat(m_buildingCommands).ToArray();

    protected new void OnEnable()
    {
        base.OnEnable();

        m_buildingCommands = new List<Command>
        {
        };
    }

    protected new void OnDisable()
    {
        base.OnDisable();

        m_buildingCommands.Clear();
    }
}
