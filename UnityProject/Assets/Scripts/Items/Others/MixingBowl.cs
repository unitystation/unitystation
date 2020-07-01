using Mirror;
public class MixingBowl : NetworkBehaviour, IServerInventoryMove
{
	public RegisterPlayer playerHolding;
	public void OnInventoryMoveServer(InventoryMove info)
	{
		if (info.FromPlayer != null && info.FromPlayer != info.ToPlayer)
		{
			playerHolding = null;
		}
		if (info.ToPlayer != null)
		{
			playerHolding = info.ToPlayer;
		}
	}
}
