using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PanelHudBottomController : MonoBehaviour
{
	public UI_ItemSlot backpackItemSlot = default;
	public UI_ItemSlot PDAItemSlot = default;
	public UI_ItemSlot beltItemSlot = default;
	public UI_ItemSlot pocketOneItemSlot = default;
	public UI_ItemSlot pocketTwoItemSlot = default;
	[FormerlySerializedAs("pocketThreeItemSlot")] public UI_ItemSlot suitStorageSlot = default;

	private bool _isWearingUniform;

	private ItemSlot OXsuit;
	private ItemSlot uniform;
	/// <summary>
	/// Do player have item in uniform slot
	/// </summary>
	public bool IsWearingUniform
	{
		get => _isWearingUniform;
		set
		{
			_isWearingUniform = value;
			if (_isWearingUniform)
			{
				// restore default settings
				pocketTwoImage.color = Color.white;
				pocketOneImage.color = Color.white;

				pocketTwoImage.raycastTarget = true;
				pocketOneImage.raycastTarget = true;
				pocketTwoItemSlot.ItemSlot.IsEnabled = true;
				pocketOneItemSlot.ItemSlot.IsEnabled = true;
			}
			else
			{
				// player cannot use slot 2 and 3 without uniform

				// change pocket image color to gray
				pocketTwoImage.color = greyedPocketColor;
				pocketOneImage.color = greyedPocketColor;

				// drop items from pockets
				DropItem(pocketTwoItemSlot);
				DropItem(pocketOneItemSlot);

				// disable raycastTarget so player cannot put items back
				pocketTwoImage.raycastTarget = false;
				pocketOneImage.raycastTarget = false;
				pocketTwoItemSlot.ItemSlot.IsEnabled = false;
				pocketOneItemSlot.ItemSlot.IsEnabled = false;
			}
		}
	}

	private bool _IsWearingXOSuit;

	public bool IsWearingXOSuit
	{
		get => _IsWearingXOSuit;
		set
		{
			_IsWearingXOSuit = value;
			if (_IsWearingXOSuit)
			{
				// restore default settings
				SuitStorageImage.color = Color.white;

				SuitStorageImage.raycastTarget = true;
				suitStorageSlot.ItemSlot.IsEnabled = true;
			}
			else
			{
				// player cannot use suitStorage without suit

				// change pocket image color to gray
				SuitStorageImage.color = greyedPocketColor;

				// drop items from suitStorage
				DropItem(suitStorageSlot);

				// disable raycastTarget so player cannot put items back
				SuitStorageImage.raycastTarget = false;
				suitStorageSlot.ItemSlot.IsEnabled = false;
			}
		}
	}

	[Header("UI GameObject references")]
	[SerializeField] private Text backpackKeybindText = default;
	[SerializeField] private Text PDAKeybindText = default;
	[SerializeField] private Text beltKeybindText = default;
	[SerializeField] private Text pocketOneKeybindText = default;
	[SerializeField] private Text pocketTwoKeybindText = default;
	[SerializeField] private Text pocketThreeKeybindText = default;
	[SerializeField] private Image pocketTwoImage = default;
	[SerializeField] private Image pocketOneImage = default;
	[SerializeField] private Image SuitStorageImage = default;
	[SerializeField] private Color greyedPocketColor = Color.gray;

	[Header("Message settings")]
	[SerializeField] private string emptyHandNPocketMessage = "There's nothing in that pocket";
	[SerializeField] private string fullHandNPocketMessage = "My pockets are full";

	#region  /=== KEYBINDS ===\

	public void SetBackPackKeybindText(string key)
	{
		backpackKeybindText.text = key;
	}

	public void SetPDAKeybindText(string key)
	{
		PDAKeybindText.text = key;
	}

	public void SetBeltKeybindText(string key)
	{
		beltKeybindText.text = key;
	}

	public void SetPocketOneKeybindText(string key)
	{
		pocketOneKeybindText.text = key;
	}

	public void SetPocketTwoKeybindText(string key)
	{
		pocketTwoKeybindText.text = key;
	}

	public void SetPocketThreeKeybindText(string key)
	{
		pocketThreeKeybindText.text = key;
	}

	#endregion

	#region  /=== EVENT LISTENERS ===\

	private void OnDisable()
	{
		if (PlayerManager.LocalPlayerScript != null)
		{
			RemoveListeners();
		}
	}

	/// <summary>
	/// Setup event listeners
	/// </summary>
	public void SetupListeners()
	{
		uniform = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.uniform);
		uniform.OnSlotContentsChangeClient.AddListener(OnUniformSlotUpdate);
		OnUniformSlotUpdate();

		OXsuit = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.outerwear);
		OXsuit.OnSlotContentsChangeClient.AddListener(OnOXsuitSlotUpdate);
		OnOXsuitSlotUpdate();
	}

	/// <summary>
	/// Remove event listeners
	/// </summary>
	public void RemoveListeners()
	{
		if (uniform != null)
		{
			uniform.OnSlotContentsChangeClient.RemoveListener(OnUniformSlotUpdate);
		}
		if (OXsuit != null)
		{
			OXsuit.OnSlotContentsChangeClient.RemoveListener(OnOXsuitSlotUpdate);
		}
	}

	/// <summary>
	/// Called when uniform slot content has changed
	/// </summary>
	private void OnUniformSlotUpdate()
	{
		ItemSlot uniform = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.uniform);
		IsWearingUniform = uniform.IsOccupied;
	}

	/// <summary>
	/// Called when OXsuit slot content has changed
	/// </summary>
	private void OnOXsuitSlotUpdate()
	{
		ItemSlot OXsuit = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.outerwear);
		IsWearingXOSuit = OXsuit.IsOccupied;
	}

	private void DropItem(UI_ItemSlot itemSlot)
	{
		if (itemSlot.ItemSlot.IsEmpty || PlayerManager.LocalPlayerScript.IsGhost)
			return;

		Logger.Log("Drop pocket item - uniform is null", Category.PlayerInventory);

		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdDropItemWithoutValidations(itemSlot.NamedSlot);
	}

	#endregion

	/// <summary>
	/// Try to interact with pocket slot/item (called only when keybind is pressed).
	/// </summary>
	/// <param name="slot">pocket you want to iteract with (value range: 1 to 3)</param>
	public void TryInteractWithPocket(int slot)
	{
		if (!IsWearingUniform && slot != 1)
			return;

		UI_ItemSlot pocket = null;// = slot == 1 ? pocketOneItemSlot : slot == 2 ? pocketTwoItemSlot : suitStorageSlot;

		switch (slot)
		{
			case 1:
				pocket = pocketOneItemSlot;
				break;
			case 2:
				pocket = pocketTwoItemSlot;
				break;
			case 3:
				pocket = suitStorageSlot;
				break;
		}

		// if hand and pocket are empty
		if (UIManager.Hands.CurrentSlot.ItemSlot.IsEmpty && pocket.ItemSlot.IsEmpty)
		{
			Chat.AddExamineMsgToClient(emptyHandNPocketMessage);
			return;
		}
		// if hand and pocket are full
		if (UIManager.Hands.CurrentSlot.ItemSlot.IsOccupied && pocket.ItemSlot.IsOccupied)
		{
			// if first pocket is empty - try to interact
			if (IsWearingUniform && pocketOneItemSlot.ItemSlot.IsEmpty)
			{
				TryInteractWithPocket(1);
				return;
			}

			// if second pocket is empty - try to interact
			if (IsWearingUniform && pocketTwoItemSlot.ItemSlot.IsEmpty)
			{
				TryInteractWithPocket(2);
				return;
			}

			// if third pocket is empty - try to interact
			if (IsWearingXOSuit && suitStorageSlot.ItemSlot.IsEmpty)
			{
				TryInteractWithPocket(3);
				return;
			}

			// all pockets are full
			Chat.AddExamineMsgToClient(fullHandNPocketMessage);
			return;
		}

		pocket.TryItemInteract();
	}
}
