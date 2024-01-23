using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Core.Sprite_Handler
{
	public class AutoSpriteSOCreationTool : UnityEditor.Editor
	{
		[MenuItem("Tools/Sprites/Create SpriteSO from selected textures", false)]
		public static void CheckForSelectedObjects()
		{
			if (Selection.objects != null && Selection.objects.Length > 0)
			{
				CreateSciprtableObjects();
			}
			else
			{
				Debug.LogError("Nothing is selected.");
			}
		}

		private static void CreateSciprtableObjects()
		{
			Object[] selectedObjects = Selection.objects;
			Debug.Log(selectedObjects.Length);
			if (selectedObjects.Length == 0)
			{
				Debug.LogError("Nothing is selected, duh.");
				return;
			}
			SpriteDataSO textureProperties = ScriptableObject.CreateInstance<SpriteDataSO>();
			var variance = textureProperties.Variance = new List<SpriteDataSO.Variant>();
			variance.Add(new SpriteDataSO.Variant()
			{
				Frames = new List<SpriteDataSO.Frame>()
			});

			Debug.Log(Selection.objects.Length);

			for (int i = 0; i < Selection.objects.Length - 1; i++)
			{
				if (selectedObjects[i] is Sprite e)
				{
					variance[0].Frames.Add(new SpriteDataSO.Frame()
					{
						sprite = e,
						secondDelay = 0.09f,
					});
				}
			}

			if (variance[0].Frames.Count == 0)
			{
				Debug.LogError("No sprites detected. Select the sprites themselves, not the texture2D files.");
				return;
			}

			textureProperties.ForceSetID();

			string path = AssetDatabase.GetAssetPath(selectedObjects[0]) + selectedObjects[0].name + ".asset";
			Debug.Log($"saving at {path}");
			AssetDatabase.CreateAsset(textureProperties, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}