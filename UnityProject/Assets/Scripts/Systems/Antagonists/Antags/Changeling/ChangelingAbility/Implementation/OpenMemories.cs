using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Systems/ChangelingAbilities/OpenMemories")]
	public class OpenMemories: ChangelingBaseAbility
	{
		public override bool UseAbilityClient(ChangelingMain changeling)
		{
			UIManager.Display.hudChangeling.OpenMemoriesUI();
			return true;
		}
	}
}