using UnityEngine;
using US13.UI.Systems;

namespace US13.Systems.Antagonists.Antags.Changeling.ChangelingAbility.Implementation
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