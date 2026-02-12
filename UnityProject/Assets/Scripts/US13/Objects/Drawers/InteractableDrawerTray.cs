using System;
using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Systems.Inventory;

namespace US13.Objects.Drawers
{
	/// <summary>
	/// Interaction logic for drawer trays.
	/// Enables placing items on the tray and closing the drawer by clicking on the tray.
	/// </summary>
	public class InteractableDrawerTray : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
	{
		[NonSerialized]
		public Drawer parentDrawer;

		#region PositionalHandApply

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (interaction.UsedObject == null)
			{
				parentDrawer.CloseDrawer();
			}
			else
			{
				Inventory.ServerDrop(interaction.HandSlot, interaction.TargetVector);
			}
		}

		#endregion PositionalHandApply
	}
}
