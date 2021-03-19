using HealthV2;
using UnityEngine;

/// <summary>
/// Allows an object to be CPRed by a player.
/// </summary>
public class CPRable : MonoBehaviour, ICheckedInteractable<HandApply>
{
	const float CPR_TIME = 5;
	const float OXYLOSS_HEAL_AMOUNT = 7;

	public float HeartbeatStrength = 25;


	private static readonly StandardProgressActionConfig CPRProgressConfig =
			new StandardProgressActionConfig(StandardProgressActionType.CPR);

	private string performerName;
	private string targetName;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.Intent != Intent.Help) return false;
		if (interaction.HandObject != null) return false;
		if (interaction.TargetObject == interaction.Performer) return false;

		if (interaction.TargetObject.TryGetComponent(out LivingHealthMasterBase targetPlayerHealth))
		{
			if (targetPlayerHealth.ConsciousState == ConsciousState.CONSCIOUS) return false;
		}

		var performerRegisterPlayer = interaction.Performer.GetComponent<RegisterPlayer>();
		if (performerRegisterPlayer.IsLayingDown) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		performerName = interaction.Performer.ExpensiveName();
		targetName = interaction.TargetObject.ExpensiveName();

		var cardiacArrestPlayerRegister = interaction.TargetObject.GetComponent<RegisterPlayer>();
		void ProgressComplete()
		{
			ServerDoCPR(interaction.Performer, interaction.TargetObject, interaction.TargetBodyPart);
		}

		var cpr = StandardProgressAction.Create(CPRProgressConfig, ProgressComplete)
				.ServerStartProgress(cardiacArrestPlayerRegister, CPR_TIME, interaction.Performer);
		if (cpr != null)
		{
			Chat.AddActionMsgToChat(
					interaction.Performer,
					$"You begin performing CPR on {targetName}'s " + interaction.TargetBodyPart,
					$"{performerName} is trying to perform CPR on {targetName}'s " + interaction.TargetBodyPart);
		}
	}

	private void ServerDoCPR(GameObject performer, GameObject target,BodyPartType TargetBodyPart)
	{
		var health = target.GetComponent<LivingHealthMasterBase>();

		health.CirculatorySystem.ConvertUsedBlood(OXYLOSS_HEAL_AMOUNT);

		bool HasHeart = false;
		foreach (var BodyPart in health.GetBodyPartsInZone(TargetBodyPart))
		{
			var heart = BodyPart as Heart;
			if (heart != null)
			{
				heart.Heartbeat(HeartbeatStrength);
				HasHeart = true;
			}

		}

		if (HasHeart)
		{
			Chat.AddActionMsgToChat(
				performer,
				$"You perform CPR on {targetName}.",
				$"{performerName} performs CPR on {targetName}.");
			Chat.AddExamineMsgFromServer(target, $"You feel fresh air enter your lungs. It feels good!");
		}
		else
		{
			Chat.AddActionMsgToChat(
				performer,
				$"You perform CPR on {targetName}. It doesn't seem to work, maybe they're missing something.",
				$"{performerName} performs CPR on {targetName} In vain.");
		}

	}
}
