
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
[RequireComponent(typeof(SpriteDataHandler))]
[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(ObjectBehaviour))]
[RequireComponent(typeof(RegisterItem))]
[RequireComponent(typeof(CustomNetTransform))]
public class ItemAttributesV2 : NetworkBehaviour, IRightClickable, IServerSpawn, IItemAttributes
{

	[Tooltip("Display name of this item when spawned.")]
	[SerializeField]
	private string initialName;
	/// <summary>
	/// Current name
	/// </summary>
	[SyncVar(hook=nameof(SyncItemName))]
	private string itemName;

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

	[Tooltip("Damage when we click someone with harm intent")]
	[Range(0, 100)]
	[SerializeField]
	private float hitDamage = 0;

	[Tooltip("Type of damage done when this is thrown or used for melee.")]
	[SerializeField]
	private DamageType damageType = DamageType.Brute;

	[Tooltip("How painful it is when someone throws it at you")]
	[Range(0, 100)]
	[SerializeField]
	private float throwDamage = 0;

	[Tooltip("How many tiles to move per 0.1s when being thrown")]
	[SerializeField]
	private float throwSpeed = 2;

	[Tooltip("Max throw distance")]
	[SerializeField]
	private float throwRange = 7;

	[Tooltip("Sound to be played when we click someone with harm intent")]
	[SerializeField]
	private string hitSound = "GenericHit";

	//TODO: These fields should probably be migrated to a different component as they are very specific to clothing, particularly
	//suits and masks. Probably belong in the Clothing component.
	[Tooltip("Is this a mask that can connect to a tank?")]
	[SerializeField]
	private bool canConnectToTank;

	[Tooltip("Can this item protect humans against spess?")]
	[SerializeField]
	private bool isEVACapable;

	[Tooltip("Possible verbs used to describe the attack when this is used for melee.")]
	[SerializeField]
	private List<string> attackVerbs;

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

#if UNITY_EDITOR
	public void AttributesFromCD(ItemAttributesData ItemAttributes)
	{
		initialName = ItemAttributes.itemName;
		initialDescription = ItemAttributes.itemDescription;
		var trait = TypeToTrait(ItemAttributes.itemType);
		if (trait != null)
		{
			traits.Add(trait);
		}
		initialSize = ItemAttributes.size;
		canConnectToTank = ItemAttributes.CanConnectToTank;
		hitDamage = ItemAttributes.hitDamage;
		damageType = ItemAttributes.damageType;
		throwDamage = ItemAttributes.throwDamage;
		throwSpeed = ItemAttributes.throwSpeed;
		throwRange = ItemAttributes.throwRange;
		hitSound = ItemAttributes.hitSound;
		attackVerbs = ItemAttributes.attackVerb;
		isEVACapable = ItemAttributes.IsEVACapable;
	}
	private ItemTrait TypeToTrait(ItemType itemType)
	{
		return ItemTypeToTraitMapping.Instance.GetTrait(itemType);
	}
#endif

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

	public string ItemName => name;

	public float ServerHitDamage
	{
		get => hitDamage;
		set => hitDamage = value;
	}

	public DamageType ServerDamageType
	{
		get => damageType;
		set => damageType = value;
	}

	public bool CanConnectToTank { get; }

	public void ServerSetItemName(string newName)
	{
		SyncItemName(newName);
	}


	[Server]
	public void ServerSetItemDescription(string desc)
	{
		SyncItemDescription(desc);
	}

	/// <summary>
	/// NOTE: Not synced between client / server
	/// </summary>
	public ItemSize Size => size;

	[Server]
	public void ServerSetSize(ItemSize newSize)
	{
		SyncSize(newSize);
	}

	public float ServerThrowSpeed
	{
		get => throwSpeed;
		set => throwSpeed = value;
	}
	public float ServerThrowRange
	{
		get => throwRange;
		set => throwRange = value;
	}
	public float ServerThrowDamage
	{
		get => throwDamage;
		set => throwDamage = value;
	}
	public IEnumerable<string> ServerAttackVerbs
	{
		get => attackVerbs;
		set => attackVerbs = new List<string>(value);
	}

	public bool IsEVACapable => isEVACapable;

	public string ServerHitSound
	{
		get => hitSound;
		set => hitSound = value;
	}
}