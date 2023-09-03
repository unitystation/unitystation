using ScriptableObjects.Systems.Spells;
using ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using NaughtyAttributes;

namespace Changeling
{
	[CreateAssetMenu(fileName = "ChangelingAbilitesList", menuName = "Singleton/ChangelingAbilitesList")]
	public class ChangelingAbilityList : SingletonScriptableObject<ChangelingAbilityList>
	{
		[ReorderableList]
		public List<ChangelingBaseAbility> Abilites = new List<ChangelingBaseAbility>();

		public ChangelingBaseAbility InvalidData;


		public ChangelingBaseAbility FromIndex(short index)
		{
			if (index < 0 || index > Abilites.Count - 1)
			{
				Loggy.LogErrorFormat("ChangelingAbilityList: no ability found at index {0}", Category.Changeling, index);
				return InvalidData;
			}

			return Abilites[index];
		}
	}

}