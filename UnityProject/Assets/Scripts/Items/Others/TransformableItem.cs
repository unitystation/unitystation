using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformableItem : MonoBehaviour , IPredictedCheckedInteractable<HandApply>
{
	// Start is called before the first frame update
	//this method is invoked on the client side before informing the server of the interaction.
	//If it returns false, no message is sent.
	//If it returns true, the message is sent to the server. Then this is invoked on the server side, and if 
	//it returns true, the server finally performs the interaction.
	//We don't NEED to implement this method, but by implementing it we can cut down on the amount of messages
	//sent to the server.
	[Tooltip("Choose an item to spawn.")]
	[SerializeField]
	private ItemTrait TraitRequired;
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//the Default method defines the "default" behavior for when a HandApply should occur, which currently
		//checks if the player is conscious and standing next to the thing they are clicking.
		GameObject ObjectInHand = interaction.HandObject;
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//we only want this interaction to happen when a lit welder is used
		//interaction.HandObject.GetComponent<Welder>()
		if(!Validations.HasItemTrait(ObjectInHand, TraitRequired)) return false;
		//ObjectInHand.GetComponents<IInteractable<HandActivate>>();
		if (Validations.HasItemTrait(ObjectInHand, CommonTraits.Instance.Welder) && !Validations.HasUsedActiveWelder(interaction)) return false;
		return true;
	}

	//this is invoked when WillInteract returns true on the client side.
	//We can implement this to add client prediction logic to make the game feel more responsive.
	public void ClientPredictInteraction(HandApply interaction)
	{
		//display an explosion effect on this client that has no effect on their actual health
		
	}

	//this is invoked on the server when client requests an interaction
	//but the server's WillInteract method returns false. So the server may need to tell
	// the client their prediction is wrong, depending on how client prediction works for this
	// interaction.
	public void ServerRollbackClient(HandApply interaction)
	{
		//Server should send the client a message or invoke a ClientRpc telling it to
		//undo its prediction or reset its state to sync with the server
		
	}

	[Tooltip("Choose an item to spawn.")]
	[SerializeField]
	private GameObject TransformTo;
	//invoked when the server recieves the interaction request and WIllinteract returns true
	public void ServerPerformInteraction(HandApply interaction)
	{
		//Server-side trigger the explosion and inform all clients of it
		ItemAttributesV2 attr = interaction.TargetObject.GetComponent<ItemAttributesV2>();
		if(attr.HasTrait(CommonTraits.Instance.Transforamble))
		{
			//interaction.TargetObject.transform.position
			Spawn.ServerPrefab(TransformTo, interaction.TargetObject.RegisterTile().WorldPositionServer);
			Despawn.ServerSingle(interaction.TargetObject);
		}
		
}
}
