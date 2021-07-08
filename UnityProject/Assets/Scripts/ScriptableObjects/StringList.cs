using System.Collections;
using UnityEngine;
using NaughtyAttributes;

namespace ScriptableObjects
{
	/// <summary>
	/// A generic ScriptableObject with which a list of strings can be defined via the inspector.
	/// </summary>
	[CreateAssetMenu(fileName = "MyStringList", menuName = "ScriptableObjects/StringList")]
	public class StringList : ScriptableObject
	{
		[Tooltip("Define a list of your strings here.")]
		[SerializeField, ReorderableList]
		private string[] strings = default;

		/// <summary>
		/// Gets the array of strings, as defined via the inspector.
		/// </summary>
		public string[] Strings => strings;

		/// <summary>
		/// Gets a random string from the array of strings as defined via the inspector.
		/// </summary>
		/// <returns>a random string</returns>
		public string GetRandom()
		{
			return Strings.PickRandom();
		}
	}
}
