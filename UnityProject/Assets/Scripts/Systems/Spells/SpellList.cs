using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using NaughtyAttributes;

namespace ScriptableObjects.Systems.Spells
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
				Loggy.LogErrorFormat("SpellList: no spell found at index {0}", Category.Spells, index);
				return InvalidData;
			}

			return Spells[index];
		}
	}
}
