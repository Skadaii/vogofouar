using UnityEngine;

[CreateAssetMenu(fileName = "DefaultBuilding_data", menuName = "RTS/Buildings/Default building", order = 1)]
public class BuildingDataScriptable : EntityDataScriptable
{
    [Header("FX")]
    public GameObject deathFXPrefab = null;
}
