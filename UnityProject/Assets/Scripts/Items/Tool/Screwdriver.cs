using Messages.Client;
using UnityEngine;

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