using UnityEngine;

public class ExportScanner : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		if (interaction.HandObject != gameObject)
		{
			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var attributes = interaction.TargetObject.GetComponent<ItemAttributesV2>();
		var price = attributes ? attributes.GetExportCost() : 0;
		var exportName = interaction.TargetObject.ExpensiveName();
		var message = price > 0
			? $"Scanned { exportName }, value: { price } credits."
			: $"Scanned { exportName }, no export value.";

		// TODO #2400 if it has contents say contents included.

		Chat.AddExamineMsg(interaction.Performer, message);
	}
}
