using UnityEngine;

[CreateAssetMenu(fileName = "Factory_Data", menuName = "RTS/FactoryData", order = 1)]
public class FactoryDataScriptable : EntityDataScriptable
{
    [Header("Spawn Unit Settings")]
    public int nbSpawnSlots = 10;
    public int spawnRadius = 12;
    public int radiusOffset = 4;

    [Header("Available Entities")]
    public GameObject[] availableUnits = null;
    public GameObject[] availableFactories = null;

    [Header("FX")]
    public GameObject deathFXPrefab = null;
}
