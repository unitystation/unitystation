using System.Collections;
using PlayGroup;
using PlayGroups.Input;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public class FireCabinetTrigger : InputTrigger
{
	private bool hasJustPlaced;

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

		GameObject item = ItemFactory.SpawnItem(itemPrefab, transform.parent);
		
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

	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		if (IsClosed)
		{
			HandleInteraction(false, hand);
		}
		else
		{
			if (isFull && !hasJustPlaced)
			{
				if (!UIManager.Hands.CurrentSlot.IsFull)
				{
					HandleInteraction(true, hand);
				}
				else
				{
					HandleInteraction(false, hand);
				}
			}
			else
			{
				GameObject handItem = UIManager.Hands.CurrentSlot.Item;
				if (handItem != null)
				{
					if (handItem.GetComponent<ItemAttributes>().itemName == "Extinguisher")
					{
						HandleInteraction(true, hand);
						hasJustPlaced = true;
					}
					else
					{
						HandleInteraction(false, hand);
						hasJustPlaced = false;
					}
				}
				else
				{
					HandleInteraction(false, hand);
					hasJustPlaced = false;
				}
			}
		}
	}

	private void HandleInteraction(bool forItemInteract, string currentHand)
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleFireCabinet(
			gameObject, forItemInteract, currentHand);
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