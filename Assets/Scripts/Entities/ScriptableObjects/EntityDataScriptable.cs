﻿using System.Collections.Generic;
using UnityEngine;
using static Entity;

public class EntityDataScriptable : ScriptableObject
{
    public Sprite icon;

    [Header("Build Data")]
    public int typeId = 0;
    public string caption = "Unknown Unit";
    public int cost = 1;

    [Header("Health Points")]
    public int maxHP = 100;


    private List<Command> m_entityCommands;

    public virtual Command[] Commands => m_entityCommands.ToArray();

    protected void OnValidate()
    {
        m_entityCommands = new List<Command>
        {
            //new VoidCommand(newActionName: "Entity_Stop", newMethod:"Stop", icon: Resources.Load<Sprite>("Textures/Sprites/Commands/stop_icon"))
            new VoidCommand("Entity_Stop", Resources.Load<Sprite>("Textures/Sprites/Commands/stop_icon"), Entity.Command_Stop)
        };
    }
}
