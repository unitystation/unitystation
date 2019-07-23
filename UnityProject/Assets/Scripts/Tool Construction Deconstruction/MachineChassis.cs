using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineChassis : MonoBehaviour, IConstructionHandler
{

	public bool InteractionUpdate(HandApply interaction, InventorySlot Slot, ConstructionHandler Handler) {
		if (Slot?.Item != null) {			var Circuit = Slot.Item.GetComponent<CircuitBoard>();
			if (Circuit != null)
			{
				if (CustomNetworkManager.Instance._isServer == true)
				{
					var _Object = PoolManager.PoolNetworkInstantiate(Circuit.ConstructionTarget, this.transform.position, parent: this.transform.parent);
					var CH = _Object.GetComponent<ConstructionHandler>();
					CustomNetTransform netTransform = _Object.GetComponent<CustomNetTransform>();
					netTransform.AppearAtPosition(this.transform.position);
					netTransform.AppearAtPositionServer(this.transform.position);
					CH.GoToStage(Circuit.StartAtStage);
					CH.GenerateComponents = false;
					CH.CircuitBoard = Slot.Item;
					InventoryManager.UpdateInvSlot(true, "", interaction.HandObject, Slot.UUID);
					Destroy(this.gameObject);
				}
			}
		}


		return (false);

	}

	public bool CanInteraction(HandApply interaction, InventorySlot Slot, ConstructionHandler Handler) { 
		if (Slot?.Item != null)
		{
			var Circuit = Slot.Item.GetComponent<CircuitBoard>();
			if (Circuit != null)
			{
				return (true);
			}
		}
		return (false);
	}
 
}
