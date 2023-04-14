using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Entity;

[CreateAssetMenu(fileName = "Fighter_Data", menuName = "RTS/Units/Fighter Data", order = 0)]
public class FighterDataScriptable : UnitDataScriptable
{
    [Header("Combat")]
    public int dps = 10;
    public float attackFrequency = 1f;
    public float attackDistanceMax = 10f;
    public float captureDistanceMax = 10f;

    public GameObject bulletPrefab = null;

    private List<Command> m_fighterCommands;
    public override Command[] Commands => base.Commands.Concat(m_fighterCommands).ToArray();

    protected new void OnValidate()
    {
        base.OnValidate();

        m_fighterCommands ??= new List<Command>
        {
            new TargetCommand("Attacj", newMethod: "Attack", icon: Resources.Load<Sprite>("Textures/T_cross"))
        };
    }
}
