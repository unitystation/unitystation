using System.Collections.Generic;
using Logs;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using US13.ScriptableObjects;

namespace US13.Systems.Spells
{
	/// <summary>
	/// Singleton. List of all spells mapped with corresponding spell data
	/// </summary>
	[CreateAssetMenu(fileName = "SpellListSingleton", menuName = "Singleton/SpellList")]
	public class SpellList : SingletonScriptableObject<SpellList>
	{
		public SpellData InvalidData;

		public GameObject DefaultImplementation;

		[ReorderableList]
		public List<SpellData> Spells = new List<SpellData>();

		public SpellData FromIndex(short index)
		{
			if (index < 0 || index > Spells.Count - 1)
			{
				Loggy.Error().Format("SpellList: no spell found at index {0}", Category.Spells, index);
				return InvalidData;
			}

			return Spells[index];
		}

#if UNITY_EDITOR

		[NaughtyAttributes.Button]
		public void FindAll()
		{
			Spells = FindAssetsByType<SpellData>();
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
