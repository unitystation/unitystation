using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class InteractableFireCabinet : NBHandApplyInteractable
{
	[SyncVar(hook = nameof(SyncCabinet))] public bool IsClosed;

	[SyncVar(hook = nameof(SyncItemSprite))] public bool isFull;

	public GameObject itemPrefab;
	public Sprite spriteClosed;
	public Sprite spriteOpenedEmpty;
	public Sprite spriteOpenedOccupied;
	private SpriteRenderer spriteRenderer;

	//For storing extinguishers server side
	private GameObject storedObject;

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
		storedObject = PoolManager.PoolNetworkInstantiate(itemPrefab, parent: transform.parent);

		base.OnStartServer();
	}

	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	private IEnumerator WaitForLoad()
	{
		yield return WaitFor.Seconds(3f);
		SyncCabinet(IsClosed);
		SyncItemSprite(isFull);
	}

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;

		//only allow interactions targeting this
		if (interaction.TargetObject != gameObject) return false;

		return true;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		PlayerNetworkActions pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

		if (IsClosed)
		{
			if(isFull && interaction.HandObject == null) {
				RemoveExtinguisher(pna, interaction.HandSlot.equipSlot);
			}
			IsClosed = false;
		}
		else
		{
			if (isFull)
			{
				if (interaction.HandObject == null)
				{
					RemoveExtinguisher(pna, interaction.HandSlot.equipSlot);
				}
				else
				{
					IsClosed = true;
				}
			}
			else
			{
				if (interaction.HandObject && interaction.HandObject.GetComponent<FireExtinguisher>())
				{
					AddExtinguisher(interaction);
				}
				else
				{
					IsClosed = true;
				}
			}
		}
	}

	private void RemoveExtinguisher(PlayerNetworkActions pna, EquipSlot hand){
		if (pna.AddItemToUISlot(storedObject.gameObject, hand))
		{
			storedObject = null;
			isFull = false;
		}
	}

	private void AddExtinguisher(HandApply interaction){
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
		InventoryManager.ClearInvSlot(slot);
		storedObject = interaction.HandObject;
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