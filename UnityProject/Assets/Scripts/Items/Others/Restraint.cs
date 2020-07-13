using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for restraining a player (with handcuffs or zip ties etc)
/// </summary>
public class Restraint : MonoBehaviour, ICheckedInteractable<HandApply>
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Restrain);

	/// <summary>
	/// How long it takes to apply the restraints
	/// </summary>
	[SerializeField]
	private float applyTime = 0;

	/// <summary>
	/// How long it takes for another person to remove the restraints
	/// </summary>
	[SerializeField]
	private float removeTime = 0;
	public float RemoveTime => removeTime;

	/// <summary>
	/// How long it takes for self to remove the restraints
	/// </summary>
	[SerializeField]
	private float resistTime = 0;
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

		void ProgressFinishAction()
		{
			if(performer.GetComponent<PlayerScript>()?.IsInReach(target, true) ?? false)
			{
				target.GetComponent<PlayerMove>().Cuff(interaction);
				Chat.AddActionMsgToChat(performer, $"You successfully restrain {target.ExpensiveName()}.",
					$"{performer.ExpensiveName()} successfully restrains {target.ExpensiveName()}.");
			}
		}

		var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
			.ServerStartProgress(target.RegisterTile(), applyTime, performer);
		if (bar != null)
		{
			SoundManager.PlayNetworkedAtPos(sound, target.transform.position, sourceObj: target.gameObject);
			Chat.AddActionMsgToChat(performer,
				$"You begin restraining {target.ExpensiveName()}...",
				$"{performer.ExpensiveName()} begins restraining {target.ExpensiveName()}...");
		}
	}
}
