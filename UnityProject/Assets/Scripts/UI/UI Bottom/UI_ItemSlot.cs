using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Represents an item slot rendered in the UI.
/// </summary>
[Serializable]
public class UI_ItemSlot : TooltipMonoBehaviour
{

	[SerializeField]
	[FormerlySerializedAs("NamedSlot")]
	[Tooltip("For player inventory, named slot in player's ItemStorage that this UI slot corresponds to.")]
	private NamedSlot namedSlot = NamedSlot.back;
	public NamedSlot NamedSlot => namedSlot;

	[Tooltip("whether this is for the local player's top level inventory or will be instead used" +
	         " for another player's inventory.")]
	[SerializeField]
	private bool forLocalPlayer = false;

	[Tooltip("Name to display when hovering over this slot in the UI")]
	[SerializeField]
	private string hoverName = null;

	[Tooltip("Whether this slot is initially visible in the UI.")]
	[SerializeField]
	private bool initiallyHidden = false;


	/// pointer is over the actual item in the slot due to raycast target. If item ghost, return slot tooltip
	public override string Tooltip => Item == null ? ExitTooltip : Item.GetComponent<ItemAttributesV2>().ArticleName;

	/// set back to the slot name since the pointer is still over the slot background
	public override string ExitTooltip => hoverName;

	/// <summary>
	/// Item in this slot, null if empty.
	/// </summary>
	public Pickupable Item => itemSlot.Item;

	/// <summary>
	/// Actual slot this UI slot is linked to
	/// </summary>
	public ItemSlot ItemSlot => itemSlot;

	/// <summary>
	/// GameObject of the item equipped in this slot, null if not equipped.
	/// (Convenience method for not having to do Item.gameObject)
	/// </summary>
	public GameObject ItemObject => itemSlot.ItemObject;

	/// <summary>
	/// Current image displayed in this slot.
	/// </summary>
	public Image Image => image;

	private bool hidden;
	private ItemSlot itemSlot;
	private Image image;
	private Image secondaryImage;
	private Sprite sprite;
	private Sprite secondarySprite;
	private Text amountText;

	private void Awake()
	{

		amountText = GetComponentInChildren<Text>();
		if (amountText)
		{
			amountText.enabled = false;
		}
		image = GetComponent<Image>();
		image.material = Instantiate(Resources.Load<Material>("Materials/Palettable UI"));
		secondaryImage = GetComponentsInChildren<Image>()[1];
		secondaryImage.alphaHitTestMinimumThreshold = 0.5f;
		secondaryImage.enabled = false;
		image.alphaHitTestMinimumThreshold = 0.5f;
		image.enabled = false;
		hidden = initiallyHidden;

	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
	}

	//Reset Item slot sprite on game restart
	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		sprite = null;
		image.sprite = null;
		image.enabled = false;
	}

	/// <summary>
	/// Link this item slot to its configured named slot on the local player, if this slot is for the local player.
	/// Should only be called after local player is spawned.
	/// </summary>
	public void LinkToLocalPlayer()
	{
		if (namedSlot != NamedSlot.none && forLocalPlayer)
		{
			LinkSlot(ItemSlot.GetNamed(PlayerManager.LocalPlayerScript.ItemStorage, namedSlot));
		}
	}


	/// <summary>
	/// Link this item slot to display the contents of the indicated slot, updating whenever the contents change.
	/// </summary>
	/// <param name="linkedSlot"></param>
	public void LinkSlot(ItemSlot linkedSlot)
	{
		if (itemSlot != null)
		{
			//stop observing this slot
			itemSlot.LinkLocalUISlot(null);
			itemSlot.OnSlotContentsChangeClient.RemoveListener(OnClientSlotContentsChange);
		}
		//start observing the new slot
		itemSlot = linkedSlot;
		if (itemSlot != null)
		{
			itemSlot.LinkLocalUISlot(this);
			itemSlot.OnSlotContentsChangeClient.AddListener(OnClientSlotContentsChange);
		}

		RefreshImage();
	}

	private void OnClientSlotContentsChange()
	{
		//callback for when our item slot's contents change.
		//We update our sprite
		var item = itemSlot.Item;
		if (!item)
		{
			Clear();
			return;
		}

		RefreshImage();
	}

	/// <summary>
	/// Update the image displayed in the slot based on the slots current contents
	/// </summary>
	public void RefreshImage()
	{
		UpdateImage(ItemObject);
	}

	/// <summary>
	/// Update the image that should be displayed in this slot to display the sprite of the specified item.
	///
	/// If hidden, effect will not be visible until this slot is unhidden
	///
	/// </summary>
	/// <param name="item">game object to use to determine what to show in this slot</param>
	/// <param name="color">color tint to apply</param>
	public void UpdateImage(GameObject item = null, Color? color = null)
	{
		var nullItem = item == null;
		var forceColor = color != null;

		if (nullItem && Item != null)
		{ // Case for when we have a hovered image and insert, then stop hovering
			return;
		}

		// If player is cuffed, a special icon appears on his hand slots, exit without changing it.
		if ((namedSlot == NamedSlot.leftHand || namedSlot == NamedSlot.rightHand) &&
			PlayerManager.LocalPlayerScript.playerMove.IsCuffed)
		{
			return;
		}

		if (!nullItem)
		{
			//determine the sprites to display based on the new item
			var spriteRends = item.GetComponentsInChildren<SpriteRenderer>();
			if (image == null)
			{
				image = GetComponent<Image>();
			}

			ItemAttributesV2 itemAttrs = item.GetComponent<ItemAttributesV2>();

			spriteRends = spriteRends.Where(x => x.sprite != null && x != Highlight.instance.spriteRenderer).ToArray();
			sprite = spriteRends[0].sprite;
			image.sprite = sprite;
			image.color = spriteRends[0].color;
			MaterialPropertyBlock pb = new MaterialPropertyBlock();
			spriteRends[0].GetPropertyBlock(pb);
			bool isPaletted = pb.GetInt("_IsPaletted") > 0;
			if (itemAttrs.ItemSprites.InventoryIcon != null && itemAttrs.ItemSprites.IsPaletted)
			{
				image.material.SetInt("_IsPaletted", 1);
				image.material.SetColorArray("_ColorPalette", itemAttrs.ItemSprites.Palette.ToArray());
			}
			else
			{
				image.material.SetInt("_IsPaletted", 0);
			}


			if (spriteRends.Length > 1)
			{
				if (spriteRends[1].sprite != null)
				{
					SetSecondaryImage(spriteRends[1].sprite);
					secondaryImage.color = spriteRends[1].color;
				}
			}

			//determine if we should show an amount
			var stack = item.GetComponent<Stackable>();
			if (stack != null && stack.Amount > 1 && amountText)
			{
				amountText.enabled = true;
				amountText.text = stack.Amount.ToString();
			}
			else if (stack != null && stack.Amount <= 1 && amountText)
			{
				//remove the stack display
				amountText.enabled = false;
			}
		}
		else
		{
			//no object was passed, so clear out the sprites
			Clear();
		}

		if (forceColor)
		{
			image.color = color.GetValueOrDefault(Color.white);
		}

		image.enabled = !nullItem && !hidden;
		image.preserveAspect = !nullItem && !hidden;

		if (secondaryImage)
		{
			if (forceColor)
			{
				secondaryImage.color = color.GetValueOrDefault(Color.white);
			}

			secondaryImage.enabled = secondaryImage.sprite != null && !nullItem && !hidden;
			secondaryImage.preserveAspect = !nullItem && !hidden;
		}
	}

	public void SetSecondaryImage(Sprite sprite)
	{
		secondarySprite = sprite;
		if (secondarySprite != null)
		{
			secondaryImage.sprite = secondarySprite;
			secondaryImage.enabled = !hidden;
			secondaryImage.preserveAspect = true;
		}
		else
		{
			secondaryImage.sprite = null;
			secondaryImage.enabled = false;
		}
	}

	/// <summary>
	/// Clears the displayed image.
	/// </summary>
	public void Clear()
	{
		PlayerScript lps = PlayerManager.LocalPlayerScript;
		if (!lps)
		{
			return;
		}
		sprite = null;
		image.enabled = false;
		secondaryImage.enabled = false;
		image.sprite = null;
		secondarySprite = null;
		secondaryImage.sprite = null;
		if (amountText)
		{
			amountText.enabled = false;
		}


	}

	public void Reset()
	{
		sprite = null;
		image.sprite = null;
		image.enabled = false;
		secondarySprite = null;
		secondaryImage.sprite = null;
		secondaryImage.enabled = false;
		if (amountText)
		{
			amountText.enabled = false;
		}

		ControlTabs.CheckTabClose();
	}


	/// <summary>
	/// Check if item has an interaction with a an item in a slot
	/// If not or if bool returned is true, swap items
	/// </summary>
	public void TryItemInteract()
	{

		var slotName = itemSlot.SlotIdentifier.NamedSlot;
		// Clicked on another slot other than our own hands
		if (itemSlot != UIManager.Hands.LeftHand.ItemSlot && itemSlot != UIManager.Hands.RightHand.itemSlot)
		{
			// If full, attempt to interact the two, otherwise swap
			if (Item != null)
			{
				//check IF2 InventoryApply interaction - combine the active hand item with this (only if
				//both are occupied)
				if (TryIF2InventoryApply()) return;

				UIManager.Hands.SwapItem(this);
				return;
			}
			else
			{
				UIManager.Hands.SwapItem(this);
				return;
			}
		}
		// If there is an item and the hand is interacting in the same slot
		if (Item != null && UIManager.Hands.CurrentSlot.ItemSlot == itemSlot)
		{
			//check IF2 logic first
			var interactables = Item.GetComponents<IBaseInteractable<HandActivate>>()
				.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
			var activate = HandActivate.ByLocalPlayer();
			InteractionUtils.ClientCheckAndTrigger(interactables, activate);
		}
		else
		{
			if (UIManager.Hands.CurrentSlot.ItemSlot != itemSlot)
			{
				//Clicked on item with otherslot selected
				if (UIManager.Hands.OtherSlot.Item != null)
				{
					if (TryIF2InventoryApply()) return;
					UIManager.Hands.SwapItem(this);
				}
			}
		}
	}


	private bool TryIF2InventoryApply()
	{
		//check IF2 InventoryApply interaction - apply the active hand item with this (only if
		//target slot is occupied, but it's okay if active hand slot is not occupied)
		if (Item != null)
		{
			var combine = InventoryApply.ByLocalPlayer(itemSlot, UIManager.Hands.CurrentSlot.itemSlot);
			//check interactables in the active hand (if active hand occupied)
			if (UIManager.Hands.CurrentSlot.Item != null)
			{
				var handInteractables = UIManager.Hands.CurrentSlot.Item.GetComponents<IBaseInteractable<InventoryApply>>()
					.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
				if (InteractionUtils.ClientCheckAndTrigger(handInteractables, combine) != null) return true;
			}

			//check interactables in the target
			var targetInteractables = Item.GetComponents<IBaseInteractable<InventoryApply>>()
				.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
			if (InteractionUtils.ClientCheckAndTrigger(targetInteractables, combine) != null) return true;
		}

		return false;
	}


	[ContextMenu("Debug Slot")]
	void DebugItem()
	{
		Logger.Log(itemSlot.ToString(), Category.Inventory);
	}

	/// <summary>
	/// Sets whether this should be shown / hidden (but the set sprites will still be remembered when it is unhidden)
	/// </summary>
	/// <param name="hidden"></param>
	public void SetHidden(bool hidden)
	{
		this.hidden = hidden;
		image.sprite = sprite;
		image.enabled = sprite != null && !hidden;
		image.preserveAspect = sprite != null && !hidden;
		if (hidden && amountText)
		{
			amountText.enabled = false;
		}
		else if (!hidden && amountText)
		{
			//show if we have something stackable.
			if (itemSlot?.ItemObject != null)
			{
				var stack = itemSlot.ItemObject.GetComponent<Stackable>();
				if (stack != null && stack.Amount > 1)
				{
					amountText.enabled = true;
				}
			}
		}


		if (secondaryImage)
		{
			secondaryImage.sprite = secondarySprite;
			secondaryImage.enabled = secondarySprite != null && !hidden;
			secondaryImage.preserveAspect = secondarySprite != null && !hidden;
		}
	}
}