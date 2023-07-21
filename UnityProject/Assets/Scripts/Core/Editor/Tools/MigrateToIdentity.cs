using System.Globalization;
using Core.Identity;
using UnityEditor;
using UnityEngine;

namespace Core.Editor.Tools
{
	/// <summary>
	/// This tool is a single use tool that will migrate names and descriptions from the Attributes component to the new Identity component.
	/// </summary>
	public class MigrateToIdentity: ScriptableWizard
	{

		[MenuItem("Tools/Migrations/Migrate to Identity")]
		private static void CreateWizard()
		{
			DisplayWizard<MigrateToIdentity>("Migrate to Identity", "Migrate");
		}

		/// <summary>
		/// Reads all game objects in the Assets folder and returns them as an array.
		/// </summary>
		/// <returns></returns>
		private GameObject[] AllGameObjects()
		{
			string[] guids = AssetDatabase.FindAssets("t:GameObject");
			GameObject[] gameObjects = new GameObject[guids.Length];
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				gameObjects[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			}

			return gameObjects;
		}

		private void MigrateNameAndDescription(GameObject[] gameObjects)
		{
			foreach (var go in gameObjects)
			{
				var entityIdentity = go.GetComponent<EntityIdentity>();
				var attributes = go.GetComponent<global::Attributes>();

				if (entityIdentity == null || attributes == null) continue;
				entityIdentity.SetDisplayName(string.Empty, attributes.ArticleName);
				entityIdentity.SetDescription(string.Empty ,BuildDescription(attributes.InitialDescription));
				EditorUtility.SetDirty(go);
				AssetDatabase.SaveAssets();
			}
		}

		private string BuildDescription(string description)
		{
			if (string.IsNullOrEmpty(description))
			{

				var article = "a";
				if (description!.StartsWith("a", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("e", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("i", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("o", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("u", true, CultureInfo.InvariantCulture))
				{
					article = "an";
				}
				return "This is "+ article + " {0}.";
			}

			return description;
		}

		private void OnWizardCreate()
		{
			MigrateNameAndDescription(AllGameObjects());
		}
	}
}