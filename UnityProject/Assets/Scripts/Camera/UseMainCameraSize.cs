using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseMainCameraSize : MonoBehaviour
{

    private Camera MainCamera;
    private Camera Camera;

    // Use this for initialization
    void Start()
    {
        Camera = GetComponent<Camera>();
        MainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (MainCamera != null && Camera != null)
        {
            Camera.orthographicSize = MainCamera.orthographicSize;
        }
    }
}
