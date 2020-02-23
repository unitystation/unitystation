using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformableItem : MonoBehaviour , IPredictedCheckedInteractable<HandApply>
{
	
	[Tooltip("Choose an item to spawn.")]
	[SerializeField]
	private ItemTrait TraitRequired;
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		
		GameObject ObjectInHand = interaction.HandObject;
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if(!Validations.HasItemTrait(ObjectInHand, TraitRequired)) return false;

		if (Validations.HasItemTrait(ObjectInHand, CommonTraits.Instance.Welder) && !Validations.HasUsedActiveWelder(interaction)) return false;
		return true;
	}


	[Tooltip("Choose an item to spawn.")]
	[SerializeField]
	private GameObject TransformTo;
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
