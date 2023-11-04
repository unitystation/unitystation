using System;
using System.Collections.Generic;
using System.IO;
using Chemistry.Components;
#if UNITY_EDITOR
using UnityEditor;

#endif
using UnityEngine;

namespace Items
{
	[CreateAssetMenu(fileName = "RandomItemPool", menuName = "ScriptableObjects/RandomItemPool")]
	public class RandomItemPool : ScriptableObject
	{
		[Tooltip("List of game objects to choose from")][SerializeField]
		private List<GameObjectPool> pool = null;

		public List<GameObjectPool> Pool => pool;

#if UNITY_EDITOR
		public ItemTrait RequiredTrait;

		public static List<T> LoadAllPrefabsOfType<T>(string path) where T : MonoBehaviour
		{

			if (path != "")
			{
				if (path.EndsWith("/"))
				{
					path = path.TrimEnd('/');
				}
			}

			DirectoryInfo dirInfo = new DirectoryInfo(path);
			FileInfo[] fileInf = dirInfo.GetFiles("*.prefab", SearchOption.AllDirectories);



			//loop through directory loading the game object and checking if it has the component you want
			List<T> prefabComponents = new List<T>();
			foreach (FileInfo fileInfo in fileInf)
			{
				string fullPath = fileInfo.FullName.Replace(@"\", "/");
				string assetPath = "Assets" + fullPath.Replace(Application.dataPath, "");
				GameObject prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;

				if (prefab != null)
				{
					T hasT = prefab.GetComponent<T>();
					if (hasT != null)
					{
						prefabComponents.Add(hasT);
					}

					var hasTT = prefab.GetComponentsInChildren<T>();

					foreach (var S in hasTT)
					{
						prefabComponents.Add(S);
					}
				}
			}

			return prefabComponents;
		}


		[NaughtyAttributes.Button()]
		public void Dothing()
		{
			pool.Clear();
			//var all = LoadAllPrefabsOfType<mymarkerscript>("Assets/Prefabs");

			// foreach (var one in all)
			// {
				// pool.Add(new GameObjectPool()
					// {
						// prefab = one.gameObject,
						// maxAmount = 1,
						// probability = 100
				// });

			// }
		}
#endif
	}

	[Serializable]
	public class GameObjectPool
	{
		[Tooltip("Object we will spawn in the world")] [SerializeField]
		public GameObject prefab = null;
		[Tooltip("Max amount we can spawn of this object")] [SerializeField]
		public int maxAmount = 1;
		[Tooltip("Probability of spawning this item when chosen")] [SerializeField] [Range(0, 100)]
		public int probability = 100;

		public GameObject Prefab => prefab;
		public int MaxAmount => maxAmount;
		public int Probability => probability;
	}


}
