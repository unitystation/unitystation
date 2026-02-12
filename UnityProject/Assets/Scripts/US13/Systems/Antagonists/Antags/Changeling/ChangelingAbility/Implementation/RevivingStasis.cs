using Mirror;
using UnityEngine;
using US13.HealthV2;
using US13.Managers.NetworkManagement;

namespace US13.Systems.Antagonists.Antags.Changeling.ChangelingAbility.Implementation
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
				changeling.UseAbility(this);
				// healing
				changeling.ChangelingMind.Body.playerHealth.FullyHeal();
				changeling.ChangelingMind.Body.playerHealth.UnstopOverallCalculation();
				changeling.ChangelingMind.Body.playerHealth.UnstopHealthSystemsAndRestartHeart();
				changeling.HasFakingDeath(false);
			}
			else
			{
				changeling.HasFakingDeath(true);

				changeling.ChangelingMind.Body.playerHealth.StopHealthSystemsAndHeart();
				changeling.ChangelingMind.Body.playerHealth.StopOverralCalculation();
				changeling.ChangelingMind.Body.playerHealth.SetConsciousState(ConsciousState.UNCONSCIOUS);
			}
			return true;
		}
	}
}