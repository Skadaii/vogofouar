using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    //  Variables
    //  ---------

    [SerializeField] private Vector3 m_targetPosition;
    private Vector3 m_cameraHorizontalOffset;

    [SerializeField] private float m_speed;

    [Header("Zoom settings")]
    [SerializeField] private float m_maxHeight = 350f;
    [SerializeField] private float m_minHeight = 25f;
    [SerializeField] private float m_zoomSpeed = 500f;

    [SerializeField] private float m_zoom = 100f;


    [Header("Terrain related settings")]
    [SerializeField] private float m_terrainBorderWidth = 5f;
    [SerializeField] private bool m_enableMapRestricion = true;

    [SerializeField]
    private AnimationCurve m_moveSpeedFromZoomCurve = new AnimationCurve();


    private Vector3 m_terrainSize = Vector2.zero;
    private float m_zoomSpeedModifier = 1f;
    private float m_keyboardSpeedModifier = 10f;
    private float m_zoomRatio = 0f;

    private const float ZOOM_SPEED_FACTOR = 100f;

    #region MonoBehaviour methods

    private void OnValidate()
    {
        Vector3 offset = transform.position - m_targetPosition;
        m_cameraHorizontalOffset = new Vector3(offset.x, 0f, offset.z);
        transform.forward = Vector3.Normalize(m_targetPosition - transform.position);
    }

    private void Start()
    {
        m_terrainSize = GameServices.TerrainSize;
    }

    private void FixedUpdate()
    {
        ComputeCameraPosition();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(m_targetPosition, 5f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(m_targetPosition, transform.position);
    }

    #endregion

    private void ComputeCameraPosition()
    {
        if (m_enableMapRestricion)
        {
            m_targetPosition.x = Mathf.Clamp(m_targetPosition.x, m_terrainBorderWidth, m_terrainSize.x - m_terrainBorderWidth);
            m_targetPosition.y = 0f;
            m_targetPosition.z = Mathf.Clamp(m_targetPosition.z, m_terrainBorderWidth, m_terrainSize.z - m_terrainBorderWidth);
        }

        Vector3 offset = new Vector3(m_cameraHorizontalOffset.x, m_zoom, m_cameraHorizontalOffset.z);
        Vector3 targetPosition = m_targetPosition + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 10f * Time.fixedDeltaTime);
        transform.forward  = Vector3.Lerp(transform.forward , - offset.normalized, 10f * Time.fixedDeltaTime);
    }


    #region Camera movement methods
    public void Zoom(float value)
    {
        if (value != 0f)
        {
            m_zoom = Mathf.Clamp(m_zoom - value * ZOOM_SPEED_FACTOR * m_zoomSpeed * m_zoomSpeedModifier * Time.deltaTime, m_minHeight, m_maxHeight);
            m_zoomRatio = Mathf.Clamp(1f - (m_maxHeight - m_zoom) / (m_maxHeight - m_minHeight), 0f, 1f);
            m_zoomSpeedModifier = m_moveSpeedFromZoomCurve.Evaluate(m_zoomRatio);
        }
    }

    public void MouseMove(Vector2 move)
    {
        if (Mathf.Approximately(move.sqrMagnitude, 0f))
            return;

        MoveHorizontal(move.x);
        MoveVertical(move.y);
    }
    public void KeyboardMoveHorizontal(float value)
    {
        MoveHorizontal(value * m_keyboardSpeedModifier);
    }
    public void KeyboardMoveVertical(float value)
    {
        MoveVertical(value * m_keyboardSpeedModifier);
    }
    public void MoveHorizontal(float value)
    {
        m_targetPosition.x += value * m_speed * m_zoomSpeedModifier * Time.deltaTime;
    }
    public void MoveVertical(float value)
    {
        m_targetPosition.z += value * m_speed * m_zoomSpeedModifier * Time.deltaTime;
    }

    // Direct focus on one entity (no smooth)
    public void FocusEntity(Entity entity)
    {
        if (entity == null)
            return;

        Vector3 newPos = entity.transform.position;
        transform.position = newPos;
    }
    #endregion
}
