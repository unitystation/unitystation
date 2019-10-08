using Mirror;
using UnityEngine;
using WebSocketSharp;

public class Renameable : NBHandActivateInteractable, IRightClickable
{
	public NetTabType NetTabType = NetTabType.Rename;

	[SyncVar(hook = nameof(SyncOriginalName))]
	public string originalName;

	[SyncVar(hook = nameof(SyncCustomName))]
	public string customName;

	private ItemAttributes attributes;

	private void SyncOriginalName(string original)
	{
		originalName = original;
	}

	private void SyncCustomName(string custom)
	{
		customName = custom;

		var itemName = originalName;

		if (itemName.IsNullOrEmpty())
		{
			itemName = attributes.itemName;
			SyncOriginalName(itemName);
		}

		if (!string.IsNullOrEmpty(custom))
		{
			itemName += " - '" + custom + "'";
		}

		attributes.SetItemName(itemName);
	}

	public override void OnStartClient()
	{
		SyncEverything();
		base.OnStartClient();
	}

	public override void OnStartServer()
	{
		SyncEverything();
		base.OnStartServer();
	}

	public void SetCustomName(string msg)
	{
		SyncCustomName(msg);
	}

	public string GetCustomName()
	{
		return customName;
	}

	private void SyncEverything()
	{
		attributes = gameObject.GetComponent<ItemAttributes>();
		SyncOriginalName(originalName);
		SyncCustomName(customName);
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