using System;
using System.Collections;
using System.Collections.Generic;
#if Unity_Editor
using UnityEditor;
#endif
using UnityEngine;

public class PrefabTracker : MonoBehaviour
{
	public string ForeverID
	{
		get
		{
#if Unity_Editor
			if (string.IsNullOrEmpty(foreverID))
			{
				ReassignID();
				try
				{
					PrefabUtility.SavePrefabAsset(this.gameObject);
				}
				catch (Exception e)
				{
				}
			}

#endif
			return foreverID;
		}
		set { foreverID = value; }
	}

	[SerializeField] private string foreverID;


	public void ReassignID() //Assuming it's a prefab Variant
	{
#if Unity_Editor
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
#if Unity_Editor

		foreverID =
			AssetDatabase.AssetPathToGUID(
				AssetDatabase.GetAssetPath(gameObject)); //Can possibly change over time so need some prevention
		if (string.IsNullOrEmpty(foreverID))
		{
			foreverID = CreateString(20);
		}

		EditorUtility.SetDirty(gameObject);
#endif
	}
	
	static System.Random rd = new System.Random();
	internal static string CreateString(int stringLength)
	{
		const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@$?_-";
		char[] chars = new char[stringLength];

		for (int i = 0; i < stringLength; i++)
		{
			chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
		}

		return new string(chars);
	}

}