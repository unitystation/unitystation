using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FireCabinetTrigger : InputTrigger
{
	public bool hasJustPlaced;

	[SyncVar(hook = nameof(SyncCabinet))] public bool IsClosed;

	[SyncVar(hook = nameof(SyncItemSprite))] public bool isFull;

	public GameObject itemPrefab;
	public Sprite spriteClosed;
	public Sprite spriteOpenedEmpty;
	public Sprite spriteOpenedOccupied;
	private SpriteRenderer spriteRenderer;

	//For storing extinguishers server side
	[HideInInspector] public ObjectBehaviour storedObject;

	private bool sync;

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

		if (IsClosed)
		{
			HandleInteraction(false, hand, pna);
		}
		else
		{
			GameObject handObj = pna.Inventory[hand].Item;
			if (isFull && !hasJustPlaced)
			{
				if (handObj == null)
				{
					HandleInteraction(true, hand, pna);
				}
				else
				{
					HandleInteraction(false, hand, pna);
				}
			}
			else
			{
				if (handObj != null)
				{
					if (handObj.GetComponent<ItemAttributes>().itemName == "Extinguisher")
					{
						HandleInteraction(true, hand, pna);
						hasJustPlaced = true;
					}
					else
					{
						HandleInteraction(false, hand, pna);
						hasJustPlaced = false;
					}
				}
				else
				{
					HandleInteraction(false, hand, pna);
					hasJustPlaced = false;
				}
			}
		}

		return true;
	}

	private void HandleInteraction(bool forItemInteract, string currentHand, PlayerNetworkActions pna)
	{
		pna.CmdToggleFireCabinet(gameObject, forItemInteract, currentHand);
	}

	public void SyncItemSprite(bool _isFull)
	{
		isFull = _isFull;
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

	private void SyncCabinet(bool _isClosed)
	{
		IsClosed = _isClosed;
		if (_isClosed)
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
		PlaySound();
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
		PlaySound();
		spriteRenderer.sprite = spriteClosed;
	}

	private void PlaySound()
	{
		if (!sync)
		{
			sync = true;
		}
		else
		{
			SoundManager.PlayAtPosition("OpenClose", transform.position);
		}
	}

	private bool IsCorrectItem(GameObject item)
	{
		return item.GetComponent<ItemAttributes>().itemName == itemPrefab.GetComponent<ItemAttributes>().itemName;
	}
}