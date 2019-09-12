using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///	Stores an item in a StorageObject
/// </summary>
public class StoreItemMessage : ClientMessage
{
	public static short MessageType = (short)MessageTypes.StoreItemMessage;
	public NetworkInstanceId player;
	public NetworkInstanceId Storage;
	public EquipSlot PlayerEquipSlot;
	public EquipSlot StoreEquipSlot;
	public bool StoreItem;

	public override IEnumerator Process()
	{
		yield return WaitFor(Storage, player);
		var storageObj = NetworkObjects[0].GetComponent<StorageObject>();
		var pna = NetworkObjects[1].GetComponent<PlayerNetworkActions>();
		var playerInvSlot = pna.Inventory[PlayerEquipSlot];
		if(StoreItem)
		{
			var storageInvSlot = storageObj.NextSpareSlot();
			InventoryManager.EquipInInvSlot(storageInvSlot, playerInvSlot.Item);
			InventoryManager.ClearInvSlot(playerInvSlot);
		}
		else
		{
			//var storageInvSlot = storageObj.GetSlot(StoreEquipSlot);
			//InventoryManager.EquipInInvSlot(playerInvSlot, storageInvSlot.Item);
			//InventoryManager.ClearInvSlot(storageInvSlot);
		}
		storageObj.NotifyPlayer(pna.gameObject);



	}

	public static StoreItemMessage Send(GameObject storage, GameObject player, EquipSlot playerEquipSlot, bool storeItem, EquipSlot storeEquipSlot = 0)
	{
		StoreItemMessage msg = new StoreItemMessage
		{
			PlayerEquipSlot = playerEquipSlot,
			StoreEquipSlot = storeEquipSlot,
			player = player.GetComponent<NetworkIdentity>().netId,
			Storage = storage.GetComponent<NetworkIdentity>().netId,
			StoreItem = storeItem,
		};
		msg.Send();
		return msg;
	}
}
