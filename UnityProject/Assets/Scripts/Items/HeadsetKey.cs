using UnityEngine;

/// <summary>
/// Allows the object to function as a headset key - to be inserted into a headset to set its encryption key.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class HeadsetKey : MonoBehaviour, IInteractable<InventoryApply>
{
	public bool Interact(InventoryApply interaction)
	{
		//insert the headset key if this is used on a headset
		if (interaction.HandObject == gameObject
		    && interaction.TargetObject.GetComponent<Headset>() != null)
		{
			UpdateHeadsetKeyMessage.Send(interaction.TargetObject, gameObject);
			return true;
		}

		return false;
	}
}