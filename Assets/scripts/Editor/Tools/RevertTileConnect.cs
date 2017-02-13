using Matrix;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RevertTileConnect : Editor {

    [MenuItem("Tools/Reconnect TileConnect %r")]
    static void Revert() {
        var triggers = FindObjectsOfType<ConnectTrigger>();

        foreach(var t in triggers) {
            PrefabUtility.RevertPrefabInstance(t.gameObject);
        }
    }
}
