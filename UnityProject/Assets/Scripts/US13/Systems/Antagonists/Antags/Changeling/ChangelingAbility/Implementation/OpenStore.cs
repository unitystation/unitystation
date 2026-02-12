using UnityEngine;
using US13.UI.Systems;

namespace US13.Systems.Antagonists.Antags.Changeling.ChangelingAbility.Implementation
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Systems/ChangelingAbilities/OpenStore")]
	public class OpenStore: ChangelingBaseAbility
	{
		public override bool UseAbilityClient(ChangelingMain changeling)
		{
			UIManager.Display.hudChangeling.OpenStoreUI();
			return true;
		}
	}
}