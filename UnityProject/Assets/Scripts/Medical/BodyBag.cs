using Mirror;
using UnityEngine;

public class BodyBag : NetworkBehaviour, IInteractable<MouseDrop>
{
	public GameObject prefabVariant;

	public bool Interact(MouseDrop interaction)
	{
		var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

		if (interaction.Performer != PlayerManager.LocalPlayer
		    || interaction.DroppedObject != gameObject
		    || interaction.TargetObject != PlayerManager.LocalPlayer
		    || pna.GetActiveHandItem() != null)
		{
			return false;
		}

		TryFoldUp(pna);
		return true;
	}

	public virtual void TryFoldUp(PlayerNetworkActions pna)
	{
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

		// Remove from world
		PoolManager.PoolNetworkDestroy(gameObject);

		// Add folded to player inventory
		InventoryManager.EquipInInvSlot(pna.Inventory[pna.activeHand],
			PoolManager.PoolNetworkInstantiate(prefabVariant));
	}
}
