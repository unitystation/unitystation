﻿using Matrix;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(EditModeControl))]
public class EditModeControlEditor: Editor {

    private Vector3 currentPosition;

    void OnSceneGUI() {
        var editModeControl = target as EditModeControl;
        if(currentPosition != editModeControl.transform.position) {
            currentPosition = editModeControl.transform.position;
            editModeControl.Snap();

            var registerTile = editModeControl.GetComponent<RegisterTile>();
            if(registerTile) {
                registerTile.UpdatePosition();

                var connectTrigger = editModeControl.GetComponent<ConnectTrigger>();
                if(connectTrigger) {
                    connectTrigger.UpdatePosition();
                }
            }
        }
    }
}
