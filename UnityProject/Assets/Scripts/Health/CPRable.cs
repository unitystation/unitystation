using UnityEngine;

/// <summary>
/// Allows an object to be CPRed by a player.
/// </summary>
public class CPRable : MonoBehaviour, ICheckedInteractable<HandApply>
{
	const float CPR_TIME = 5;
	const float OXYLOSS_HEAL_AMOUNT = 7;

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

		if (interaction.TargetObject.TryGetComponent(out PlayerHealth targetPlayerHealth))
		{
			if (targetPlayerHealth.ConsciousState == ConsciousState.CONSCIOUS ||
				targetPlayerHealth.ConsciousState == ConsciousState.DEAD) return false;
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
			ServerDoCPR(interaction.Performer, interaction.TargetObject);
		}

		var cpr = StandardProgressAction.Create(CPRProgressConfig, ProgressComplete)
				.ServerStartProgress(cardiacArrestPlayerRegister, CPR_TIME, interaction.Performer);
		if (cpr != null)
		{
			Chat.AddActionMsgToChat(
					interaction.Performer,
					$"You begin performing CPR on {targetName}.",
					$"{performerName} is trying to perform CPR on {targetName}.");
		}
	}

	private void ServerDoCPR(GameObject performer, GameObject target)
	{
		target.GetComponent<PlayerHealth>().bloodSystem.oxygenDamage -= OXYLOSS_HEAL_AMOUNT;

		Chat.AddActionMsgToChat(
				performer,
				$"You perform CPR on {targetName}.",
				$"{performerName} performs CPR on {targetName}.");
		Chat.AddExamineMsgFromServer(target, $"You feel fresh air enter your lungs. It feels good!");
	}
}
