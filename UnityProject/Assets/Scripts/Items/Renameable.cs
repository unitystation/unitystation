using Items;
using Messages.Server;
using Mirror;
using UnityEngine;
using WebSocketSharp;

public class Renameable : NetworkBehaviour, ICheckedInteractable<HandActivate>, IRightClickable
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

	private ItemAttributesV2 attributes;

	public override void OnStartClient()
	{
		attributes = gameObject.GetComponent<ItemAttributesV2>();
	}

	public override void OnStartServer()
	{
		attributes = gameObject.GetComponent<ItemAttributesV2>();
	}

	public void SetCustomName(string custom)
	{
		var itemName = originalName;

		if (itemName.IsNullOrEmpty())
		{
			itemName = attributes.ArticleName;
			originalName = itemName;
		}

		customName = custom;

		if (!string.IsNullOrEmpty(custom))
		{
			itemName += " - '" + custom + "'";
		}

		attributes.ServerSetArticleName(itemName);
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		var uop = GetComponent<UniversalObjectPhysics>();
		var ps = interaction.Performer.GetComponent<PlayerScript>();
		var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

		if (pna.GetActiveHandItem() == gameObject)
		{
			return true;
		}

		if (!ps.IsRegisterTileReachable(uop.registerTile, side == NetworkSide.Server))
		{

			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
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
		InteractionUtils.RequestInteract(HandActivate.ByLocalPlayer(), this);
	}

	private void OpenRenameDialog(GameObject player)
	{
		TabUpdateMessage.Send(player, gameObject, NetTabType, TabAction.Open);
	}
}