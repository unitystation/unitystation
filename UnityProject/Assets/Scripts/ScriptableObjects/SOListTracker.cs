using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using ScriptableObjects.Atmospherics;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine;
#endif

[CreateAssetMenu(fileName = "SOListTracker", menuName = "Singleton/ScriptableObjectsTracker")]
public class SOListTracker : SingletonScriptableObject<SOListTracker>
{
	public List<SOTracker> SOTrackers = new List<SOTracker>();

#if UNITY_EDITOR

	[NaughtyAttributes.Button]
	public void FindAllSOTrackers()
	{
		SOTrackers = new List<SOTracker>();
		SOTrackers.AddRange(FindAssetsByType<SpriteDataSO>());
		SOTrackers.AddRange(FindAssetsByType<ItemTrait>());
		SOTrackers.AddRange(FindAssetsByType<GasSO>());
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

#endif


}
