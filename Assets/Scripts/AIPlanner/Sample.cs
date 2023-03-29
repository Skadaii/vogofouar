using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AIPlanner;
using AIPlanner.GOAP;

public class Sample : MonoBehaviour
{
    [ActionMethod]
    public Action.EActionState ActionSample(WorldState worldState)
    {
        return Action.EActionState.Finished;
    }

    [StateMethod]
    public BoolType StateSample()
    {
        return new BoolType(true);
    }

    [ConsiderationMethod]
    public float ConsiderationSample(WorldState worldState)
    {
        return 1f;
    }

    [ConsiderationMethod]
    public float ConsiderationSample2(WorldState worldState)
    {
        return 1f;
    }
}
