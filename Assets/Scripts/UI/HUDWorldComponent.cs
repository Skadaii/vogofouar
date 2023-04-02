using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class HUDWorldComponent : MonoBehaviour
{
    private Quaternion originalRotation;

    public bool enableBillboard = true;
    public bool enableFixedScreenSize = false;

    [SerializeField] private float screenSize = 1f;

    private const float SCREEN_SIZE_SCALE_FACTOR = 0.001f;

    protected void Awake()
    {
        originalRotation = transform.rotation;
    }

    private void OnEnable()
    {
        Camera.onPreRender += PreRender;
    }

    private void OnDisable()
    {
        Camera.onPreRender -= PreRender;
    }

    private void PreRender(Camera cam)
    {
        if ((cam.cullingMask & (1 << gameObject.layer)) == 0) return;

        if (enableBillboard) ComputeBillboard(cam);
        if (enableFixedScreenSize) ComputeSize(cam);
    }

    private void ComputeBillboard(Camera camera) => transform.rotation = camera.transform.rotation * originalRotation;

    private void ComputeSize(Camera camera)
    {
        float size;
        if (camera.orthographic)
        {
            size = camera.orthographicSize;
        }
        else
        {
            float halfFOV = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float distance = Vector3.Distance(transform.position, camera.transform.position);
            size = distance * Mathf.Tan(halfFOV) * camera.aspect;
        }

        transform.localScale = Vector3.one * size * screenSize * SCREEN_SIZE_SCALE_FACTOR;
    }
}
