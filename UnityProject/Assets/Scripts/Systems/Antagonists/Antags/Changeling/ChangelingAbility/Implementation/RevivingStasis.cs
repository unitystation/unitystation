using CameraEffects;
using HealthV2;
using Items.Implants.Organs;
using Mirror;
using NaughtyAttributes;
using UI.Core.Action;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Systems/ChangelingAbilities/RevivingStasis")]
	public class RevivingStasis: ChangelingToggleAbility
	{
		public override bool UseAbilityToggleClient(ChangelingMain changeling, bool toggle)
		{
			return false;
		}

		[Server]
		public override bool UseAbilityToggleServer(ChangelingMain changeling, bool toggle)
		{
			if (CustomNetworkManager.IsServer == false) return false;
			if (toggle == false)
			{
				//UIActionManager.SetServerSpriteSO(this, Sprites[0]);
				changeling.UseAbility(this);
				// healing
				changeling.ChangelingMind.Body.playerHealth.FullyHeal();
				changeling.ChangelingMind.Body.playerHealth.UnstopOverallCalculation();
				changeling.ChangelingMind.Body.playerHealth.UnstopHealthSystemsAndRestartHeart();
				changeling.HasFakingDeath(false);
			}
			else
			{
				//UIActionManager.SetServerSpriteSO(this, Sprites[1]);
				changeling.HasFakingDeath(true);

				changeling.ChangelingMind.Body.playerHealth.StopHealthSystemsAndHeart();
				changeling.ChangelingMind.Body.playerHealth.StopOverralCalculation();
				changeling.ChangelingMind.Body.playerHealth.SetConsciousState(ConsciousState.UNCONSCIOUS);
			}
			return true;
		}
	}
}