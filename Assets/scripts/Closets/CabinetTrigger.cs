using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using UI;
using Network;
using Items;
using InputControl;

public class CabinetTrigger: InputTrigger
{
	public Sprite spriteClosed;
	public Sprite spriteOpenedOccupied;
	public Sprite spriteOpenedEmpty;

	public GameObject itemPrefab;

	public bool IsClosed { get; private set; }

	public NetworkInstanceId ItemID { get; private set; }

	private SpriteRenderer spriteRenderer;

	void Start()
	{
		spriteRenderer = transform.FindChild("Sprite").GetComponent<SpriteRenderer>();
		IsClosed = true;

		ItemID = transform.FindChild("Extinguisher").GetComponent<NetworkIdentity>().netId;
	}

	public override void Interact()
	{
		if (IsClosed) {
			CmdSetState(false, ItemID, true);
		} else {
			OnClose();
		}
	}

	private void OnClose()
	{
		if (ItemID != null) {
			var item = UIManager.Hands.CurrentSlot.Item;
			if (item != null && IsCorrectItem(item)) {
				var itemViewID = item.GetComponent<NetworkIdentity>().netId;
				ItemID = itemViewID;
				item.SetActive(false);
				item.transform.parent = transform;
				UIManager.Hands.CurrentSlot.Clear();
				CmdSetState(IsClosed, itemViewID, false);
			} else {
				CmdSetState(true, ItemID, true);
			}
		} else {
			var item = transform.FindChild("Extinguisher").gameObject;
			if (ItemManager.TryToPickUpObject(item)) {
				// remove extinguisher from closet
				item.SetActive(true);
				Debug.Log("TODO: Fix THIS!");
//				CmdSetState(IsClosed, null, false);
			}
		}
	}

	[Command]
	public void CmdSetState(bool isClosed, NetworkInstanceId itemViewID, bool playSound)
	{
		SetState(isClosed, itemViewID, playSound);
		RpcSetState(IsClosed, itemViewID, playSound);
	}

	[ClientRpc]
	void RpcSetState(bool isClosed, NetworkInstanceId itemViewID, bool playSound)
	{
		SetState(IsClosed, itemViewID, playSound);
	}

	public void SetState(bool isClosed, NetworkInstanceId itemViewID, bool playSound)
	{
		IsClosed = isClosed;
		ItemID = itemViewID;
		if (ItemID != null)
	
		if (playSound)
			SoundManager.Play("OpenClose");

		UpdateSprite();
	}

	private void UpdateSprite()
	{
		if (IsClosed) {
			spriteRenderer.sprite = spriteClosed;
		} else if (ItemID != null) {
			spriteRenderer.sprite = spriteOpenedOccupied;
		} else {
			spriteRenderer.sprite = spriteOpenedEmpty;
		}
	}

	private bool IsCorrectItem(GameObject item)
	{
		return item.GetComponent<ItemAttributes>().itemName == itemPrefab.GetComponent<ItemAttributes>().itemName;
	}
}