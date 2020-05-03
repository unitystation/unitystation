
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
	private EmptyFullStatus initialState = EmptyFullStatus.Empty;

	[SyncVar(hook = nameof(SyncState))]
	private EmptyFullStatus spriteSync;


	private Pickupable pickupable;
	private ItemAttributesV2 itemAttributes;
	private SpriteRenderer spriteRenderer;

	#region SyncVar boilerplate

	public override void OnStartClient()
	{
		EnsureInit();
		SyncState(spriteSync, initialState);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncState(spriteSync, initialState);
	}

	#endregion

	public void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (spriteRenderer != null) return;
		pickupable = GetComponent<Pickupable>();
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		itemAttributes = GetComponentInChildren<ItemAttributesV2>();

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
			EmptyName = itemAttributes.ArticleName;
		} else if (initialState == EmptyFullStatus.Full && string.IsNullOrEmpty(FullName))
		{
			FullName = itemAttributes.ArticleName;
		}
	}

	[Server]
	public void SetState(EmptyFullStatus value)
	{
		SyncState(spriteSync, value);
	}

	private void SyncState(EmptyFullStatus oldValue, EmptyFullStatus value)
	{
		EnsureInit();
		spriteSync = value;
		spriteRenderer.sprite = spriteSync == EmptyFullStatus.Empty ? EmptySprite : FullSprite;

		if (!string.IsNullOrEmpty(EmptyName) && !string.IsNullOrEmpty(FullName))
		{
			itemAttributes.ServerSetArticleName( spriteSync == EmptyFullStatus.Empty ? EmptyName : FullName );
		}

		pickupable.RefreshUISlotImage();
	}
}

public enum EmptyFullStatus
{
	Empty = 0,
	Full = 1
}