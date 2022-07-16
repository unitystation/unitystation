using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public class SetupSpriteCatalogue : IPreprocessBuild
{
	public int callbackOrder
	{
		get { return 6; }
	}

	public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
	{
		List<T> assets = new List<T>();
		string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
		for (int i = 0; i < guids.Length; i++)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
			T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
			if (asset != null)
			{
				assets.Add(asset);
			}
		}

		return assets;
	}

	public void OnPreprocessBuild(BuildTarget target, string path)
	{
		AssetDatabase.StartAssetEditing();
		var AAA = FindAssetsByType<SpriteCatalogue>();
		foreach (var AA in AAA)
		{
			AA.Catalogue = new List<SpriteDataSO>();
			EditorUtility.SetDirty(AA);
		}

		var SOs = FindAssetsByType<SpriteDataSO>();
		foreach (var SO in SOs)
		{
			AAA[0].AddToCatalogue(SO);
		}
		EditorUtility.SetDirty(AAA[0]);
		AssetDatabase.StopAssetEditing();
		AssetDatabase.SaveAssets();
	}
}
