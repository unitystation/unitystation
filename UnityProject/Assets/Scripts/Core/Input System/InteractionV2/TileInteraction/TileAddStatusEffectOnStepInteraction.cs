using Systems.Interaction;
using Systems.StatusesAndEffects;
using UnityEngine;

namespace Core.Input_System.InteractionV2.TileInteraction
{
	[CreateAssetMenu(fileName = "TileStatusEffectOnStep", menuName = "Interaction/TileInteraction/TileStatusEffectOnStep")]
	public class TileAddStatusEffectOnStepInteraction : TileStepInteraction
	{
		public StatusEffect StatusEffectOnStep;

		public override bool WillAffectPlayer(PlayerScript playerScript)
		{
			return true;
		}

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			base.OnPlayerStep(playerScript);
			playerScript.StatusEffectManager.AddStatus(StatusEffectOnStep);
		}
	}
}