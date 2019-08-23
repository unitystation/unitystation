using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Restraint : Interactable<HandApply>
{
	/// <summary>
	/// How long it takes to apply the restraints
	/// </summary>
	[SerializeField]
	private float applyTime;

	/// <summary>
	/// How long it takes for another person to remove the restraints
	/// </summary>
	[SerializeField]
	private float removeTime;
	public float RemoveTime => removeTime;

	// TODO: Add time it takes to resist out of handcuffs

	/// <summary>
	/// Sound to be played when applying restraints
	/// </summary>
	[SerializeField]
	private string sound = "Handcuffs";

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;

		PlayerMove targetPM = interaction.TargetObject?.GetComponent<PlayerMove>();

		// Interacts iff the target isn't cuffed
		return interaction.UsedObject == gameObject
			&& targetPM != null
			&& targetPM.IsCuffed == false;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		GameObject target = interaction.TargetObject;
		GameObject performer = interaction.Performer;

		var progressFinishAction = new FinishProgressAction(
			reason =>
			{
				if (reason == FinishProgressAction.FinishReason.COMPLETED)
				{
					if(performer.GetComponent<PlayerScript>()?.IsInReach(target, true) ?? false) {
						target.GetComponent<PlayerMove>().Cuff(gameObject, interaction.Performer.GetComponent<PlayerNetworkActions>());
					}
				}
			}
		);

		SoundManager.PlayNetworkedAtPos(sound, target.transform.position);
		UIManager.ProgressBar.StartProgress(target.transform.position, applyTime, progressFinishAction, performer);
	}
}
