using UnityEngine;

/// <summary>
/// Allows an object to be disarmed by a player.
/// </summary>
public class Disarmable : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	// This is based off the alien/humanoid/attack_hand disarm code of TGStation's codebase.
	// Disarms have 5% chance to knock down, then it has a 50% chance to disarm.

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
		if (!DefaultWillInteract.Default(interaction, side)) return false;
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
			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.PunchMiss, interactionWorldPosition, sourceObj: target);

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

		SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.ThudSwoosh, interactionWorldPosition, sourceObj: target);
		Chat.AddCombatMsgToChat(
				performer,
				$"You knock {targetName} down!",
				$"{performerName} knocks {targetName} down!");
		Chat.AddExamineMsgFromServer(target, $"{performerName} knocks you down!");
	}

	private void Disarm()
	{
		var disarmStorage = target.GetComponent<ItemStorage>();
		var leftHandSlot = disarmStorage.GetNamedItemSlot(NamedSlot.leftHand);
		var rightHandSlot = disarmStorage.GetNamedItemSlot(NamedSlot.rightHand);

		if (leftHandSlot.Item != null)
		{
			Inventory.ServerDrop(leftHandSlot);
		}

		if (rightHandSlot.Item != null)
		{
			Inventory.ServerDrop(rightHandSlot);
		}

		SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.ThudSwoosh, interactionWorldPosition, sourceObj: target);
		Chat.AddCombatMsgToChat(
				performer,
				$"You successfully disarm {targetName}!",
				$"{performerName} disarms {targetName}!");
		Chat.AddExamineMsgFromServer(target, $"{performerName} disarms you!");
	}
}
