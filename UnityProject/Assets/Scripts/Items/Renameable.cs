using Mirror;
using UnityEngine;
using WebSocketSharp;

public class Renameable : NBHandActivateInteractable, IRightClickable
{
	public NetTabType NetTabType = NetTabType.Rename;
	[SerializeField]
	private string originalName;
	[SerializeField]
	private string customName;

	public string CustomName
	{
		get => customName;
		private set => SetCustomName( value );
	}

	private ItemAttributes attributes;

	public override void OnStartClient()
	{
		attributes = gameObject.GetComponent<ItemAttributes>();
		base.OnStartClient();
	}

	public override void OnStartServer()
	{
		attributes = gameObject.GetComponent<ItemAttributes>();
		base.OnStartServer();
	}

	public void SetCustomName(string custom)
	{
		var itemName = originalName;

		if (itemName.IsNullOrEmpty())
		{
			itemName = attributes.itemName;
			originalName = itemName;
		}

		customName = custom;

		if (!string.IsNullOrEmpty(custom))
		{
			itemName += " - '" + custom + "'";
		}

		attributes.SetItemName(itemName);
	}

	public string GetCustomName()
	{
		return customName;
	}

	protected override bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;

		var cnt = GetComponent<CustomNetTransform>();
		var ps = interaction.Performer.GetComponent<PlayerScript>();
		var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

		if (pna.GetActiveHandItem() == gameObject)
		{
			return true;
		}

		if (!ps.IsInReach(cnt.RegisterTile, side == NetworkSide.Server))
		{

			return false;
		}

		return true;
	}

	protected override void ServerPerformInteraction(HandActivate interaction)
	{
		OpenRenameDialog(interaction.Performer);
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create();

		if (WillInteract(HandActivate.ByLocalPlayer(), NetworkSide.Client))
		{
			result.AddElement("Rename", RightClickInteract);
		}

		return result;
	}

	private void RightClickInteract()
	{
		Interact(HandActivate.ByLocalPlayer(), nameof(Renameable));
	}

	private void OpenRenameDialog(GameObject player)
	{
		TabUpdateMessage.Send(player, gameObject, NetTabType, TabAction.Open);
	}
}