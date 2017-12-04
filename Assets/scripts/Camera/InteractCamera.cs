using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractCamera : MonoBehaviour
{

    public static InteractCamera Instance;
    public Camera mainCam;
    public Camera interactCam;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    void Start()
    {
        interactCam.orthographicSize = mainCam.orthographicSize;

    }

    void Update()
    {
        if (interactCam.orthographicSize != mainCam.orthographicSize)
        {
            interactCam.orthographicSize = mainCam.orthographicSize;
        }
    }
}
