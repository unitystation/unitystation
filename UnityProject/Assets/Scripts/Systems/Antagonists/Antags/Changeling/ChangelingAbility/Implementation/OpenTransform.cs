using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
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