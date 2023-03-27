using UnityEngine;

public class EntityDataScriptable : ScriptableObject
{
    [Header("Build Data")]
    public int typeId = 0;
    public string caption = "Unknown Unit";
    public int cost = 1;
    public float buildDuration = 1f;

    [Header("Health Points")]
    public int maxHP = 100;
}
