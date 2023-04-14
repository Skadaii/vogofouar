using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CameraViewQuad : MonoBehaviour
{
    //  Variables
    //  ---------

    [SerializeField] private Camera m_camera;
    [SerializeField] private float m_width = 2f;
    private LineRenderer m_viewQuadRenderer = null;

    //  Functions
    //  ---------

    #region MonoBehaviour methods

    private void Awake()
    {
        m_viewQuadRenderer = GetComponent<LineRenderer>();
        m_viewQuadRenderer.positionCount = 4;
        m_viewQuadRenderer.startWidth = m_viewQuadRenderer.endWidth = m_width;
        m_viewQuadRenderer.loop = true;
    }

    private void FixedUpdate()
    {
        DrawViewQuad();
    }

    #endregion


    #region ViewQuad methods

    private Vector3 GetRayPositionAtFloor(Ray ray)
    {
        return ray.origin + (-ray.origin.y / ray.direction.y) * ray.direction;
    }


    private void DrawViewQuad()
    {
        Ray rayA = m_camera.ScreenPointToRay(new Vector3(0f, 0f, 0f));
        Ray rayB = m_camera.ScreenPointToRay(new Vector3(Screen.width, 0f, 0f));
        Ray rayC = m_camera.ScreenPointToRay(new Vector3(Screen.width, Screen.height, 0f));
        Ray rayD = m_camera.ScreenPointToRay(new Vector3(0f, Screen.height, 0f));

        m_viewQuadRenderer.SetPosition(0, GetRayPositionAtFloor(rayA));
        m_viewQuadRenderer.SetPosition(1, GetRayPositionAtFloor(rayB));
        m_viewQuadRenderer.SetPosition(2, GetRayPositionAtFloor(rayC));
        m_viewQuadRenderer.SetPosition(3, GetRayPositionAtFloor(rayD));
    }

    #endregion
}
