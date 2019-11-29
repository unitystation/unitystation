
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Syncs Empty/Full sprites and names for you
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class EmptyFullSync : NetworkBehaviour, IServerSpawn
{
	[Header("No need to fill sprite/name for initial state")]

	public Sprite EmptySprite;
	public Sprite FullSprite;

	public string EmptyName;
	public string FullName;

	[SerializeField] [FormerlySerializedAs(nameof(spriteSync))]
	private EmptyFullStatus initialState;

	[SyncVar(hook = nameof(SyncState))]
	private EmptyFullStatus spriteSync;


	private Pickupable pickupable;
	private IItemAttributes itemAttributes;
	private SpriteRenderer spriteRenderer;

	#region SyncVar boilerplate

	public override void OnStartClient()
	{
		SyncState(initialState);
		base.OnStartClient();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncState(initialState);
	}

	#endregion

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

		if ( itemAttributes == null )
		{
			itemAttributes = GetComponentInChildren<IItemAttributes>();
		}

		//aid for lazy people. you don't have to fill out fields for name and sprite in their default state
		if (initialState == EmptyFullStatus.Empty && EmptySprite == null)
		{
			EmptySprite = spriteRenderer.sprite;
		} else if (initialState == EmptyFullStatus.Full && FullSprite == null)
		{
			FullSprite = spriteRenderer.sprite;
		}

		if (initialState == EmptyFullStatus.Empty && string.IsNullOrEmpty(EmptyName))
		{
			EmptyName = itemAttributes.ItemName;
		} else if (initialState == EmptyFullStatus.Full && string.IsNullOrEmpty(FullName))
		{
			FullName = itemAttributes.ItemName;
		}
	}

	[Server]
	public void SetState(EmptyFullStatus value)
	{
		SyncState(value);
	}

	private void SyncState(EmptyFullStatus value)
	{
		spriteSync = value;
		spriteRenderer.sprite = spriteSync == EmptyFullStatus.Empty ? EmptySprite : FullSprite;

		if (!string.IsNullOrEmpty(EmptyName) && !string.IsNullOrEmpty(FullName))
		{
			itemAttributes.ServerSetItemName( spriteSync == EmptyFullStatus.Empty ? EmptyName : FullName );
		}

		pickupable.RefreshUISlotImage();
	}
}

public enum EmptyFullStatus
{
	Empty = 0,
	Full = 1
}