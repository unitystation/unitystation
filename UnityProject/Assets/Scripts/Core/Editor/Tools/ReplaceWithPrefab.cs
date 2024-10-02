using Construction.Conveyors;
using Core.Sprite_Handler;
using Light2D;
using Logs;
using UnityEngine;
using UnityEditor;

public class ReplaceWithPrefab : EditorWindow
{
	[Tooltip("The new new prefab to replace old prefabs with.")]
	[SerializeField] private GameObject prefab;

	// -- this creates the menu to open the "Replace With Prefab" window
	[MenuItem("Mapping/Replace With Prefab")]
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
					Loggy.LogError("Error instantiating prefab", Category.Editor);
					break;
				}

				// -- set up "undo" features for the new prefab, like setting up the old transform
				Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
				newObject.transform.parent = selected.transform.parent;
				newObject.transform.localPosition = selected.transform.localPosition;
				newObject.transform.localRotation = selected.transform.localRotation;
				newObject.transform.localScale = selected.transform.localScale;
				newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());

				var selectedRotatable = selected.GetComponent<Rotatable>();
				var newObjectRotatable = newObject.GetComponent<Rotatable>();
				if (selectedRotatable != null && newObjectRotatable != null)
				{
					newObjectRotatable.FaceDirection(selectedRotatable.CurrentDirection);
				}

				var selectedLightSprite = selected.GetComponent<LightSprite>();
				var newObjectLightSprite = newObject.GetComponentInChildren<LightSprite>();
				if (selectedLightSprite != null && newObjectLightSprite != null)
				{
					newObjectLightSprite.InitialColour = selectedLightSprite.InitialColour;
					newObjectLightSprite.transform.localScale = selected.transform.lossyScale;

					var Handler = newObjectLightSprite.GetComponentInChildren<LightSpriteHandler>();

					var Catalogue = Handler.GetSubCatalogue();

					SpriteDataSO Bright = null;
					foreach (var srightSO in Catalogue)
					{
						if (srightSO.Variance[0].Frames[0].sprite == selectedLightSprite.Sprite)
						{
							Bright = srightSO;
							break;
						}

					}

					if (Bright == null)
					{
						Loggy.LogError("AAAA > " + selectedLightSprite.Sprite + "selected > " + selected.name);
					}

					Handler.SetSpriteSO(Bright);
				}


				var Conveyorselected = selected.GetComponent<ConveyorBelt>();
				var newConveyorselected = newObject.GetComponent<ConveyorBelt>();
				if (Conveyorselected != null && newConveyorselected != null)
				{
					newConveyorselected.CurrentDirection = Conveyorselected.CurrentDirection;
					newConveyorselected.CurrentStatus = Conveyorselected.CurrentStatus;
				}


				var MobSpawnScripselected = selected.GetComponent<LegacyMobSpawnScript>();
				var MobSpawnScripnewObject = newObject.GetComponent<LegacyMobSpawnScript>();
				if (MobSpawnScripselected != null && MobSpawnScripnewObject != null)
				{
					MobSpawnScripnewObject.MobToSpawn = MobSpawnScripselected.MobToSpawn;
				}


				newObject.name = selected.name;
				Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
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
