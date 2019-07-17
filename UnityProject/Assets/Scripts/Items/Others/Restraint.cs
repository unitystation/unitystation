using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Restraint : Interactable<HandApply>
{
	/// <summary>
	/// How long it takes to apply the restraints
	/// </summary>
	public float applyTime;

	/// <summary>
	/// How long it takes for another person to remove the restraints
	/// </summary>
	public float removeTime;

	// TODO: Add time it takes to resist out of handcuffs

	/// <summary>
	/// Sound to be played when applying restraints
	/// </summary>
	public string sound = "Handcuffs";

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		PlayerMove targetPM = interaction.TargetObject?.GetComponent<PlayerMove>();

		// Interacts iff the target isn't cuffed
		return !(targetPM?.IsCuffed ?? false);
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		GameObject target = interaction.TargetObject;
		GameObject performer = interaction.Performer;

		SoundManager.PlayNetworkedAtPos(sound, target.transform.position);

		var progressFinishAction = new FinishProgressAction(
			reason =>
			{
				if (reason == FinishProgressAction.FinishReason.COMPLETED)
				{
					if(performer.GetComponent<PlayerScript>()?.IsInReach(target, true) ?? false) {
						target.GetComponent<PlayerMove>().Cuff(gameObject);

						// Hacky! Hand doesn't automatically update so we have to do it manually
						performer.GetComponent<PlayerNetworkActions>()?.UpdatePlayerEquipSprites(InventoryManager.GetSlotFromItem(gameObject), null);
					}
				}
			}
		);
		
		UIManager.ProgressBar.StartProgress(target.transform.position, applyTime, progressFinishAction, performer);
	}
}
