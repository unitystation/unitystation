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

	// Interacts if the target isn't cuffed
	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		GameObject target = interaction.TargetObject;
		PlayerMove targetPM = target.GetComponent<PlayerMove>();

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
					// Explicitly 
					if(performer.GetComponent<PlayerScript>()?.IsInReach(target, true) == true) {
						target.GetComponent<PlayerMove>().Cuff(gameObject);

						performer.GetComponent<PlayerNetworkActions>()?.UpdatePlayerEquipSprites(InventoryManager.GetSlotFromItem(gameObject), null);
					}
				}
			}
		);
		
		UIManager.ProgressBar.StartProgress(target.transform.position, applyTime, progressFinishAction, performer);
	}
}
