using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Util
{
	/// <summary>
	/// Utils related to finding stuff
	/// </summary>
	public static class FindUtils
	{
		/// <summary>
		/// Special version of FindObjects which supports interfaces
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static List<T> FindInterfaceImplementersInScene<T>()
		{
			List<T> interfaces = new List<T>();
			GameObject[] rootGameObects = SceneManager.GetActiveScene().GetRootGameObjects();

			foreach (var rootGameObject in rootGameObects)
			{
				T[] childrenInterfaces = rootGameObject.GetComponentsInChildren<T>();
				foreach (var childInterface in childrenInterfaces)
				{
					interfaces.Add(childInterface);
				}
			}

			return interfaces;
		}

		public static IEnumerable<T> FindInterfacesOfType<T>(bool includeInactive = false)
		{
			return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(includeInactive));
		}

		/// <summary>
		/// 	Generates and returns a new dictionary of pair of values: parent-prefab and its childs-prefabs as a set.
		/// 	Variants can also have their variants, so they also can be sources and should be presented in this
		/// 	dictionary with the matching key.
		/// </summary>
		/// <param name="necessaryComponentTypes">Components that prefabs should have.</param>
		/// <returns>Dictionary of pairs: Parent-prefab; Set of its childs. </returns>
		public static Dictionary<GameObject, HashSet<GameObject>> BuildAndGetInheritanceDictionaryOfPrefabs(
			List<Type> necessaryComponentTypes
		)
		{
			// thank unity we have no better way to see variants (heirs) of game objects.
			// This dictionary contains pairs of values:
			// <Parent, List<parent's heirs>> or something like that. Heirs can also have its heirs, so they also
			// can be parents and should be presented in this dictionary with the matching key.
			Dictionary<GameObject, HashSet<GameObject>> parentsAndChilds =
				new Dictionary<GameObject, HashSet<GameObject>>();
#if UNITY_EDITOR
			string[] possibleChildGuids =
				AssetDatabase.FindAssets("t:prefab", new[] {"Assets/Prefabs"});

			foreach (string possibleChildGuid in possibleChildGuids)
			{
				GameObject possibleChildPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
					AssetDatabase.GUIDToAssetPath(possibleChildGuid)
				);

				bool doesntHaveNecessaryComponent = false;
				foreach (Type componentType in necessaryComponentTypes)
				{
					if (possibleChildPrefab.GetComponent(componentType) == null)
					{
						doesntHaveNecessaryComponent = true;
						break;
					}
				}

				if (doesntHaveNecessaryComponent)
				{
					continue;
				}

				// assuming that game object as a prefab variant and trying to find its base
				GameObject parent = PrefabUtility.GetCorrespondingObjectFromSource(possibleChildPrefab);
				// is it not a variant?
				if (parent == null)
				{
					// yes so no need to add it somewhere
					continue;
				}

				// have we met this parent first time?
				if (parentsAndChilds.ContainsKey(parent) == false)
				{
					parentsAndChilds[parent] = new HashSet<GameObject>();
					continue;
				}

				parentsAndChilds[parent].Add(possibleChildPrefab);
			}
#endif
			return parentsAndChilds;
		}

		/// <summary>
		/// Returns the reference if it isn't null or finds an object of that type, sets the reference, and returns it.
		/// This uses the slow FindObjectOfType method, consider alternative ways to get the object.
		/// </summary>
		public static T LazyFindObject<T>(ref T obj, bool includeInactive = false) where T : Object
		{
			if (obj == null) obj = Object.FindObjectOfType<T>(includeInactive);
			return obj;
		}
	}
}
