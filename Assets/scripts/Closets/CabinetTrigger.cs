using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using UI;
using Items;
using InputControl;

public class CabinetTrigger: InputTrigger
{
	public Sprite spriteClosed;
	public Sprite spriteOpenedOccupied;
	public Sprite spriteOpenedEmpty;

    public GameObject itemPrefab;

    [SyncVar (hook = "SyncCabinet")]
    public bool IsClosed = true;
    private bool isFull = true;
	private SpriteRenderer spriteRenderer;
    private bool sync = false;
	void Start()
	{
		spriteRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
		IsClosed = true;
	}

	public override void Interact()
	{
        if (IsClosed)
        {
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleFireCabinet(gameObject,false);
        }
        else
        {
            if (isFull)
            {
                if (!UIManager.Hands.CurrentSlot.IsFull)
                {
                    PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryToInstantiateInHand(UIManager.Hands.CurrentSlot.eventName, itemPrefab);
                    PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleFireCabinet(gameObject,true);
                }
                else
                {
                    PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleFireCabinet(gameObject,false);
                }
            }
            else
            {
                PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleFireCabinet(gameObject,false);
            }
        }
	}

    [ClientRpc]
    public void RpcSetEmptySprite(){
        isFull = false;
        spriteRenderer.sprite = spriteOpenedEmpty;
    }

    void SyncCabinet(bool _isClosed){
        if (_isClosed)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    void Open(){
        PlaySound();
        if (isFull) {
            spriteRenderer.sprite = spriteOpenedOccupied;
        } else {
            spriteRenderer.sprite = spriteOpenedEmpty;
        }
    }

    void Close(){
        PlaySound();
        spriteRenderer.sprite = spriteClosed;
    }

    void PlaySound(){
        if (!sync)
        {
            sync = true;
        }
        else
        {
			SoundManager.PlayAtPosition("OpenClose",transform.position);
        }
    }

    //This was stuff that worked with photon (leaving for reference for
    //help with developing the action of putting back the extinguisher
//	private void OnClose()
//	{
//		if (ItemID != null) {
//			var item = UIManager.Hands.CurrentSlot.Item;
//			if (item != null && IsCorrectItem(item)) {
//				var itemViewID = item.GetComponent<NetworkIdentity>().netId;
//				ItemID = itemViewID;
//				item.SetActive(false);
//				item.transform.parent = transform;
//				UIManager.Hands.CurrentSlot.Clear();
//				SetState(itemViewID, false);
//			} else {
//				SetState(ItemID, true);
//			}
//		} else {
//			var item = transform.FindChild("Extinguisher").gameObject;
//			if (ItemManager.TryToPickUpObject(item)) {
//				// remove extinguisher from closet
//				item.SetActive(true);
//                UpdateSprite();
//			}
//		}
//	}


//	public void SetState(NetworkInstanceId itemViewID, bool playSound)
//	{
//		ItemID = itemViewID;
//		if (ItemID != null)
//	
//		if (playSound)
//			SoundManager.Play("OpenClose");
//
//		UpdateSprite();
//	}

	private bool IsCorrectItem(GameObject item)
	{
		return item.GetComponent<ItemAttributes>().itemName == itemPrefab.GetComponent<ItemAttributes>().itemName;
	}
}