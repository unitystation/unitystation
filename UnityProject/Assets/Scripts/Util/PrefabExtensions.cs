using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Util.PrefabUtils
{
	public static class PrefabExtensions
	{
		// == anything that has nothing but spaces at the begging of a line and has "m_SourcePrefab" in it
		private static Regex basePrefabRegex = new Regex("^\\s*m_SourcePrefab", RegexOptions.Compiled);
		// == guid with the "guid: " word and comma at the end
		private static Regex basePrefabGuidRegex = new Regex("guid: .*?[,\\}]", RegexOptions.Compiled);

		/// <summary>
		/// 	Gets guid (as a string) of a base prefab.
		/// 	Quite expensive method, don't use it too often.
		/// </summary>
		/// <param name="variantGameObject">Prefab-variant as a game object</param>
		/// <returns>
		/// 	GUID of a base prefab of a prefab variant.
		/// 	Null if couldn't find a .prefab file or if this prefab has no base prefab.
		/// </returns>
		public static string GetVariantBaseGuid(this GameObject variantGameObject)
		{
			string variantPath = AssetDatabase.GetAssetPath(variantGameObject);
			if (string.IsNullOrEmpty(variantPath))
			{
				return null;
			}

			// source prefab guids are usually located in the bottom of a text file or quite close to it
			ReverseLineReader reverseLineReader = new ReverseLineReader(variantPath);
			foreach (string line in reverseLineReader)
			{
				if (basePrefabRegex.IsMatch(line) == false)
				{
					// not this line. Keep searching
					continue;
				}
				// yeah the prefab is actually a variant and has its base. Let's find its guid.
				string rawGuid = basePrefabGuidRegex.Match(line).Value;
				// ignore everything ("guid: " at the beginning and "," at the end) except a guid
				string guid = rawGuid.Substring(6, rawGuid.Length - 7);
				return string.IsNullOrEmpty(guid) ? null : guid;
			}

			return null;
		}

		public static GameObject GetVariantBaseGameObject(this GameObject variantGameObject)
		{
			return AssetDatabase.LoadAssetAtPath<GameObject>(
				AssetDatabase.GUIDToAssetPath(variantGameObject.GetVariantBaseGuid())
			);
		}
	}
}