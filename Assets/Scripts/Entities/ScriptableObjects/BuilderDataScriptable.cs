using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Entity;

[CreateAssetMenu(fileName = "Builder_Data", menuName = "RTS/Units/Builder Data", order = 0)]
public class BuilderDataScriptable : UnitDataScriptable
{
    [Header("Building")]
    public float bps = 10f;
    public float buildingDistanceMax = 10f;

    [Header("Repairing")]
    public bool canRepair = false;
    public int rps = 10;
    public float repairFrequency = 1f;
    public float repairDistanceMax = 10f;

    public float captureDistanceMax = 10f;
    public GameObject[] availableBuildings = null;

    private List<Command> m_builderCommands;
    public override Command[] Commands => base.Commands.Concat(m_builderCommands).ToArray();

    protected new void OnEnable()
    {
        base.OnEnable();

        m_builderCommands = new List<Command>
        {
            new TargetCommand($"Builder_Build", Resources.Load<Sprite>("Textures/Sprites/Commands/build_icon"), Builder.Command_Build, Builder.Command_CanBuildTarget)
        };

        foreach (GameObject buildingPrefab in availableBuildings)
        {
            if (buildingPrefab.TryGetComponent(out Building building))
            {
                m_builderCommands.Add(new BuildCommand($"Builder_Build_{buildingPrefab.name}", building.EntityData.icon, Builder.Command_RequestBuild, toBuild: buildingPrefab));
            }
        }
    }
    protected new void OnDisable()
    {
        base.OnDisable();

        m_builderCommands.Clear();
    }
}
