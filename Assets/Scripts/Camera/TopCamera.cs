using UnityEngine;

public class TopCamera : MonoBehaviour
{
    [SerializeField]
    private int m_moveSpeed = 20;
    [SerializeField]
    private int m_keyboardSpeedModifier = 10;
    [SerializeField]
    private int m_zoomSpeed = 400;
    [SerializeField]
    private int m_minHeight = 50;
    [SerializeField]
    private int m_maxHeight = 200;
    [SerializeField]
    private AnimationCurve m_moveSpeedFromZoomCurve = new AnimationCurve();

    [SerializeField]
    private float m_terrainBorder = 0f;
    [SerializeField, Tooltip("Set to false for debug camera movement")]
    private bool m_enableMoveLimits = true;

    private Vector3 m_move = Vector3.zero;
    private Vector3 m_terrainSize = Vector3.zero;

    #region Camera movement methods
    public void Zoom(float value)
    {
        if (value < 0f)
        {
            m_move -= transform.forward * m_zoomSpeed * Time.deltaTime;
        }
        else if (value > 0f)
        {
            m_move += transform.forward * m_zoomSpeed * Time.deltaTime;
        }
    }
    private float ComputeZoomSpeedModifier()
    {
        float zoomRatio = Mathf.Clamp(1f - (m_maxHeight - transform.position.y) / (m_maxHeight - m_minHeight), 0f, 1f);
        float zoomSpeedModifier = m_moveSpeedFromZoomCurve.Evaluate(zoomRatio);
        //Debug.Log("zoomSpeedModifier " + zoomSpeedModifier);

        return zoomSpeedModifier;
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
        m_move.x += value * m_moveSpeed * ComputeZoomSpeedModifier() * Time.deltaTime;
    }
    public void MoveVertical(float value)
    {
        m_move.z += value * m_moveSpeed * ComputeZoomSpeedModifier() * Time.deltaTime;
    }

    // Direct focus on one entity (no smooth)
    public void FocusEntity(BaseEntity entity)
    {
        if (entity == null)
            return;

        Vector3 newPos = entity.transform.position;
        newPos.y = transform.position.y;

        transform.position = newPos;
    }

    #endregion

    #region MonoBehaviour methods
    private void Start()
    {
        m_terrainSize = GameServices.TerrainSize;
    }
    private void Update()
    {
        if (m_move != Vector3.zero)
        {
            transform.position += m_move;
            if (m_enableMoveLimits)
            {
                // Clamp camera position (max height, terrain bounds)
                Vector3 newPos = transform.position;
                newPos.x = Mathf.Clamp(transform.position.x, m_terrainBorder, m_terrainSize.x - m_terrainBorder);
                newPos.y = Mathf.Clamp(transform.position.y, m_minHeight, m_maxHeight);
                newPos.z = Mathf.Clamp(transform.position.z, m_terrainBorder, m_terrainSize.z - m_terrainBorder);
                transform.position = newPos;
            }
        }

        m_move = Vector3.zero;
    }
    #endregion
}
