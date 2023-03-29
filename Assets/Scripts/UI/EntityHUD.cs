using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityHUD : MonoBehaviour
{
    public Transform camTransform;

    Quaternion originalRotation;

    void Start()
    {
        //camTransform = Camera.current.transform;
        //originalRotation = transform.rotation;
    }

    void Update()
    {
        //transform.rotation = camTransform.rotation * originalRotation;
    }
}
