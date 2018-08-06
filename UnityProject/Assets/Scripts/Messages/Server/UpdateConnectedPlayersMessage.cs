using System.Collections;

/// <summary>
///     Message that tells clients what their ConnectedPlayers list should contain
/// </summary>
public class UpdateConnectedPlayersMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.UpdateConnectedPlayersMessage;
	public ClientConnectedPlayer[] Players;

	public override IEnumerator Process()
	{
//		Logger.Log("Processed " + ToString());

		PlayerList.Instance.ClientConnectedPlayers.Clear();
		for ( var i = 0; i < Players.Length; i++ )
		{
			PlayerList.Instance.ClientConnectedPlayers.Add(Players[i]);
		}
		PlayerList.Instance.RefreshPlayerListText();
		yield return null;
	}

	public static UpdateConnectedPlayersMessage Send()
	{
		UpdateConnectedPlayersMessage msg = new UpdateConnectedPlayersMessage();
		msg.Players = PlayerList.Instance.ClientConnectedPlayerList.ToArray();

		msg.SendToAll();
		return msg;
	}

	public override string ToString()
	{
		return $"[UpdateConnectedPlayersMessage Type={MessageType} Players={string.Join(", ", Players)}]";
	}
}