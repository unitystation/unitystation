using UnityEditor;
using UnityEngine;
using Doors;

	/// <summary>
	///     DoorAnimatorEditor helps determine if the door
	///     prefab has been set up correctly by the creator.
	/// </summary>
	[CustomEditor(typeof(AirLockAnimator))]
	public class DoorAnimatorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			//		serializedObject.Update();
			//		SerializedProperty tps = serializedObject.FindProperty ("doorBaseSprites");
			//		EditorGUILayout.PropertyField(tps, true);

			AirLockAnimator dTarget = (AirLockAnimator) target;
			if (dTarget.overlay_Lights == null || dTarget.overlay_Glass == null || dTarget.doorbase == null)
			{
				EditorGUILayout.HelpBox(
					"Please add the three child GameObjects overlay_Lights, " + "overlay_Glass and doorbase and attach SpriteRenderers to them. \r\n " +
					"Add all of the default sprites to each Renderer then return here to press the " + "'Auto Load Sprites' button", MessageType.Error);
				dTarget.FindMembers();
			}
			else
			{
				if (GUILayout.Button("Auto Load Sprites"))
				{
					dTarget.LoadSprites();
				}
//               if (dTarget.doorBaseSprites.Length == null || dTarget.overlaySprites.Length == null
//                   || dTarget.overlayLights.Length == null)
//               {
//                  EditorGUILayout.HelpBox("No sprites loaded into the arrays yet. Please press" +
//                                          "'Auto Load Sprites' button.", MessageType.Info);
//                }
				else
				{
					EditorGUILayout.HelpBox(
						"All Sprites Loaded. You can always refresh them again by pressing " + "the button above. Don't forget to press apply on the prefab!",
						MessageType.Info);
				}
			}
		}
	}
