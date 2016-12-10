using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelPerfect: MonoBehaviour {

    public int spriteSize = 32;
    public float zoom = 2;

    private int screenPixelsY = 0;

    private float currentZoom;

    void Start() {
        currentZoom = zoom;
    }

    void Update() {
        if(screenPixelsY != Screen.height || currentZoom != zoom) {
            screenPixelsY = Screen.height;
            currentZoom = zoom;

            var s_baseOrthographicSize = ((float) Screen.height) / spriteSize / (2 * zoom);
            Camera.main.orthographicSize = s_baseOrthographicSize;
        }
    }
}
