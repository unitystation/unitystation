using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Logs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using US13.Clothing.BackPack;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items;
using US13.Items.Implants.Organs;
using US13.Managers;
using US13.Messages.Client;
using US13.Objects;
using US13.Player;
using US13.ScriptableObjects;
using US13.Systems.Inventory;
using US13.UI.Core;
using Util;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	/// <summary>
	/// Represents an item slot rendered in the UI.
	/// </summary>
	[Serializable]
	public class UI_ItemSlot : TooltipMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		//private static List<(GameObject, UI_ItemSlot)> CurrentlyOpenItems = new  List<(GameObject, UI_ItemSlot)>();

		private GameObject Previewingitem;
		private bool IsCanFitPreview;

		[SerializeField]
		[FormerlySerializedAs("NamedSlot")]
		[Tooltip("For player inventory, named slot in player's ItemStorage that this UI slot corresponds to.")]
		protected NamedSlot namedSlot = NamedSlot.back;

		public NamedSlot NamedSlot => namedSlot;

		[Tooltip("whether this is for the local player's top level inventory or will be instead used" +
		         " for another player's inventory.")]
		[SerializeField]
		protected bool forLocalPlayer = false;

		[Tooltip("Name to display when hovering over this slot in the UI")] [SerializeField]
		protected string hoverName = null;

		[Tooltip("Whether this slot is initially visible in the UI.")] [SerializeField]
		protected bool initiallyHidden = false;

		[Tooltip("Placeholder image that will be disabled when there is an item in slot")] [SerializeField]
		protected Image placeholderImage = null;

		[Tooltip("From where the item slot is linked from")]
		public ItemStorageLinkOrigin ItemStorageLinkOrigin = ItemStorageLinkOrigin.localPlayer;

		/// pointer is over the actual item in the slot due to raycast target. If item ghost, return slot tooltip
		public override string Tooltip => Item == null ? ExitTooltip : Item.GetComponent<ItemAttributesV2>().ArticleName;

		/// set back to the slot name since the pointer is still over the slot background
		public override string ExitTooltip => hoverName;

		/// <summary>
		/// Item in this slot, null if empty.
		/// </summary>
		public Pickupable Item => itemSlot?.Item;

		/// <summary>
		/// Actual slot this UI slot is linked to
		/// </summary>
		public ItemSlot ItemSlot => itemSlot;

		/// <summary>
		/// GameObject of the item equipped in this slot, null if not equipped.
		/// (Convenience method for not having to do Item.gameObject)
		/// </summary>
		public GameObject ItemObject => itemSlot.ItemObject;

		public UI_ItemImage Image => image;

		private bool hidden;
		private UI_ItemImage image;
		private ItemSlot itemSlot;
		public Text amountText;

		public Image MoreInventoryImage;
		public HasSubInventory HasSubInventory;

		public bool IsAdmins = false;

		private void Awake()
		{
			if (amountText)
			{
				amountText.enabled = false;
			}

			if (MoreInventoryImage)
			{
				MoreInventoryImage.enabled = false;
			}


			hidden = initiallyHidden;
		}

		/// <summary>
		/// Link this item slot to its configured named slot on the local player, if this slot is for the local player.
		/// Should only be called after local player is spawned.
		/// </summary>
		public void LinkToLocalPlayer()
		{
			if (namedSlot != NamedSlot.none && forLocalPlayer)
			{
				var linkedSlot = ItemSlot.GetNamed(GetItemStorage(), namedSlot);
				if (linkedSlot != null)
				{
					LinkSlot(linkedSlot);
				}
			}
		}

		private ItemStorage GetItemStorage()
		{
			if (ItemStorageLinkOrigin == ItemStorageLinkOrigin.localPlayer)
			{
				return null;
			}
			else
			{
				return AdminManager.Instance.LocalAdminGhostStorage;
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

				if (itemSlot.NamedSlot != null)
				{
					namedSlot = itemSlot.NamedSlot.Value;
				}
				else
				{
					namedSlot = NamedSlot.none;
				}

				SetPlaceholder();
			}

			RefreshImage();
		}


		public void SetPlaceholder()
		{
			if (placeholderImage == null) return;
			switch (namedSlot)
			{
				case NamedSlot.outerwear:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.outerwear.GetFirstSprite;
					break;
				case NamedSlot.belt:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.belt.GetFirstSprite;
					break;
				case NamedSlot.head:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.head.GetFirstSprite;
					break;
				case NamedSlot.feet:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.feet.GetFirstSprite;
					break;
				case NamedSlot.mask:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.mask.GetFirstSprite;
					break;
				case NamedSlot.uniform:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.uniform.GetFirstSprite;
					break;
				case NamedSlot.leftHand:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.leftHand.GetFirstSprite;
					break;
				case NamedSlot.rightHand:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.rightHand.GetFirstSprite;
					break;
				case NamedSlot.eyes:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.eyes.GetFirstSprite;
					break;
				case NamedSlot.back:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.back.GetFirstSprite;
					break;
				case NamedSlot.hands:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.hands.GetFirstSprite;
					break;
				case NamedSlot.ear:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.ear.GetFirstSprite;
					break;
				case NamedSlot.neck:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.neck.GetFirstSprite;
					break;
				case NamedSlot.handcuffs:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.handcuffs.GetFirstSprite;
					break;
				case NamedSlot.id:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.id.GetFirstSprite;
					break;
				case NamedSlot.suitStorage:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.suitStorage.GetFirstSprite;
					break;

				// Storage items remain as before (using InventoryPocket, assumed)
				case NamedSlot.storage01:
				case NamedSlot.storage02:
				case NamedSlot.storage03:
				case NamedSlot.storage04:
				case NamedSlot.storage05:
				case NamedSlot.storage06:
				case NamedSlot.storage07:
				case NamedSlot.storage08:
				case NamedSlot.storage09:
				case NamedSlot.storage10:
				case NamedSlot.storage11:
				case NamedSlot.storage12:
				case NamedSlot.storage13:
				case NamedSlot.storage14:
				case NamedSlot.storage15:
				case NamedSlot.storage16:
				case NamedSlot.storage17:
				case NamedSlot.storage18:
				case NamedSlot.storage19:
				case NamedSlot.storage20:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.InventoryPocket.GetFirstSprite;
					break;
				default:
					placeholderImage.sprite = CommonSpriteDataSOs.Instance.bob.GetFirstSprite;
					break;
			}
		}

		/// <summary>
		///  any relation to any slot on client
		/// </summary>
		/// <param name="linkedSlot"></param>
		public void UnLinkSlot()
		{
			if (itemSlot != null)
			{
				//stop observing this slot
				itemSlot.LinkLocalUISlot(null);
				itemSlot.OnSlotContentsChangeClient.RemoveListener(OnClientSlotContentsChange);
				itemSlot = null;
				Reset();
			}
		}

		public void SetUp(BodyPartUISlots.StorageCharacteristics storageCharacteristics)
		{
			if (placeholderImage != null) placeholderImage.sprite = storageCharacteristics.placeholderSprite;
			namedSlot = storageCharacteristics.namedSlot;
			hoverName = storageCharacteristics.hoverName;
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
		public void RefreshImage(UI_ItemImage ToUse = null)
		{
			try
			{
				if (itemSlot != null)
					UpdateImage(ItemObject,  ToUse : ToUse);
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
			}
		}

		/// <summary>
		/// Update the image that should be displayed in this slot to display the sprite of the specified item.
		///
		/// If hidden, effect will not be visible until this slot is unhidden
		///
		/// </summary>
		/// <param name="item">game object to use to determine what to show in this slot</param>
		/// <param name="colour">color tint to apply</param>
		public void UpdateImage(GameObject item = null, Color? colour = null, bool CanFitPreview = false, bool SkipMoveAnimation = true, UI_ItemImage ToUse = null)
		{
			bool ClearSubIcons = false;
			if (item != null)
			{
				//determine if we should show an amount
				var stack = item.GetComponent<Stackable>();
				if (stack != null && ((stack.Amount > 1 && amountText) || stack.IsRepresentationOfStack))
				{
					amountText.enabled = true;
					amountText.text = stack.Amount.ToString();
				}
				else if (stack != null && stack.Amount <= 1 && amountText)
				{
					//remove the stack display
					amountText.enabled = false;
				}

				if (MoreInventoryImage != null)
				{
					var Storage = item.GetComponent<InteractableStorage>();
					var canister = item.GetComponent<GasContainer>();
					if (Storage != null && Storage.DoNotShowInventoryOnUI == false)
					{
						HasSubInventory.itemStorage = Storage.ItemStorage;
						MoreInventoryImage.enabled = true;
					}
					else if (canister != null && canister.IgnoreInternals == false)
					{
						MoreInventoryImage.enabled = true;
					}
					else
					{
						HasSubInventory.itemStorage = null;
						MoreInventoryImage.enabled = false;
					}
				}
			}
			else
			{
				ClearSubIcons = true;
			}

			if (Previewingitem == item && IsCanFitPreview == false) return;

			bool nullItem = item == null;

			if (nullItem && Item != null)
			{
				// Case for when we have a hovered image and insert, then stop hovering
				return;
			}

			// If player is cuffed, a special icon appears on his hand slots, exit without changing it.
			if ((namedSlot == NamedSlot.leftHand || namedSlot == NamedSlot.rightHand) &&
			    PlayerManager.LocalPlayerScript.playerMove.IsCuffed)
			{
				return;
			}

			Clear(true, ClearSubIcons);

			Previewingitem = item;
			IsCanFitPreview = CanFitPreview;

			if (!nullItem)
			{
				//var hasEntry = CurrentlyOpenItems.Any(x => x.Item1 == Previewingitem);

				//if (hasEntry)
				//{
					//image = UI_ItemImage.RequestItemImage(gameObject, item,CanFitPreview, colour, true);
					if (ToUse == null)
					{
						image = UI_ItemImage.RequestItemImage(gameObject, item,CanFitPreview, colour, SkipMoveAnimation);
					}
					else
					{
						image = ToUse;
						image.SetParent(gameObject, false);
					}


				//CurrentlyOpenItems.Add((item, this));


				if (placeholderImage)
					placeholderImage.color = new Color(1, 1, 1, 0);


			}
			else
			{
				//no object was passed, so clear out the sprites
				Clear();
			}
		}

		/// <summary>
		/// Clears the displayed image.
		/// </summary>
		public void Clear(bool ClearOnlyPreviews = false, bool ClearSubIcons = true)
		{
			PlayerScript lps = PlayerManager.LocalPlayerScript;
			if (!lps)
			{
				return;
			}

			Previewingitem = null;
			IsCanFitPreview = false;
			//var Entry = CurrentlyOpenItems.Find(x => x.Item1 == Previewingitem && x.Item2 == this);
			//CurrentlyOpenItems.Remove(Entry);

			image?.ClearAll( this.gameObject, ClearOnlyPreviews: ClearOnlyPreviews);
			image = null;



			if (ClearSubIcons)
			{
				if (amountText)
				{
					amountText.enabled = false;
				}

				if (placeholderImage)
				{
					placeholderImage.color = Color.white;
				}

				if (HasSubInventory)
				{
					HasSubInventory.itemStorage = null;
				}

				if (MoreInventoryImage)
				{
					MoreInventoryImage.enabled = false;
				}
			}
		}

		public void OnDestroy()
		{
			image?.ClearAll( this.gameObject, true);
			image = null;
		}

		public void Reset()
		{
			Previewingitem = null;
			IsCanFitPreview = false;

			image?.ClearAll(this.gameObject);
			image = null;
			if (amountText)
			{
				amountText.enabled = false;
			}

			if (placeholderImage)
			{
				placeholderImage.color = Color.white;
			}

			if (MoreInventoryImage)
			{
				HasSubInventory.itemStorage = null;
				MoreInventoryImage.enabled = false;
			}

			ControlTabs.CheckTabClose();
		}

		private bool isValidPlayer()
		{
			if (PlayerManager.LocalPlayerScript == null) return false;

			// TODO tidy up this if statement once it's working correctly
			if (!PlayerManager.LocalPlayerScript.playerMove.AllowInput ||
			    PlayerManager.LocalPlayerScript.IsGhost)
			{
				Loggy.Info("Invalid player, cannot perform action!", Category.Interaction);
				return false;
			}

			return true;
		}

		public  bool SwapItem(UI_ItemSlot itemSlot)
		{
			if (itemSlot.IsAdmins || isValidPlayer())
			{
				var CurrentSlot = PlayerManager.LocalPlayerScript?.DynamicItemStorage?.GetActiveHandSlot();
				if (itemSlot.IsAdmins && PlayerManager.LocalMindScript.isGhosting)
				{
					CurrentSlot = AdminManager.Instance.LocalAdminGhostStorage.GetNamedItemSlot(NamedSlot.ghostStorage01);
				}

				if (CurrentSlot != itemSlot.itemSlot) //Check if we're not interacting with our own hand
				{
					if (CurrentSlot.Item == null) //check if hand is empty
					{
						if (itemSlot.Item != null) //check if slot is not empty
						{
							if (itemSlot.IsAdmins)
							{
								//if slot is not empty and hand is empty; ask the inventory to give us that item in our hand
								AdminInventoryTransferMessage.Send(itemSlot.ItemSlot, CurrentSlot);
								return true;
							}
							else
							{
								//if slot is not empty and hand is empty; ask the inventory to give us that item in our hand
								Inventory.ClientRequestTransfer(itemSlot.ItemSlot, CurrentSlot);
								return true;
							}

						}
					}
					else
					{
						if (itemSlot.Item != null) return false;

						if (itemSlot.IsAdmins)
						{
							//if slot is empty, ask the game to put whatever thats in out hand in it.
							AdminInventoryTransferMessage.Send(CurrentSlot, itemSlot.ItemSlot);
							return true;
						}
						else
						{
							//if slot is empty, ask the game to put whatever thats in out hand in it.
							Inventory.ClientRequestTransfer(CurrentSlot, itemSlot.ItemSlot);
							return true;
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Check if item has an interaction with a an item in a slot
		/// If not or if bool returned is true, swap items
		/// </summary>
		public void TryItemInteract(bool swapIfEmpty = true)
		{
			// Clicked on another slot other than our own hands
			bool IsHandSlots = false;
			var HandSlot = PlayerManager.LocalPlayerScript?.DynamicItemStorage?.GetActiveHandSlot();
			IsHandSlots =  HandSlot == itemSlot;

			if (IsHandSlots == false && HandSlot != null)
			{
				// If full, attempt to interact the two, otherwise swap
				if (Item != null)
				{
					//check IF2 InventoryApply interaction - combine the active hand item with this (only if
					//both are occupied)
					if (TryIF2InventoryApply()) return;

					if (swapIfEmpty && HandSlot.ItemNotRemovable == false && itemSlot.ItemNotRemovable == false)
						SwapItem(this);
					return;
				}
				else
				{
					if (swapIfEmpty && HandSlot.ItemNotRemovable == false && itemSlot.ItemNotRemovable == false)
						SwapItem(this);
					return;
				}
			}

			// If there is an item and the hand is interacting in the same slot
			if (Item != null && HandSlot == itemSlot)
			{
				//check IF2 logic first
				var interactables = Item.GetComponents<IBaseInteractable<HandActivate>>()
					.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
				var activate = HandActivate.ByLocalPlayer();
				InteractionUtils.ClientCheckAndTrigger(interactables, activate);
			}
			else
			{
				if (HandSlot != itemSlot)
				{
					if (HandSlot != null)
					{
						if (TryIF2InventoryApply()) return;
					}

					if (swapIfEmpty && HandSlot?.ItemNotRemovable is not true)
						SwapItem(this);
				}
			}
		}


		private bool TryIF2InventoryApply()
		{
			//check IF2 InventoryApply interaction - apply the active hand item with this (only if
			//target slot is occupied, but it's okay if active hand slot is not occupied)
			if (Item != null)
			{
				var combine = InventoryApply.ByLocalPlayer(itemSlot,
					PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot());
				//check interactables in the active hand (if active hand occupied)
				if (PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().Item != null)
				{
					if (combine.IsAltClick && SwapTwoItemsInInventory(combine.FromSlot)) return true;
					var handInteractables = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().Item
						.GetComponents<IBaseInteractable<InventoryApply>>()
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

		private bool SwapTwoItemsInInventory(ItemSlot CurrentSlot)
		{
			if (PlayerManager.LocalPlayerScript.PlayerNetworkActions == null) return false;
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdServerReplaceItemInInventory(CurrentSlot.ItemObject,
				itemSlot.ItemStorageNetID, itemSlot.NamedSlot.Value);
			return true;
		}


		[ContextMenu("Debug Slot")]
		void DebugItem()
		{
			Loggy.Info(itemSlot.ToString(), Category.PlayerInventory);
		}

		/// <summary>
		/// Sets whether this should be shown / hidden (but the set sprites will still be remembered when it is unhidden)
		/// </summary>
		/// <param name="hidden"></param>
		public void SetHidden(bool hidden)
		{
			this.hidden = hidden;
			image.SetHidden(hidden);
			if (hidden && amountText)
			{
				amountText.enabled = false;
			}
			else if (!hidden)
			{
				//show if we have something stackable.
				if (itemSlot?.ItemObject != null)
				{
					if (amountText)
					{
						var stack = itemSlot.ItemObject.GetComponent<Stackable>();
						if (stack != null && stack.Amount > 1)
						{
							amountText.enabled = true;
						}
					}

					if (MoreInventoryImage != null)
					{
						var Storage = itemSlot.ItemObject.GetComponent<InteractableStorage>();
						if (Storage != null)
						{
							HasSubInventory.itemStorage = Storage.ItemStorage;
							MoreInventoryImage.enabled = true;
						}
						else
						{
							HasSubInventory.itemStorage = null;
							MoreInventoryImage.enabled = false;
						}
					}
				}

				if (Item && placeholderImage)
				{
					placeholderImage.color = new Color(1, 1, 1, 0);
				}
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (ItemObject == null) return;
			UIManager.SetHoverToolTip = ItemObject;
			//thanks stack overflow!
			Regex r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
			UIManager.SetToolTip = r.Replace(ItemObject.ExpensiveName(), " ");
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			UIManager.SetToolTip = "";
			UIManager.SetHoverToolTip = null;
		}
	}

	public enum ItemStorageLinkOrigin
	{
		localPlayer = 0,
		adminGhost = 1,
	}
}