using System.Collections.Generic;
using Logs;
using NaughtyAttributes;
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
	}
}
