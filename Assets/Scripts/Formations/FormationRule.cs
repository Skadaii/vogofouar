using UnityEngine;

[System.Serializable]
public class FormationRule : ScriptableObject
{
    virtual public Vector3 ComputePosition(Vector3 center, Quaternion rotation, int index)
    {
        return center;
    }
}
