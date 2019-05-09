using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FireCabinetTrigger : InputTrigger
{
	[SyncVar(hook = nameof(SyncCabinet))] public bool IsClosed;

	[SyncVar(hook = nameof(SyncItemSprite))] public bool isFull;

	public GameObject itemPrefab;
	public Sprite spriteClosed;
	public Sprite spriteOpenedEmpty;
	public Sprite spriteOpenedOccupied;
	private SpriteRenderer spriteRenderer;

	//For storing extinguishers server side
	[HideInInspector] public ObjectBehaviour storedObject;

	private void Start()
	{
		spriteRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
	}

	public override void OnStartServer()
	{
		if (spriteRenderer == null)
		{
			spriteRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
		}
		IsClosed = true;
		isFull = true;

		GameObject item = PoolManager.PoolNetworkInstantiate(itemPrefab, parent: transform.parent);

		storedObject = item.GetComponent<ObjectBehaviour>();
		storedObject.visibleState = false;
		base.OnStartServer();
	}

	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(3f);
		SyncCabinet(IsClosed);
		SyncItemSprite(isFull);
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!CanUse(originator, hand, position, false))
		{
			return false;
		}
		if (!isServer)
		{
			//ask server to perform the interaction
			InteractMessage.Send(gameObject, position, hand);
			return true;
		}

		PlayerNetworkActions pna = originator.GetComponent<PlayerNetworkActions>();
		GameObject handObj = pna.Inventory[hand].Item;

		if (IsClosed)
		{
			if(isFull && !handObj){
				RemoveExtinguisher(pna, hand);
			}
			IsClosed = false;
		}
		else
		{
			if (isFull)
			{
				if (handObj == null)
				{
					RemoveExtinguisher(pna, hand);
				}
				else
				{
					IsClosed = true;
				}
			}
			else
			{
				if (handObj && handObj.GetComponent<FireExtinguisher>())
				{
					AddExtinguisher(pna, hand, handObj);
				}
				else
				{
					IsClosed = true;
				}
			}
		}
		return true;
	}

	private void RemoveExtinguisher(PlayerNetworkActions pna, string hand){
		if (pna.AddItemToUISlot(storedObject.gameObject, hand))
		{
			storedObject.visibleState = true;
			storedObject = null;
			isFull = false;
		}
	}

	private void AddExtinguisher(PlayerNetworkActions pna, string hand, GameObject handObj){
		storedObject = handObj.GetComponent<ObjectBehaviour>();
		pna.ClearInventorySlot(hand);
		storedObject.visibleState = false;
		isFull = true;
	}

	public void SyncItemSprite(bool value)
	{
		isFull = value;
		if (!isFull)
		{
			spriteRenderer.sprite = spriteOpenedEmpty;
		}
		else
		{
			if (!IsClosed)
			{
				spriteRenderer.sprite = spriteOpenedOccupied;
			}
		}
	}

	private void SyncCabinet(bool value)
	{
		IsClosed = value;
		if (IsClosed)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	private void Open()
	{
		SoundManager.PlayAtPosition("OpenClose", transform.position);
		if (isFull)
		{
			spriteRenderer.sprite = spriteOpenedOccupied;
		}
		else
		{
			spriteRenderer.sprite = spriteOpenedEmpty;
		}
	}

	private void Close()
	{
		SoundManager.PlayAtPosition("OpenClose", transform.position);
		spriteRenderer.sprite = spriteClosed;
	}

}