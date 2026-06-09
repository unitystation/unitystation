using System.Collections.Generic;
using Logs;
using NaughtyAttributes;
using UnityEngine;
using US13.ScriptableObjects;
using US13.Systems.Antagonists.Antags.Changeling.ChangelingAbility;

namespace US13.UI.Systems.MainHUD
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
				Loggy.Error().Format("ChangelingAbilityList: no ability found at index {0}", Category.Changeling, index);
				return InvalidData;
			}

			return Abilites[index];
		}
	}

}