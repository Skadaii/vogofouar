using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable, CreateAssetMenu(fileName = "LineFormation", menuName = "FormationRules/Line", order = 2)]
public class LineFormation : FormationRule
{
    [SerializeField, JsonProperty] private int m_unitsPerLine = 3;

    [SerializeField, JsonProperty] private float m_lineSpacing = 5f;

    [SerializeField, JsonProperty] private float m_columnSpacing = 5f;

    [SerializeField, JsonProperty] private Vector3 m_localOffset = Vector3.zero;

    [SerializeField, JsonProperty] private bool m_followRotation = false;

    override public Vector3 ComputePosition(Vector3 center, Quaternion rotation, int index)
    {
        int verticalIndex = Mathf.FloorToInt(index / m_unitsPerLine);

        int hDirSign = index % 2 * 2 - 1;

        int horizontalIndex = Mathf.FloorToInt((index % m_unitsPerLine + 1) * 0.5f);

        //float evenCountOffset = ((unitsPerLine + 1) % 2 + index % 2) * 0.5f;

        Vector3 horizontalDirection = (hDirSign * (horizontalIndex)) * m_columnSpacing * Vector3.right;
        Vector3 verticalDirection = verticalIndex * m_lineSpacing * Vector3.back;

        Vector3 direction = horizontalDirection + verticalDirection + m_localOffset;

        if (m_followRotation)
            direction = rotation * direction;

        return center + direction;
    }
}
