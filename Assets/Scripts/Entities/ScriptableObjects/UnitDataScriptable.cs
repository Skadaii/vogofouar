﻿using System.Collections.Generic;
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

    [Header("Death FX")]
    public GameObject deathFXPrefab = null;

    private List<Command> m_unitCommands;

    public override Command[] Commands => base.Commands.Concat(m_unitCommands).ToArray();

    protected new void OnValidate()
    {
        base.OnValidate();

        m_unitCommands = new List<Command>
        {
            new LocationCommand(newActionName: "Move", newMethod:"MoveTo", icon: Resources.Load<Sprite>("Textures/T_Move")),
            new TargetCommand(newActionName: "Move", newMethod:"MoveTo", icon: Resources.Load<Sprite>("Textures/T_Move")),
            new LocationCommand(newActionName: "Patrol", newMethod:"AddPatrolPoint", icon: Resources.Load<Sprite>("Textures/T_Patrol"))
        };
    }
}
