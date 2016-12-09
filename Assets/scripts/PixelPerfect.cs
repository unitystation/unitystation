using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrthoSize: MonoBehaviour {

    public int spriteSize = 32;
    public int zoom = 2;

    private int screenPixelsY = 0;

    void Start() {

    }

    void Update() {
        if(screenPixelsY != Screen.height) {
            screenPixelsY = Screen.height;
            var s_baseOrthographicSize = ((float) Screen.height) / spriteSize / (2 * zoom);
            Camera.main.orthographicSize = s_baseOrthographicSize;
        }
    }
}
