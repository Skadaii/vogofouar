using UnityEngine;

[CreateAssetMenu(fileName = "Builder_Data", menuName = "RTS/Units/Builder Data", order = 0)]
public class BuilderDataScriptable : UnitDataScriptable
{
    [Header("Building")]
    public bool canBuild = false;
    public float bps = 10f;
    public float buildingFrequency = 1f;
    public float buildingDistanceMax = 10f;

    [Header("Repairing")]
    public bool canRepair = false;
    public int rps = 10;
    public float repairFrequency = 1f;
    public float repairDistanceMax = 10f;

    public float captureDistanceMax = 10f;
    public GameObject[] availableBuildings = null;
}
