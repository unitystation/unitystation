using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FUCKHideFlags_DontSave : IPreprocessBuild
{
	public int callbackOrder
	{
		get { return 99; }
	}

	public void OnPreprocessBuild(BuildTarget target, string path)
	{

		string[] allAssetGUIDs = AssetDatabase.FindAssets("");

		foreach (string assetGUID in allAssetGUIDs)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
			Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

			if (asset != null && asset.hideFlags != HideFlags.None)
			{
				// Modify the hide flags to HideFlags.None
				asset.hideFlags = HideFlags.None;

				// Mark the asset as dirty to apply the changes
				EditorUtility.SetDirty(asset);
			}
		}

		// Save the changes to the asset database
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

}
