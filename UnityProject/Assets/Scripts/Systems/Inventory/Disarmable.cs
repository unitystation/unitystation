using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows an object to be disarmed by a player.
/// </summary>
public class Disarmable : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, ICooldown
{
	// This is based off the alien/humanoid/attack_hand disarm code of TGStation's codebase.
	// Disarms have 5% chance to knock down, then it has a 50% chance to disarm.

	public float TimeBetweenDisarms = 0.5f;

	public float DefaultTime => TimeBetweenDisarms;

	const float KNOCKDOWN_CHANCE = 5; // Percent
	const float DISARM_CHANCE = 50; // Percent
	const float KNOCKDOWN_STUN_TIME = 6; // Seconds

	private GameObject performer;
	private GameObject target;
	private string performerName;
	private string targetName;
	private Vector2 interactionWorldPosition;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.Intent != Intent.Disarm) return false;
		if (interaction.TargetObject == interaction.Performer) return false;

		var performerRegisterPlayer = interaction.Performer.GetComponent<RegisterPlayer>();
		if (performerRegisterPlayer.IsLayingDown) return false;

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		performer = interaction.Performer;
		target = interaction.TargetObject;
		performerName = interaction.Performer.ExpensiveName();
		targetName = interaction.TargetObject.ExpensiveName();
		interactionWorldPosition = interaction.WorldPositionTarget;

		if (Cooldowns.TryStart(interaction, this, side: NetworkSide.Server) == false) return;

		var rng = new System.Random();
		if (rng.Next(1, 100) <= KNOCKDOWN_CHANCE)
		{
			KnockDown();
		}

		else if (rng.Next(1, 100) <= DISARM_CHANCE)
		{
			Disarm();
		}
		else
		{
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.PunchMiss, interactionWorldPosition, sourceObj: target);

			Chat.AddCombatMsgToChat(
					performer,
					$"You fail to disarm {targetName}!",
					$"{performerName} attempts to disarm {targetName}!");
			Chat.AddExamineMsgFromServer(target, $"{performerName} attempts to disarm you!");
		}
	}

	private void KnockDown()
	{
		var targetRegister = target.GetComponent<RegisterPlayer>();
		targetRegister.ServerStun(KNOCKDOWN_STUN_TIME, false);

		SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.ThudSwoosh, interactionWorldPosition, sourceObj: target);
		Chat.AddCombatMsgToChat(
				performer,
				$"You knock {targetName} down!",
				$"{performerName} knocks {targetName} down!");
		Chat.AddExamineMsgFromServer(target, $"{performerName} knocks you down!");
	}

	private void Disarm()
	{
		var disarmStorage = target.GetComponent<DynamicItemStorage>();
		if(disarmStorage == null) return;

		var leftHandSlots = disarmStorage.GetNamedItemSlots(NamedSlot.leftHand);
		var rightHandSlots = disarmStorage.GetNamedItemSlots(NamedSlot.rightHand);

		foreach (var leftHandSlot in leftHandSlots)
		{
			if (leftHandSlot.IsEmpty) continue;

			Inventory.ServerDrop(leftHandSlot);
		}

		foreach (var rightHandSlot in rightHandSlots)
		{
			if (rightHandSlot.IsEmpty) continue;

			Inventory.ServerDrop(rightHandSlot);
		}

		SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.ThudSwoosh, interactionWorldPosition, sourceObj: target);
		Chat.AddCombatMsgToChat(
				performer,
				$"You successfully disarm {targetName}!",
				$"{performerName} disarms {targetName}!");
		Chat.AddExamineMsgFromServer(target, $"{performerName} disarms you!");
	}
}
