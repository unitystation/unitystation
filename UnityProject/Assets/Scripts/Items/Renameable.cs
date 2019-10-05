using System.Text.RegularExpressions;
using UnityEngine;

public class Renameable : Interactable<HandActivate>, IRightClickable
{
	public NetTabType NetTabType = NetTabType.Rename;

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

	public void SetCustomName(string msg)
	{
		var attributes = gameObject.GetComponent<ItemAttributes>();
		var customName = attributes.itemName.Split('-')[0].Trim();
		if (!string.IsNullOrEmpty(msg))
		{
			customName += " - '" + msg + "'";
		}
		attributes.SetItemName(customName);
	}

	public string GetCustomName()
	{
		var attributes = gameObject.GetComponent<ItemAttributes>();
		var customName = "";
		var match = Regex.Match(attributes.itemName, @"'(.*)'");
		if (match.Groups.Count > 1)
		{
			customName = match.Groups[1].Value;
		}

		return customName;
	}
}