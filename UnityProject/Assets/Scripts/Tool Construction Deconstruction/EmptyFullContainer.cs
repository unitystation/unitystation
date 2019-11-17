
using Mirror;
using UnityEngine;

/// <summary>
/// Syncs Empty/Full sprites and names for you
/// </summary>
[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(ItemAttributes))]
public class EmptyFullContainer : NetworkBehaviour
{
	[Header("No need to fill sprite/name for initial state")]
	
	public Sprite EmptySprite;
	public Sprite FullSprite;
	
	public string EmptyName;
	public string FullName;
	[SyncVar(hook = nameof(SyncSprite))] public EmptyFullStatus spriteSync;
	private Pickupable pickupable;
	private ItemAttributes itemAttributes;
	private SpriteRenderer spriteRenderer;
	
	public void Awake()
	{
		if ( !pickupable )
		{
			pickupable = GetComponent<Pickupable>();
		}

		if ( !spriteRenderer )
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		}

		if ( !itemAttributes )
		{
			itemAttributes = GetComponentInChildren<ItemAttributes>();
		}
		
		//aid for lazy people. you don't have to fill out fields for name and sprite in their default state
		if (spriteSync == EmptyFullStatus.Empty && EmptySprite == null)
		{
			EmptySprite = spriteRenderer.sprite;
		} else if (spriteSync == EmptyFullStatus.Full && FullSprite == null)
		{
			FullSprite = spriteRenderer.sprite;
		}
		
		if (spriteSync == EmptyFullStatus.Empty && EmptyName == null)
		{
			EmptyName = itemAttributes.itemName;
		} else if (spriteSync == EmptyFullStatus.Full && FullName == null)
		{
			FullName = itemAttributes.itemName;
		}
	}
	public void SyncSprite(EmptyFullStatus value)
	{
		spriteSync = value;
		spriteRenderer.sprite = spriteSync == EmptyFullStatus.Empty ? EmptySprite : FullSprite;
		
		if (!string.IsNullOrEmpty(EmptyName) && !string.IsNullOrEmpty(FullName))
		{
			itemAttributes.SetItemName( spriteSync == EmptyFullStatus.Empty ? EmptyName : FullName );
		}
		
		pickupable.RefreshUISlotImage();
	}
	
}

public enum EmptyFullStatus
{
	Empty = 0,
	Full = 1
}