using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WallBuildTrigger: MonoBehaviour {

    private Vector3 currentPosition;
    private WallsConnect[] corners;

    private int x = -1, y = -1;

    void Start() {

        corners = GetComponentsInChildren<WallsConnect>();

        currentPosition = transform.position;
        UpdatePosition((int) currentPosition.x, (int) currentPosition.y);
    }

    void LateUpdate() {
        if(currentPosition != transform.position) {
            currentPosition = transform.position;
            UpdatePosition((int) currentPosition.x, (int) currentPosition.y);
        }
    }

    void OnDestroy() {
        if(x >= 0)
            WallMap.Remove(x, y);
    }

    private void UpdatePosition(int x_new, int y_new) {
        if(x >= 0)
            WallMap.Remove(x, y);

        WallMap.Add(x_new, y_new, TileType.Wall);

        x = x_new;
        y = y_new;

        foreach(var c in corners) {
            c.UpdatePosition(x, y);
        }
    }
}