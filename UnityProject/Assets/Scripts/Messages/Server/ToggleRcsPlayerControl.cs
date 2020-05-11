/// <summary>
///     Message that toggles rcs control over a
///     movable matrix
/// </summary>
public class ToggleRcsPlayerControl : ServerMessage
{
	public uint ShuttleConsoleNetId;
	public bool ActiveControl;

	public override void Process()
	{
		LoadNetworkObject(ShuttleConsoleNetId);
		var shuttleConsole = NetworkObject.GetComponent<ShuttleConsole>();
		if (shuttleConsole == null) return;
		if (ActiveControl)
		{
			PlayerManager.SetMovementControllable(shuttleConsole.ShuttleMatrixMove);
		}
		else
		{
			PlayerManager.SetMovementControllable(PlayerManager.LocalPlayerScript.PlayerSync);
		}
	}

	public static ToggleRcsPlayerControl UpdateClient(ConnectedPlayer playerToUpdate, uint shuttleConsoleNetId, bool activateControl)
	{
		ToggleRcsPlayerControl msg = new ToggleRcsPlayerControl { ShuttleConsoleNetId = shuttleConsoleNetId,
			ActiveControl = activateControl};

		msg.SendTo(playerToUpdate.Connection);

		return msg;
	}
}