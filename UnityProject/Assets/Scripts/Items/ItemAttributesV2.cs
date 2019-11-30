
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IngameDebugConsole;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;
using Random = System.Random;

/// <summary>
/// Various attributes associated with a particular item.
/// New and improved, removes need for UniCloth type stuff, works
/// well with using prefab variants.
/// </summary>
[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(ObjectBehaviour))]
[RequireComponent(typeof(RegisterItem))]
[RequireComponent(typeof(CustomNetTransform))]
public class ItemAttributesV2 : NetworkBehaviour, IRightClickable, IServerSpawn
{

	[Tooltip("Display name of this item when spawned.")]
	[SerializeField]
	private string initialName;
	public string InitialName => initialName;

	/// <summary>
	/// Current name
	/// </summary>
	[SyncVar(hook=nameof(SyncItemName))]
	private string itemName;
	/// <summary>
	/// Item's current name
	/// </summary>
	public string ItemName => itemName;

	[Tooltip("Description of this item when spawned.")]
	[SerializeField]
	private string initialDescription;
	/// <summary>
	/// Current description
	/// </summary>
	[SyncVar(hook=nameof(SyncItemDescription))]
	private string itemDescription;

	[SerializeField]
	[Tooltip("Initial traits of this item on spawn.")]
	private List<ItemTrait> initialTraits;

	[Tooltip("Size of this item when spawned.")]
	[SerializeField]
	private ItemSize initialSize;

	/// <summary>
	/// Current size.
	/// </summary>
	[SyncVar(hook=nameof(SyncSize))]
	private ItemSize size;
	/// <summary>
	/// Current size
	/// </summary>
	public ItemSize Size => size;

	[Tooltip("Damage when we click someone with harm intent")]
	[Range(0, 100)]
	[SerializeField]
	private float hitDamage = 0;
	/// <summary>
	/// Damage when we click someone with harm intent, tracked server side only.
	/// </summary>
	public float ServerHitDamage
	{
		get => hitDamage;
		set => hitDamage = value;
	}

	[Tooltip("Type of damage done when this is thrown or used for melee.")]
	[SerializeField]
	private DamageType damageType = DamageType.Brute;
	/// <summary>
	/// Type of damage done when this is thrown or used for melee, tracked server side only.
	/// </summary>
	public DamageType ServerDamageType
	{
		get => damageType;
		set => damageType = value;
	}

	[Tooltip("How painful it is when someone throws it at you")]
	[Range(0, 100)]
	[SerializeField]
	private float throwDamage = 0;
	/// <summary>
	/// Amout of damage done when this is thrown, tracked server side only.
	/// </summary>
	public float ServerThrowDamage
	{
		get => throwDamage;
		set => throwDamage = value;
	}

	[Tooltip("How many tiles to move per 0.1s when being thrown")]
	[SerializeField]
	private float throwSpeed = 2;
	/// <summary>
	/// How many tiles to move per 0.1s when being thrown
	/// </summary>
	public float ThrowSpeed => throwSpeed;

	[Tooltip("Max throw distance")]
	[SerializeField]
	private float throwRange = 7;
	/// <summary>
	/// Max throw distance
	/// </summary>
	public float ThrowRange => throwRange;

	[Tooltip("Sound to be played when we click someone with harm intent")]
	[SerializeField]
	private string hitSound = "GenericHit";
	/// <summary>
	/// Sound to be played when we click someone with harm intent, tracked server side only
	/// </summary>
	public string ServerHitSound
	{
		get => hitSound;
		set => hitSound = value;
	}

	//TODO: tank / eva fields should probably be migrated to a different component as they are very specific to clothing, particularly
	//suits and masks. Probably belong in the Clothing component.
	[Tooltip("Is this a mask that can connect to a tank?")]
	[SerializeField]
	private bool canConnectToTank;
	/// <summary>
	/// Whether this item can connect to a gas tank.
	/// </summary>
	public bool CanConnectToTank => canConnectToTank;


	[Tooltip("Can this item protect humans against spess?")]
	[SerializeField]
	private bool isEVACapable;
	/// <summary>
	/// Can this item protect humans against spess?
	/// </summary>
	public bool IsEVACapable => isEVACapable;

	[Tooltip("Possible verbs used to describe the attack when this is used for melee.")]
	[SerializeField]
	private List<string> attackVerbs;
	/// <summary>
	/// Possible verbs used to describe the attack when this is used for melee, tracked server side only.
	/// </summary>
	public IEnumerable<string> ServerAttackVerbs
	{
		get => attackVerbs;
		set => attackVerbs = new List<string>(value);
	}

	[Tooltip("How much does one of these sell for when shipped on the cargo shuttle?")]
	[SerializeField]
	private int ExportCost; // Use GetExportCost to obtain!

	[Tooltip("Should an alternate name be used when displaying this in the cargo console report?")]
	public string ExportName;

	[Tooltip("Additional message to display in the cargo console report.")]
	public string ExportMessage;

	/// <summary>
	/// Actual current traits, accounting for dynamic add / remove. Note that these adds / removes
	/// are not currently synced between client / server.
	/// </summary>
	private HashSet<ItemTrait> traits = new HashSet<ItemTrait>();

	/// <summary>
	/// SpriteDataHandler on this object
	/// </summary>
	public SpriteDataHandler SpriteDataHandler => spriteDataHandler;
	private SpriteDataHandler spriteDataHandler;

	private void Awake()
	{
		foreach (var definedTrait in initialTraits)
		{
			traits.Add(definedTrait);
		}
		spriteDataHandler = GetComponentInChildren<SpriteDataHandler>();
	}


	public override void OnStartClient()
	{
		SyncItemName(this.name);
		SyncSize(this.size);
		SyncItemDescription(this.itemDescription);
		base.OnStartClient();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncItemName(initialName);
		SyncSize(initialSize);
		SyncItemDescription(initialDescription);
	}

	/// <summary>
	/// All traits currently on the item.
	/// NOTE: Dynamically added / removed traits are not synced between client / server
	/// </summary>
	/// <returns></returns>
	public IEnumerable<ItemTrait> GetTraits()
	{
		return traits;
	}

	/// <summary>
	/// Does it have the given trait?
	/// NOTE: Dynamically added / removed traits are not synced between client / server
	/// </summary>
	/// <param name="itemTrait"></param>
	public bool HasTrait(ItemTrait toCheck)
	{
		return traits.Contains(toCheck);
	}


	/// <summary>
	/// Does it have any of the given traits?
	/// </summary>
	/// <param name="expectedTraits"></param>
	/// <returns></returns>
	public bool HasAnyTrait(IEnumerable<ItemTrait> expectedTraits)
	{
		return traits.Any(expectedTraits.Contains);
	}

	/// <summary>
	/// Does it have all of the given traits?
	/// </summary>
	/// <param name="expectedTraits"></param>
	/// <returns></returns>
	public bool HasAllTraits(IEnumerable<ItemTrait> expectedTraits)
	{
		return traits.All(expectedTraits.Contains);
	}

	/// <summary>
	/// Adds the trait dynamically
	/// NOTE: Not synced between client / server
	/// </summary>
	/// <param name="itemTrait"></param>
	public void AddTrait(ItemTrait toAdd)
	{
		traits.Add(toAdd);
	}

	private void SyncItemName(string newName)
	{
		itemName = newName;
	}

	private void SyncSize(ItemSize newSize)
	{
		size = newSize;
	}

	private void SyncItemDescription(string newDescription)
	{
		itemDescription = newDescription;
	}


	/// <summary>
	/// Removes the trait dynamically
	/// NOTE: Not synced between client / server
	/// </summary>
	/// <param name="itemTrait"></param>
	public void RemoveTrait(ItemTrait toRemove)
	{
		traits.Remove(toRemove);
	}

	private static string GetMasterTypeHandsString(SpriteType masterType)
	{
		switch (masterType)
		{
			case SpriteType.Clothing: return "clothing";

			default: return "items";
		}
	}


	public void OnHoverStart()
	{
		// Show the parenthesis for an item's description only if the item has a description
		UIManager.SetToolTip =
			itemName +
			(String.IsNullOrEmpty(itemDescription) ? "" : $" ({itemDescription})");
	}

	public void OnHoverEnd()
	{
		UIManager.SetToolTip = String.Empty;
	}

	// Sends examine event to all monobehaviors on gameobject
	public void SendExamine()
	{
		SendMessage("OnExamine");
	}

	// When right clicking on an item, examine the item
	public void OnHover()
	{
		if (CommonInput.GetMouseButtonDown(1))
		{
			SendExamine();
		}
	}

	private void OnExamine()
	{
		if (!string.IsNullOrEmpty(initialDescription))
		{
			Chat.AddExamineMsgToClient(initialDescription);
		}
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		return RightClickableResult.Create()
			.AddElement("Examine", OnExamine);
	}


	/// <summary>
	/// Change this item's name and sync it to clients.
	/// </summary>
	/// <param name="newName"></param>
	[Server]
	public void ServerSetItemName(string newName)
	{
		SyncItemName(newName);
	}

	/// <summary>
	/// Change this item's description and sync it to clients.
	/// </summary>
	/// <param name="newName"></param>
	[Server]
	public void ServerSetItemDescription(string desc)
	{
		SyncItemDescription(desc);
	}

	/// <summary>
	/// CHange this item's size and sync it to clients
	/// </summary>
	/// <param name="newSize"></param>
	[Server]
	public void ServerSetSize(ItemSize newSize)
	{
		SyncSize(newSize);
	}

	public int GetExportCost()
	{
		var stackable = GetComponent<Stackable>();

		if (stackable != null)
		{
			return ExportCost * stackable.Amount;
		}

		return ExportCost;
	}
}