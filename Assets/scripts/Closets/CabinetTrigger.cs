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
    public bool IsClosed;

	[SyncVar (hook="SyncItemSprite")]
    public bool isFull;
	private SpriteRenderer spriteRenderer;
    private bool sync = false;
	void Start()
	{
		spriteRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
	}

	public override void OnStartServer(){
		if(spriteRenderer == null)
			spriteRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
		IsClosed = true;
		isFull = true;
		base.OnStartServer();
	}
	public override void OnStartClient(){
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	IEnumerator WaitForLoad(){
		yield return new WaitForSeconds(3f);
		SyncCabinet(IsClosed);
		SyncItemSprite(isFull);
	}

	public override void Interact(GameObject originator, string hand)
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

	public void SyncItemSprite(bool _isFull){
		if (!isFull) {
			spriteRenderer.sprite = spriteOpenedEmpty;
		} else {
		//TODO putting the sprite back of the fire extinguisher

		}
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

	private bool IsCorrectItem(GameObject item)
	{
		return item.GetComponent<ItemAttributes>().itemName == itemPrefab.GetComponent<ItemAttributes>().itemName;
	}
}