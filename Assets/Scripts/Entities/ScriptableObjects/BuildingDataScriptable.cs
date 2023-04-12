using UnityEngine;

[CreateAssetMenu(fileName = "DefaultBuilding_data", menuName = "RTS/Buildings/Default building", order = 1)]
public class BuildingDataScriptable : EntityDataScriptable
{
    public float constructionCost = 5f;

    [Header("FX")]
    public GameObject deathFXPrefab = null;
}
