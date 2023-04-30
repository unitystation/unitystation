using Mirror;
using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using UnityEngine;

/// <summary>
/// Lets null rod and its transformations change from one to another. Can only be used twice before losing the
/// ability to transform.
/// </summary>
public class NullRod : NetworkBehaviour, IInteractable<HandActivate>, IServerSpawn, IServerDespawn
{

	//Number of times the object can transform. Decreases by one everytime a transformation occurs.
	[SyncVar(hook=nameof(SyncTransformTimes))]
	private int transformTimes;

	//Public reference to times left.
	public int TransformTimes => transformTimes;

	//Changes transformTimes syncvar.
	private void SyncTransformTimes(int oldTimesLeft, int timesLeft)
	{
			this.transformTimes = timesLeft;
	}

	public void OnSpawnServer()
	{
		SyncTransformTimes(transformTimes, 2);

	}

	public override void OnStartClient()
	{
		SyncTransformTimes(transformTimes, this.TransformTimes);
		base.OnStartClient();
	}

	[Server]
	public void ServerAdjustTransformTimes(int newTimes)
	{
		SyncTransformTimes(transformTimes, newTimes);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncTransformTimes(transformTimes, 2);
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		//UIManager.Instance.TextInputDialog.ShowDialog("Set label text", OnInputReceived);
		if (transformTimes > 0)
		{
			//Open null rod select screen if there are some transformation charges left.
			TabUpdateMessage.Send( interaction.Performer, gameObject, NetTabType.NullRod, TabAction.Open );
		}
		else
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "The item pulses once and fades. You're out of transformations!");
		}

	}

	public void OnDespawnServer(DespawnInfo info)
	{
		//NetworkTabManager.Instance.RemoveTab(gameObject, NetTabType.NullRod);
	}


	//Server only.
	public void SwapItem(GameObject obj)
	{

		var storage = GetComponent<Pickupable>().ItemSlot.Player.GetComponent<DynamicItemStorage>();
		int currentTimes = TransformTimes;
		string oldItem = gameObject.ExpensiveName();
		_ = Inventory.ServerDespawn(gameObject);

		var item = Spawn.ServerPrefab(obj).GameObject;
		string newItem = item.ExpensiveName();
		var nullComp = item.GetComponent<NullRod>();

		//Reduce amount of transformation charges.
		nullComp.ServerAdjustTransformTimes(currentTimes-1);

		Inventory.ServerAdd(item, storage.GetActiveHandSlot());

		Chat.AddActionMsgToChat(storage.gameObject,
		$"The {oldItem} flashes bright and changes into a {newItem}!",
		$"The {oldItem} flashes bright and changes into a {newItem}!");

	}

}
