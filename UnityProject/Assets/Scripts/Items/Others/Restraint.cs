using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Restraint : MonoBehaviour, ICheckedInteractable<HandApply>
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

	/// <summary>
	/// How long it takes for self to remove the restraints
	/// </summary>
	[SerializeField]
	private float resistTime;
	public float ResistTime => resistTime;

	/// <summary>
	/// Sound to be played when applying restraints
	/// </summary>
	[SerializeField]
	private string sound = "Handcuffs";

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		PlayerMove targetPM = interaction.TargetObject?.GetComponent<PlayerMove>();

		// Interacts iff the target isn't cuffed
		return interaction.UsedObject == gameObject
			&& targetPM != null
			&& targetPM.IsCuffed == false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		GameObject target = interaction.TargetObject;
		GameObject performer = interaction.Performer;

		var progressFinishAction = new ProgressCompleteAction(
			() =>
			{
				if(performer.GetComponent<PlayerScript>()?.IsInReach(target, true) ?? false) {
					target.GetComponent<PlayerMove>().Cuff(interaction);
				}
			}
		);

		var bar = UIManager.ServerStartProgress(ProgressAction.Restrain,  target.transform.position, applyTime, progressFinishAction, performer);
		if (bar != null)
		{
			SoundManager.PlayNetworkedAtPos(sound, target.transform.position);
		}
	}
}
