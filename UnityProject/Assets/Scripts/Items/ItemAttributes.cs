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
/// </summary>
[RequireComponent(typeof(SpriteDataHandler))]
[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(ObjectBehaviour))]
[RequireComponent(typeof(RegisterItem))]
[RequireComponent(typeof(CustomNetTransform))]
public class ItemAttributes : NetworkBehaviour, IRightClickable, IServerSpawn
{
	/// <summary>
	/// Remember in hands is Left then right so [0] = Left, [1] = right
	/// </summary>
	[FormerlySerializedAs("spriteHandlerData")]
	public SpriteDataHandler spriteDataHandler;

	public SpriteHandler InventoryIcon;


	[SyncVar(hook = nameof(SyncItemName))]
	public string itemName;
	public string itemDescription;

	/// <summary>
	/// Only used for config in editor. To dynamically change this, please use
	/// AddTrait or RemoveTrait.
	/// Don't use this for checking if an item has a trait. Use HasTrait instead.
	/// </summary>
	[Tooltip("Traits of this item.")]
	public List<ItemTrait> InitialTraits;
	public ItemSize size;
	public SpriteType spriteType;

	/// <summary>
	/// Actual current traits, accounting for dynamic add / remove.
	/// </summary>
	private HashSet<ItemTrait> traits = new HashSet<ItemTrait>();

	/// <summary>
	/// True if this is a mask that can connect to a tank
	/// </summary>
	[FormerlySerializedAs("ConnectedToTank")]
	public bool CanConnectToTank;

	/// throw-related fields
	[Tooltip("Damage when we click someone with harm intent")] [Range(0, 100)]
	public float hitDamage = 0;

	public DamageType damageType = DamageType.Brute;

	[Tooltip("How painful it is when someone throws it at you")] [Range(0, 100)]
	public float throwDamage = 0;

	[Tooltip("How many tiles to move per 0.1s when being thrown")]
	public float throwSpeed = 2;

	[Tooltip("Max throw distance")] public float throwRange = 7;

	[Tooltip("Sound to be played when we click someone with harm intent")]
	public string hitSound = "GenericHit";

	///<Summary>
	/// Can this item protect humans against spess?
	///</Summary>
	public bool IsEVACapable { get; private set; }

	public List<string> attackVerb = new List<string>();

	public void SetItemName(string newName)
	{
		SyncItemName(newName);
	}

	private void SyncItemName(string newName)
	{
		itemName = newName;
	}

	public override void OnStartClient()
	{
		SyncItemName(itemName);
		base.OnStartClient();
	}

	public override void OnStartServer()
	{
		SyncItemName(itemName);
		base.OnStartServer();
	}

	private void Awake()
	{
		foreach (var definedTrait in InitialTraits)
		{
			traits.Add(definedTrait);
		}
		SyncItemName(itemName);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncItemName(itemName);
	}

	private void OnEnable()
	{
		spriteDataHandler = GetComponentInChildren<SpriteDataHandler>();
		InventoryIcon = GetComponentInChildren<SpriteHandler>();
	}

	/// <summary>
	/// Returns true iff this itemattributes has the specified trait
	/// </summary>
	/// <param name="toCheck"></param>
	/// <returns></returns>
	public bool HasTrait(ItemTrait toCheck)
	{
		return traits.Contains(toCheck);
	}

	/// <summary>
	/// Dynamically adds the specified trait to this item attributes
	/// </summary>
	/// <param name="toAdd"></param>
	public void AddTrait(ItemTrait toAdd)
	{
		traits.Add(toAdd);
	}

	/// <summary>
	/// Dynamically removes the specified trait from this item attributes
	/// </summary>
	/// <param name="toAdd"></param>
	public void RemoveTrait(ItemTrait toRemove)
	{
		traits.Remove(toRemove);
	}

	public void SetUpFromClothingData(EquippedData equippedData, ItemAttributesData itemAttributes)
	{
		spriteDataHandler.Infos = new SpriteData();
		spriteDataHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(equippedData.InHandsLeft));
		spriteDataHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(equippedData.InHandsRight));
		InventoryIcon.Infos = new SpriteData();
		InventoryIcon.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(equippedData.ItemIcon));
		InventoryIcon.PushTexture();
		AttributesFromCD(itemAttributes);
	}

	public void AttributesFromCD(ItemAttributesData ItemAttributes)
	{
		SyncItemName(ItemAttributes.itemName);
		itemDescription = ItemAttributes.itemDescription;
		var trait = TypeToTrait(ItemAttributes.itemType);
		if (trait != null)
		{
			traits.Add(trait);
		}

		size = ItemAttributes.size;
		spriteType = ItemAttributes.spriteType;
		CanConnectToTank = ItemAttributes.CanConnectToTank;
		hitDamage = ItemAttributes.hitDamage;
		damageType = ItemAttributes.damageType;
		throwDamage = ItemAttributes.throwDamage;
		throwSpeed = ItemAttributes.throwSpeed;
		throwRange = ItemAttributes.throwRange;
		hitSound = ItemAttributes.hitSound;
		attackVerb = ItemAttributes.attackVerb;
		IsEVACapable = ItemAttributes.IsEVACapable;
	}
	private ItemTrait TypeToTrait(ItemType itemType)
	{
		return ItemTypeToTraitMapping.Instance.GetTrait(itemType);
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
		if (!string.IsNullOrEmpty(itemDescription))
		{
			Chat.AddExamineMsgToClient(itemDescription);
		}
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		return RightClickableResult.Create()
			.AddElement("Examine", OnExamine);
	}
}