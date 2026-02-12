using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Messages.Client;
using US13.Systems.Inventory;

namespace US13.Items.Tool
{
	/// <summary>
	/// Allows object to function as a screwdriver.
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	public class Screwdriver : MonoBehaviour, IClientInteractable<InventoryApply>
	{
		public bool Interact(InventoryApply interaction)
		{
			//remove the headset key if this is used on a headset
			if (interaction.UsedObject == gameObject
			    && interaction.TargetObject.GetComponent<Headset>() != null
			    && interaction.IsFromHandSlot)
			{
				UpdateHeadsetKeyMessage.Send(interaction.TargetObject);
				return true;
			}
			return false;
		}
	}
}