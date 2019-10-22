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
		if (interaction.HandObject == gameObject
		    && interaction.TargetObject.GetComponent<Headset>() != null)
		{
			UpdateHeadsetKeyMessage.Send(interaction.TargetObject);
			return true;
		}

		return false;
	}
}