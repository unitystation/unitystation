using System;
using System.Collections;
using System.Collections.Generic;
using SecureStuff;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Random = UnityEngine.Random;

public class SOTracker : ScriptableObject, IHaveForeverID, ISearchSpritePreview
{
		public virtual SpriteDataSO Sprite => null;

		public string Name => name;

		public string ForeverID {
			get {
#if UNITY_EDITOR
				if (string.IsNullOrEmpty(foreverID))
				{
					ReassignID();
					EditorUtility.SetDirty(this);
				}
#endif
				return foreverID;
			}
			set => foreverID = value;
		}

		[SerializeField] private string foreverID;

		public void ReassignID() //Assuming it's a prefab Variant
		{
#if UNITY_EDITOR
			foreverID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this)); //Can possibly change over time so need some prevention
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
			foreverID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
			if (string.IsNullOrEmpty(foreverID))
			{
				foreverID = CreateString(20);
			}

			EditorUtility.SetDirty(this);
#endif
		}

		internal static string CreateString(int stringLength)
		{
			var RNG = new System.Random();
			const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@$?_-";
			char[] chars = new char[stringLength];

			for (int i = 0; i < stringLength; i++)
			{
				chars[i] = allowedChars[RNG.Next(0, 65)];
			}

			return new string(chars);
		}
}
