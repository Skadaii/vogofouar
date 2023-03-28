using UnityEngine;

[CreateAssetMenu(fileName = "Factory_Data", menuName = "RTS/Buildings/Factory Data", order = 1)]
public class FactoryDataScriptable : BuildingDataScriptable
{
    [Header("Spawn Unit Settings")]
    public int nbSpawnSlots = 10;
    public int spawnRadius = 12;
    public int radiusOffset = 4;

    [Header("Available Entities")]
    public GameObject[] availableUnits = null;
    public GameObject[] availableFactories = null;
}
