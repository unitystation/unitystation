using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/* Editor control over the FloorTile component:
 * This is used to add the ambient tiles so the
 * majority are instantiated in editmode
 * rather then on Start() when the game has started
 */
#if UNITY_EDITOR
[CustomEditor(typeof(FloorTile))]
[CanEditMultipleObjects]
public class FloorTileEditor : Editor
{

    private FloorTile floorTile;

    void OnSceneGUI()
    {
        if (floorTile == null)
        {
            floorTile = target as FloorTile;
        }
        if (floorTile == null)
            return;
        EditorChangedActions();
    }

    void EditorChangedActions()
    {
        floorTile.CheckAmbientTile();
    }
}
#endif
