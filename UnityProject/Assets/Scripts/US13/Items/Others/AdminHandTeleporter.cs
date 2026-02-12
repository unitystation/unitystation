using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;

namespace US13.Items.Others
{
	/// <summary>
	/// Simple hand teleporter for convenience of admins.
	/// </summary>
	public class AdminHandTeleporter : MonoBehaviour, ICheckedInteractable<AimApply>
	{
		public void ServerPerformInteraction(AimApply interaction)
		{
			if (interaction.MouseButtonState == MouseButtonState.PRESS)
			{
				interaction.PerformerPlayerScript.PlayerSync.AppearAtWorldPositionServer(interaction.WorldPositionTarget.RoundToInt().To2(), true);
			}
		}

		public bool WillInteract(AimApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}
	}
}
