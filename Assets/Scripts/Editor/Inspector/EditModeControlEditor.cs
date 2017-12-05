using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(EditModeControl))]
public class EditModeControlEditor : Editor
{

    private Vector3 currentPosition;

    void OnSceneGUI()
    {
        var editModeControl = target as EditModeControl;
        if (currentPosition != editModeControl.transform.position)
        {
            currentPosition = editModeControl.transform.position;
            editModeControl.Snap();
            // TODO remove
            //            var registerTile = editModeControl.GetComponent<RegisterTile>();
            //            if(registerTile) {
            //                registerTile.UpdateTile();
            //
            //                var connectTrigger = editModeControl.GetComponent<ConnectTrigger>();
            //                if(connectTrigger) {
            //                    connectTrigger.UpdatePosition();
            //                }
            //            }
        }
    }
}
