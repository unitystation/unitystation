using UnityEngine;

public class BodyBag : Interactable<MouseDrop>, IOnStageServer, IRightClickable
{
	public GameObject prefabVariant;

	public void GoingOnStageServer(OnStageInfo info)
	{
		GetComponent<ClosetControl>().ToggleLocker(false);
	}

	protected override bool WillInteract(MouseDrop interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side))
		{
			return false;
		}

		var cnt = GetComponent<CustomNetTransform>();
		var ps = interaction.Performer.GetComponent<PlayerScript>();

		var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

		if (!pna
			|| interaction.Performer != interaction.TargetObject
		    || interaction.DroppedObject != gameObject
			|| pna.GetActiveHandItem() != null
			|| !ps.IsInReach(cnt.RegisterTile, side == NetworkSide.Server))
		{
			return false;
		}

		return true;
	}

	protected override void ServerPerformInteraction(MouseDrop interaction)
	{
		var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

		var closetControl = GetComponent<ClosetControl>();
		if (!closetControl.IsClosed)
		{
			UpdateChatMessage.Send(interaction.Performer, ChatChannel.Examine,
				"You wrestle with the body bag, but it won't fold while unzipped.");
			return;
		}

		if (!closetControl.IsEmpty())
		{
			UpdateChatMessage.Send(interaction.Performer, ChatChannel.Examine,
				"There are too many things inside of the body bag to fold it up!");
			return;
		}

		// Add folded to player inventory
		InventoryManager.EquipInInvSlot(pna.Inventory[pna.activeHand],
			PoolManager.PoolNetworkInstantiate(prefabVariant));

		// Remove from world
		PoolManager.PoolNetworkDestroy(gameObject);
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create();

		if (WillInteract(MouseDrop.ByLocalPlayer(gameObject, PlayerManager.LocalPlayer), NetworkSide.Client))
		{
			result.AddElement("Fold Up", RightClickInteract);
		}

		return result;
	}

	private void RightClickInteract()
	{
		Interact(MouseDrop.ByLocalPlayer(gameObject, PlayerManager.LocalPlayer));
	}
}
