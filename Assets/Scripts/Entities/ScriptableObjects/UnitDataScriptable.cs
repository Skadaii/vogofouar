using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Entity;

[CreateAssetMenu(fileName = "Unit_Data", menuName = "RTS/Units/Default Unit Data", order = 0)]
public class UnitDataScriptable : EntityDataScriptable
{
    [Header("Movement")]
    [Tooltip("Overrides NavMeshAgent steering settings")]
    public float speed = 10f;
    public float angularSpeed = 200f;
    public float acceleration = 20f;
    public bool isFlying = false;

    [Header("Capture")]
    public float captureDistance = 5f;
    public float capturePointPerSecond = 5f;

    [Header("Death FX")]
    public GameObject deathFXPrefab = null;

    private List<Command> m_unitCommands;

    public override Command[] Commands => base.Commands.Concat(m_unitCommands).ToArray();

    protected new void OnEnable()
    {
        base.OnEnable();

        m_unitCommands = new List<Command>
        {
            new LocationCommand("Unit_MoveToLocation", Resources.Load<Sprite>("Textures/Sprites/Commands/move_here_icon"), Unit.Command_MoveTo),
            new TargetCommand("Unit_MoveMoveToTarget", Resources.Load<Sprite>("Textures/Sprites/Commands/move_here_icon"), Unit.Command_MoveTo),
            new TargetCommand("Unit_CaptureTarget", Resources.Load<Sprite>("Textures/Sprites/Commands/capture_icon"), Unit.Command_Capture, Unit.Command_CanCaptureTarget),
            new LocationCommand("Unit_PatrolToLocation", Resources.Load<Sprite>("Textures/Sprites/Commands/patrol_icon"), Unit.Command_AddPatrolPoint)
        };
    }

    protected new void OnDisable()
    {
        base.OnDisable();

        m_unitCommands.Clear();
    }
}
