using Mirror;
using UnityEngine;

public class ContainerConstruct : MonoBehaviour, ICheckedInteractable<HandApply>
{
	[Tooltip("Put the item in that should be used to spawn assembly")]
	public GameObject craftItem;
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		string hand = interaction.HandObject != null ? interaction.HandObject.GetComponent<ItemAttributesV2>().InitialName : null;
		string neededItem = craftItem.GetComponent<ItemAttributesV2>().InitialName;
		var storage = gameObject.GetComponent<ItemStorage>();
		if (hand == neededItem & storage == null) return true;
		return false;

	}

	//this is invoked when WillInteract returns true on the client side.
	//We can implement this to add client prediction logic to make the game feel more responsive.
	public void ClientPredictInteraction(HandApply interaction)
	{
		
	}

	//this is invoked on the server when client requests an interaction
	//but the server's WillInteract method returns false. So the server may need to tell
	// the client their prediction is wrong, depending on how client prediction works for this
	// interaction.
	public void ServerRollbackClient(HandApply interaction)
	{
		
	}

	//invoked when the server recieves the interaction request and WIllinteract returns true
	public void ServerPerformInteraction(HandApply interaction)
	{
		//Server-side trigger the explosion and inform all clients of it
		
	}
}
