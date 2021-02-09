using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;

public class TransformableItem : MonoBehaviour , IPredictedCheckedInteractable<HandApply>
{
	
	[Tooltip("Choose an item to spawn.")]
	[SerializeField]
	private ItemTrait TraitRequired = null;
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		
		GameObject ObjectInHand = interaction.HandObject;
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if(!Validations.HasItemTrait(ObjectInHand, TraitRequired)) return false;

		if (Validations.HasItemTrait(ObjectInHand, CommonTraits.Instance.Welder) && !Validations.HasUsedActiveWelder(interaction)) return false;
		return true;
	}
	public void ClientPredictInteraction(HandApply interaction)
	{
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
	private GameObject TransformTo = null;
	//invoked when the server recieves the interaction request and WIllinteract returns true
	public void ServerPerformInteraction(HandApply interaction)
	{
		ItemAttributesV2 attr = interaction.TargetObject.GetComponent<ItemAttributesV2>();
		if(attr.HasTrait(CommonTraits.Instance.Transforamble))
		{

			Spawn.ServerPrefab(TransformTo, interaction.TargetObject.RegisterTile().WorldPositionServer);
			Despawn.ServerSingle(interaction.TargetObject);
		}
		
}
}
