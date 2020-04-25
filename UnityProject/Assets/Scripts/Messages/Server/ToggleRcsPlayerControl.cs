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
			Camera2DFollow.followControl.target = shuttleConsole.transform;
		}
		else
		{
			PlayerManager.SetMovementControllable(PlayerManager.LocalPlayerScript.PlayerSync);
			Camera2DFollow.followControl.target = PlayerManager.LocalPlayer.transform;
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