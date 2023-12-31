﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Entity;

[CreateAssetMenu(fileName = "Factory_Data", menuName = "RTS/Buildings/Factory Data", order = 1)]
public class FactoryDataScriptable : BuildingDataScriptable
{
    [Header("Spawn Unit Settings")]
    public int nbSpawnSlots = 10;
    public int spawnRadius = 12;
    public int radiusOffset = 4;

    public float resourceConsumptionPerSecond = 10f;

    [Header("Available Entities")]
    public GameObject[] availableUnits = null;

    private List<Command> m_factoryCommand;
    public override Command[] Commands => base.Commands.Concat(m_factoryCommand).ToArray();

    protected new void OnEnable()
    {
        base.OnEnable();

        m_factoryCommand ??= new List<Command>();

        foreach (GameObject unitPrefab in availableUnits)
        {
            if (unitPrefab.TryGetComponent(out Unit unit))
            {
                m_factoryCommand.Add(new BuildCommand(
                    $"Factory_Produce_{unitPrefab.name}",
                    unit.EntityData.icon,
                    Factory.Command_RequestUnitProduction,
                    Factory.Command_CancelUnitProduction,
                    Factory.Command_GetQueueCountOfUnit,
                    unitPrefab
                ));
            }
        }
    }

    protected new void OnDisable()
    {
        base.OnDisable();

        m_factoryCommand.Clear();
    }
}
