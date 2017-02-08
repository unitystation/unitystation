using UnityEngine;
using UnityEditor;
using System.Collections;

public class RevertPrefabInstance : Editor {

	[MenuItem ("Tools/Reconnect Prefab %r")]
	static void Revert() {
		var selection = Selection.gameObjects;

		if (selection.Length > 0) {
			for (var i = 0; i < selection.Length; i++) {
//				PrefabUtility.ReconnectToLastPrefab(selection[i]);
				PrefabUtility.RevertPrefabInstance(selection[i]);
			}
		} else {
			Debug.Log("Cannot revert to prefab - nothing selected");
		}
	}
}