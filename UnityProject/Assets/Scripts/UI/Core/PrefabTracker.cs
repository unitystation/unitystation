using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Util
{
	public class PrefabTracker : MonoBehaviour
	{
		public string ForeverID {
			get {
#if UNITY_EDITOR
				if (string.IsNullOrEmpty(foreverID))
				{
					ReassignID();
					try
					{
						PrefabUtility.SavePrefabAsset(this.gameObject);
					}
					catch (Exception) { }
				}
#endif
				return foreverID;
			}
			set => foreverID = value;
		}

		[SerializeField] private string foreverID;
		[field:SerializeField] public string AlternativePrefabName { get; private set; }
		[field: SerializeField] public bool CanBeSpawnedByAdmin { get; private set; } = true;

		public void ReassignID() //Assuming it's a prefab Variant
		{
#if UNITY_EDITOR
			foreverID =
				AssetDatabase.AssetPathToGUID(
					AssetDatabase.GetAssetPath(gameObject)); //Can possibly change over time so need some prevention
			if (string.IsNullOrEmpty(foreverID))
			{
				foreverID = CreateString(20);
			}
#endif
		}

		[NaughtyAttributes.Button("Assign random ID")]
		public void ForceSetID() //Assuming it's a prefab Variant
		{
#if UNITY_EDITOR
			// Can possibly change over time so need some prevention
			foreverID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(gameObject));
			if (string.IsNullOrEmpty(foreverID))
			{
				foreverID = CreateString(20);
			}

			EditorUtility.SetDirty(gameObject);
#endif
		}

		internal static string CreateString(int stringLength)
		{
			const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@$?_-";
			char[] chars = new char[stringLength];

			for (int i = 0; i < stringLength; i++)
			{
				chars[i] = allowedChars.PickRandom();
			}

			return new string(chars);
		}
	}
}
