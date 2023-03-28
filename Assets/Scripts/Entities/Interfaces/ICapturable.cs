using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICapturable
{
    void Capture(int amount, ETeam team);
    void FinalizeCapture(ETeam team);
    public ETeam CapturingTeam { get; protected set; }
}
