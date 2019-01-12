using UnityEngine;
using UnityEngine.Networking;


public class PickUpTrigger : InputTrigger
{
	private void Start()
	{
		CheckSpriteOrder();
	}
	[ContextMethod("Pick Up", "hand")]
	public void GUIInteract()
	{
		Interact(
		PlayerManager.LocalPlayerScript.gameObject,
		PlayerManager.LocalPlayerScript.WorldPos,
		UIManager.Instance.hands.CurrentSlot.eventName);
	}
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (originator.GetComponent<PlayerScript>().canNotInteract())
		{
			return true;
		}

		if (!isServer)
		{
			UISlotObject uiSlotObject = new UISlotObject(InventoryManager.GetClientUUIDFromSlotName(hand), gameObject);

			//PreCheck
			if (UIManager.CanPutItemToSlot(uiSlotObject))
			{
				//Simulation
				gameObject.GetComponent<CustomNetTransform>().DisappearFromWorld();
				//                    UIManager.UpdateSlot(uiSlotObject);

				//Client informs server of interaction attempt
				InteractMessage.Send(gameObject, hand);

				return true;
			}

			return true;

		}
		else
		{
			//Server actions
			if (!ValidatePickUp(originator, hand))
			{
				//Rollback prediction (inform player about item's true state)
				GetComponent<CustomNetTransform>().NotifyPlayer(originator);
			}
			else
			{
				OnPickUpServer(originator.GetComponent<NetworkIdentity>().netId);
			}

			return true;
		}

		return true;
	}

	//Broadcast from InventoryManager on server
	public void OnRemoveFromInventory()
	{
		OnDropItemServer();
	}
	public virtual void OnPickUpServer(NetworkInstanceId ownerId) { }
	public virtual void OnDropItemServer() { }

	[Server]
	public virtual bool ValidatePickUp(GameObject originator, string handSlot)
	{
		var ps = originator.GetComponent<PlayerScript>();

		if (ps == null)
		{
			return false;
		}

		var targetSlot = InventoryManager.GetSlotFromOriginatorHand(originator, handSlot);
		if (targetSlot == null)
		{
			Logger.Log("Slot not found!", Category.Inventory);
			return false;
		}

		if (targetSlot.Item != null)
		{
			Logger.Log("Slot is full!", Category.Inventory);
			return false;
		}


		var cnt = GetComponent<CustomNetTransform>();
		var state = cnt.ServerState;

		if (cnt.IsFloatingServer ? !CanReachFloating(ps, state) : !ps.IsInReach(cnt.RegisterTile))
		{
			Logger.LogWarning($"Not in reach! server pos:{state.WorldPosition} player pos:{originator.transform.position} (floating={cnt.IsFloatingServer})", Category.Security);
			return false;
		}
		Logger.LogTraceFormat("Pickup success! server pos:{0} player pos:{1} (floating={2})", Category.Security,
			state.WorldPosition, originator.transform.position, cnt.IsFloatingServer);


		//set ForceInform to false for simulation
		return ps.playerNetworkActions.AddItemToUISlot(gameObject, targetSlot.SlotName, false /*, false*/);
	}

	/// <summary>
	///     Making reach check less strict when object is flying, otherwise high ping players can never catch shit!
	/// </summary>
	private static bool CanReachFloating(PlayerScript ps, TransformState state)
	{
		return ps.IsInReach(state.WorldPosition) || ps.IsInReach(state.WorldPosition - (Vector3)state.Impulse, 2f);
	}

	/// <summary>
	///     If a SpriteRenderer.sortingOrder is 0 then there will be difficulty
	///     interacting with the object via the InputTrigger especially when placed on
	///     tables. This method makes sure that it is never 0 on start
	/// </summary>
	private void CheckSpriteOrder()
	{
		SpriteRenderer sR = GetComponentInChildren<SpriteRenderer>();
		if (sR != null)
		{
			if (sR.sortingLayerName == "Items" && sR.sortingOrder == 0)
			{
				sR.sortingOrder = 1;
			}
		}
	}
}
