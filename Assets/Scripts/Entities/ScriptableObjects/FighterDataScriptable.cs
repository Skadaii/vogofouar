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

    protected new void OnEnable()
    {
        base.OnEnable();

        m_fighterCommands = new List<Command>
        {
            new TargetCommand("Fighter_Attack", Resources.Load<Sprite>("Textures/Sprites/Commands/attack_icon"), Fighter.Command_Attack,Fighter.Command_CanAttackTarget)
        };
    }

    protected new void OnDisable()
    {
        base.OnDisable();

        m_fighterCommands.Clear();
    }
}
