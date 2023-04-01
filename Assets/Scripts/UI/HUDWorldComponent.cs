using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDWorldComponent : MonoBehaviour
{
    private Transform camTransform;
    private Quaternion originalRotation;

    public bool enableBillboard = true;
    public bool enableFixedScreenSize = false;

    [SerializeField] private float screenSize = 1f;

    protected void Start()
    {
        camTransform = Camera.main.transform;
        originalRotation = transform.rotation;

        if (enableFixedScreenSize) ComputeSize();
        if (enableBillboard && camTransform != null) ComputeBillboard();
    }

    protected void Update()
    {
        if (enableBillboard && camTransform != null) ComputeBillboard();
    }

    private void FixedUpdate()
    {
        if (enableFixedScreenSize) ComputeSize();
    }

    private void ComputeBillboard()
    {
        transform.rotation = camTransform.rotation * originalRotation;
    }

    private void ComputeSize()
    {
        Vector3 a = Camera.main.WorldToScreenPoint(transform.position);
        Vector3 b = new Vector3(a.x, a.y + screenSize, a.z);

        Vector3 aa = Camera.main.ScreenToWorldPoint(a);
        Vector3 bb = Camera.main.ScreenToWorldPoint(b);

        transform.localScale = Vector3.one * (aa - bb).magnitude;
    }
}
