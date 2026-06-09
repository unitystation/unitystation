using Mirror;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;

namespace US13.Items.Others
{
	public class MixingBowl : NetworkBehaviour, IServerInventoryMove
	{
		public RegisterPlayer playerHolding;
		public ItemSlot currentSlot;
		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (this.gameObject != info.MovedObject.gameObject) return;

			if (info.FromPlayer != null && info.FromPlayer != info.ToPlayer)
			{
				playerHolding = null;
				currentSlot = null;
			}
			if (info.ToPlayer != null)
			{
				playerHolding = info.ToPlayer;
				currentSlot = info.ToSlot;
			}
		}
	}
}
