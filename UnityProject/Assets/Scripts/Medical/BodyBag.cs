using UnityEngine;

public class BodyBag : Interactable<MouseDrop>
{
	public GameObject prefabVariant;

	protected override bool WillInteract(MouseDrop interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side))
		{
			return false;
		}

		var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

		if (!pna
			|| interaction.Performer != interaction.TargetObject
		    || interaction.DroppedObject != gameObject
			|| pna.GetActiveHandItem() != null)
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
			ChatRelay.Instance.AddToChatLogClient("You wrestle with the body bag, but it won't fold while unzipped.",
				ChatChannel.Examine);
			return;
		}

		if (!closetControl.IsEmpty())
		{
			ChatRelay.Instance.AddToChatLogClient("There are too many things inside of the body bag to fold it up!",
				ChatChannel.Examine);
			return;
		}

		// Add folded to player inventory
		InventoryManager.EquipInInvSlot(pna.Inventory[pna.activeHand],
			PoolManager.PoolNetworkInstantiate(prefabVariant));

		// Remove from world
		Destroy(gameObject);
	}
}
