using UnityEngine;

[CreateAssetMenu(fileName = "Unit_Data", menuName = "RTS/Units/Default Unit Data", order = 0)]
public class UnitDataScriptable : EntityDataScriptable
{
    [Header("Movement")]
    [Tooltip("Overrides NavMeshAgent steering settings")]
    public float speed = 10f;
    public float angularSpeed = 200f;
    public float acceleration = 20f;
    public bool isFlying = false;

    [Header("Death FX")]
    public GameObject deathFXPrefab = null;
}
