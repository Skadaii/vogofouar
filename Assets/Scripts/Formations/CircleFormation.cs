using UnityEngine;
using System;

[CreateAssetMenu(fileName = "CircleFormation", menuName = "FormationRules/Circle", order = 2)]
public class CircleFormation : FormationRule
{
    [SerializeField]
    private int m_unitsPerCircle = 2;

    [SerializeField]
    private bool m_followRotation = true;

    [SerializeField]
    private float m_circleSpacing = 1.5f;

    [SerializeField]
    private float m_baseSpacing = 1.5f;

    [SerializeField]
    private Vector3 m_worldOffset = Vector3.zero;

    [SerializeField]
    private Vector3 m_localOffset = Vector3.zero;

    [SerializeField]
    private float m_baseAngleOffset = 0f;

    [SerializeField]
    private float m_lineAngleOffset = Mathf.PI * 0.5f;

    override public Vector3 ComputePosition(Vector3 center, Quaternion rotation, int index)
    {
        float unitsCount = Math.Max(m_unitsPerCircle, 1);

        int lineIndex = Mathf.FloorToInt(index / unitsCount);

        float positionAngle = Mathf.PI * 2.0f / unitsCount * index;
        float angleOffset = m_baseAngleOffset + m_lineAngleOffset * lineIndex;
        float finalAngle = positionAngle + angleOffset;

        float spacing = m_baseSpacing + lineIndex * m_circleSpacing;

        Vector3 finalOffset = m_worldOffset + rotation * m_localOffset;

        Vector3 circularOffset = (Vector3.right * Mathf.Cos(finalAngle) + Vector3.forward * Mathf.Sin(finalAngle)) * spacing;

        if (m_followRotation)
            circularOffset = rotation * circularOffset;

        return finalOffset + center + circularOffset;
    }
}
