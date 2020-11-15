using System.Collections;
using UnityEngine;
using NaughtyAttributes;

namespace ScriptableObjects
{
	/// <summary>
	/// A generic ScriptableObject with which a list of GameObject prefabs can be defined via the inspector.
	/// </summary>
	[CreateAssetMenu(fileName = "MyGameObjectList", menuName = "ScriptableObjects/GameObjectList")]
	public class GameObjectList : ScriptableObject
	{
		[Tooltip("Define a list of your GameObject prefabs here.")]
		[SerializeField, ReorderableList]
		private GameObject[] gameObjects = default;

		/// <summary>
		/// Gets the array of GameObject prefabs, as defined via the inspector.
		/// </summary>
		public GameObject[] GameObjectPrefabs => gameObjects;

		/// <summary>
		/// Gets a random GameObject prefab from the array of GameObject prefabs as defined via the inspector.
		/// </summary>
		/// <returns>a random GameObject prefab</returns>
		public GameObject GetRandom()
		{
			return GameObjectPrefabs.PickRandom();
		}
	}
}
