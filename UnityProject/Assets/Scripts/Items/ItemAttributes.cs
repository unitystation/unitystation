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

[RequireComponent(typeof(SpriteDataHandler))]
[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(ObjectBehaviour))]
[RequireComponent(typeof(RegisterItem))]
[RequireComponent(typeof(CustomNetTransform))]
public class ItemAttributes : NetworkBehaviour, IRightClickable
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

	public ItemType itemType = ItemType.None;
	public ItemSize size;
	public SpriteType spriteType;

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
		SyncItemName(itemName);
	}

	public void GoingOnStageServer(OnStageInfo info)
	{
		SyncItemName(itemName);
	}

	private void OnEnable()
	{
		spriteDataHandler = GetComponentInChildren<SpriteDataHandler>();
		InventoryIcon = GetComponentInChildren<SpriteHandler>();
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
		itemType = ItemAttributes.itemType;
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