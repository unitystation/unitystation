using UnityEngine;
using UnityEngine.UI;

public class PanelHudBottomController : MonoBehaviour
{
	public UI_ItemSlot backpackItemSlot;
	public UI_ItemSlot PDAItemSlot;
	public UI_ItemSlot beltItemSlot;
	public UI_ItemSlot pocketOneItemSlot;
	public UI_ItemSlot pocketTwoItemSlot;
	public UI_ItemSlot pocketThreeItemSlot;

	private bool _isWearingUniform;
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
				pocketThreeImage.color = Color.white;

				pocketTwoImage.raycastTarget = true;
				pocketThreeImage.raycastTarget = true;
			}
			else
			{
				// player cannot use slot 2 and 3 without uniform
				
				// change pocket image color to gray
				pocketTwoImage.color = greyedPocketColor;
				pocketThreeImage.color = greyedPocketColor;

				// drop items from pockets
				DropItem(pocketTwoItemSlot);
				DropItem(pocketThreeItemSlot);

				// disable raycastTarget so player cannot put items back
				pocketTwoImage.raycastTarget = false;
				pocketThreeImage.raycastTarget = false;
			}
		}
	}

	[Header("UI GameObject references")]
	[SerializeField] private Text backpackKeybindText;
	[SerializeField] private Text PDAKeybindText;
	[SerializeField] private Text beltKeybindText;
	[SerializeField] private Text pocketOneKeybindText;
	[SerializeField] private Text pocketTwoKeybindText;
	[SerializeField] private Text pocketThreeKeybindText;
	[SerializeField] private Image pocketTwoImage;
	[SerializeField] private Image pocketThreeImage;
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
			RemoveListeners();
	}

	/// <summary>
	/// Setup event listeners
	/// </summary>
	public void SetupListeners()
	{
		ItemSlot uniform = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.uniform);
		uniform.OnSlotContentsChangeClient.AddListener(() => OnUniformSlotUpdate());

		OnUniformSlotUpdate();
	}

	/// <summary>
	/// Remove event listeners
	/// </summary>
	public void RemoveListeners()
	{
		ItemSlot uniform = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.uniform);
		uniform.OnSlotContentsChangeClient.RemoveListener(() => OnUniformSlotUpdate());
	}

	/// <summary>
	/// Called when uniform slot content has changed
	/// </summary>
	private void OnUniformSlotUpdate()
	{
		ItemSlot uniform = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.uniform);
		IsWearingUniform = uniform.IsOccupied;
	}

	private void DropItem(UI_ItemSlot itemSlot)
	{
		if (itemSlot.ItemSlot.IsEmpty || PlayerManager.LocalPlayerScript.IsGhost)
			return;
		
		Logger.Log("Drop pocket item - uniform is null", Category.Inventory);

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

		UI_ItemSlot pocket = slot == 1 ? pocketOneItemSlot : slot == 2 ? pocketTwoItemSlot : pocketThreeItemSlot;

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
			if (pocketOneItemSlot.ItemSlot.IsEmpty)
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
			if (IsWearingUniform && pocketThreeItemSlot.ItemSlot.IsEmpty)
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