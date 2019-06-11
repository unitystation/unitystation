using UnityEngine;

/// <summary>
/// Allows object to function as a screwdriver.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class Screwdriver : MonoBehaviour, IInteractable<InventoryApply>
{
	public InteractionControl Interact(InventoryApply interaction)
	{
		//remove the headset key if this is used on a headset
		if (interaction.UsedObject == gameObject
		    && interaction.TargetObject.GetComponent<Headset>() != null)
		{
			UpdateHeadsetKeyMessage.Send(interaction.TargetObject);
			return InteractionControl.STOP_PROCESSING;
		}

		return InteractionControl.CONTINUE_PROCESSING;
	}
}