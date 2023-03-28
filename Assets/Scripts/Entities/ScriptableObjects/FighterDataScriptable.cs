using UnityEngine;

[CreateAssetMenu(fileName = "Fighter_Data", menuName = "RTS/Units/Fighter Data", order = 0)]
public class FighterDataScriptable : UnitDataScriptable
{
    [Header("Combat")]
    public int dps = 10;
    public float attackFrequency = 1f;
    public float attackDistanceMax = 10f;
    public float captureDistanceMax = 10f;

    public GameObject bulletPrefab = null;
}
