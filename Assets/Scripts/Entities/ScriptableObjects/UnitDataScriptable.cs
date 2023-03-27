using UnityEngine;

[CreateAssetMenu(fileName = "Unit_Data", menuName = "RTS/UnitData", order = 0)]
public class UnitDataScriptable : EntityDataScriptable
{
    [Header("Combat")]
    public int dps = 10;
    public float attackFrequency = 1f;
    public float attackDistanceMax = 10f;
    public float captureDistanceMax = 10f;

    [Header("Repairing")]
    public bool canRepair = false;
    public int rps = 10;
    public float repairFrequency = 1f;
    public float repairDistanceMax = 10f;

    [Header("Movement")]
    [Tooltip("Overrides NavMeshAgent steering settings")]
    public float speed = 10f;
    public float angularSpeed = 200f;
    public float acceleration = 20f;
    public bool isFlying = false;

    [Header("FX")]
    public GameObject bulletPrefab = null;
    public GameObject deathFXPrefab = null;
}
