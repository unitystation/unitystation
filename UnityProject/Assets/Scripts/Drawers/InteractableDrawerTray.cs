using UnityEngine;

/// <summary>
/// Interaction logic for drawer trays. Enables placing items on the tray.
/// </summary>
public class InteractableDrawerTray : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	#region PositionalHandApply

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.HandObject != null) return true;

		return false;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		Inventory.ServerDrop(interaction.HandSlot, interaction.TargetVector);
	}

	#endregion PositionalHandApply
}
