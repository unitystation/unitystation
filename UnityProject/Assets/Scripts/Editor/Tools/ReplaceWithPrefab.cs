using UnityEngine;
using UnityEditor;

public class ReplaceWithPrefab : EditorWindow
{
	[Tooltip("The new new prefab to replace old prefabs with.")]
	[SerializeField] private GameObject prefab;

	// -- this creates the menu to open the "Replace With Prefab" window
	[MenuItem("Tools/Replace With Prefab")]
	static void CreateReplaceWithPrefab()
	{
		EditorWindow.GetWindow<ReplaceWithPrefab>();
	}

	private void OnGUI()
	{
		// -- get a handle to the prefab you want to replace everything with
		prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

		// -- if you've pressed the "Replace" button...
		if (GUILayout.Button("Replace"))
		{
			// -- get the list of objects you have selected
			var selection = Selection.gameObjects;

			// -- get the prefab type (I moved this out of the loop because it makes
			// -- no sense to check it every time)
			var prefabType = PrefabUtility.GetPrefabAssetType(prefab);

			// -- loop over all of the selected objects
			for (var i = selection.Length - 1; i >= 0; --i)
			{
				// -- get the next selected object
				var selected = selection[i];
				GameObject newObject;

				// -- if your "prefab" really is a prefab . . .
				if (prefabType != PrefabAssetType.NotAPrefab)
				{
					// -- . . . then this part should always run.
					// -- If you update the original prefab, the replaced items will
					// -- update as well.
					newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
				}
				else
				{
					// -- if this code is running, you didn't drag a prefab to your window.
					// -- it will just Instantiate whatever you did drag over.
					newObject = Instantiate(prefab);
					newObject.name = prefab.name;
				}

				// -- if for some reason Unity couldn't perform your request, print an error
				if (newObject == null)
				{
					Debug.LogError("Error instantiating prefab");
					break;
				}

				// -- set up "undo" features for the new prefab, like setting up the old transform
				Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
				newObject.transform.parent = selected.transform.parent;
				newObject.transform.localPosition = selected.transform.localPosition;
				newObject.transform.localRotation = selected.transform.localRotation;
				newObject.transform.localScale = selected.transform.localScale;
				newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
				// -- now delete the old prefab
				Undo.DestroyObjectImmediate(selected);
			}
		}

		// -- prevent the user from editing the window
		GUI.enabled = false;

		// -- update how many items you have selected (Note, it only updates when the mouse cursor is above the Replace window)
		EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
	}
}
