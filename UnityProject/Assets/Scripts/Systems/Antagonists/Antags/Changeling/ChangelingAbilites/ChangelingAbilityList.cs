using ScriptableObjects.Systems.Spells;
using ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Changeling
{
	[CreateAssetMenu(fileName = "ChangelingAbilitesList", menuName = "Singleton/ChangelingAbilitesList")]
	public class ChangelingAbilityList : SingletonScriptableObject<ChangelingAbilityList>
	{
		[ReorderableList]
		public List<ChangelingData> Abilites = new List<ChangelingData>();

		public ChangelingData InvalidData;

		public ChangelingData FromIndex(short index)
		{
			if (index < 0 || index > Abilites.Count - 1)
			{
				Logger.LogErrorFormat("ChangelingAbilityList: no ability found at index {0}", Category.Changeling, index);
				return InvalidData;
			}

			return Abilites[index];
		}
	}

}