using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Restraint : MeleeItemTrigger
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

	public override bool MeleeItemInteract(GameObject originator, GameObject victim)
	{
		PlayerMove victimPM = victim.GetComponent<PlayerMove>();

		if (victimPM == null || victimPM.IsCuffed)
			return true;

		SoundManager.PlayNetworkedAtPos(sound, victim.transform.position);

		var progressFinishAction = new FinishProgressAction(
			reason =>
			{
				if (reason == FinishProgressAction.FinishReason.COMPLETED)
				{
					victimPM.Cuff(gameObject);

					originator.GetComponent<PlayerNetworkActions>()?.UpdatePlayerEquipSprites(InventoryManager.GetSlotFromItem(gameObject), null);
				}
			}
		);
		
		UIManager.ProgressBar.StartProgress(victim.transform.position, applyTime, progressFinishAction, originator);
		return false;
	}
}
