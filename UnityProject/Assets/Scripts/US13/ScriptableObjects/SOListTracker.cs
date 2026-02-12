using System.Collections.Generic;
using Chemistry;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;
using US13.Items.Others;
using US13.Items.Traits;
using US13.ScriptableObjects.Atmospherics;
using US13.ScriptableObjects.Audio;
using US13.Systems.Ore_Generation;

namespace US13.ScriptableObjects
{
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
			SOTrackers.AddRange(FindAssetsByType<Reagent>());
			SOTrackers.AddRange(FindAssetsByType<RandomItemPool>());
			SOTrackers.AddRange(FindAssetsByType<GasMixesSO>());
			SOTrackers.AddRange(FindAssetsByType<OreGeneratorConfig>());
			SOTrackers.AddRange(FindAssetsByType<AudioClipsArray>());
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
}
