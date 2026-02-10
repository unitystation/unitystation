using UnityEngine;
using US13.Player;
using US13.UI.Systems;

namespace US13.Systems.Antagonists.Antags.Changeling.ChangelingAbility.Implementation
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Systems/ChangelingAbilities/OpenTransform")]
	public class OpenTransform: ChangelingBaseAbility
	{
		public TransformAbility transformAbility;

		public override bool UseAbilityClient(ChangelingMain changeling)
		{
			UIManager.Display.hudChangeling.OpenTransformUI(changeling, (ChangelingDna dna) =>
			{
				PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilitesWithParam(transformAbility.Index, $"{dna.DnaID}");
			});
			return true;
		}
	}
}