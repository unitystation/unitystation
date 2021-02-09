using Mirror;
public class MixingBowl : NetworkBehaviour, IServerInventoryMove
{
	public RegisterPlayer playerHolding;
	public ItemSlot currentSlot;
	public void OnInventoryMoveServer(InventoryMove info)
	{
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
