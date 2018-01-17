using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Message that tells clients what their ConnectedPlayers list should contain
/// </summary>
public class UpdateConnectedPlayersMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.UpdateConnectedPlayersMessage;
	public ClientConnectedPlayer[] Players;
	public NetworkInstanceId Subject;

	public override IEnumerator Process()
	{
		Debug.Log("Processed " + ToString());
		yield return WaitFor(Subject);

		PlayerList.Instance.ClientConnectedPlayers.Clear();
		for ( var i = 0; i < Players.Length; i++ )
		{
			PlayerList.Instance.ClientConnectedPlayers.Add(Players[i]);
		}
		PlayerList.Instance.RefreshPlayerListText();

//		//Add missing players
//		for ( var i = 0; i < Players.Length; i++ )
//		{
//			var player = Players[i];
//			if ( !connectedPlayers.ContainsName(player.Name) )
//			{
//				connectedPlayers.Add(player);
//			}
//		}
//
//		//Remove players that are stored locally, but not on server. Unless its us.
//		//foreach does not allow mutations on the dictionary collection while it is iterating over it
//		//Store names to be removed and do it after
//		var playersToRemove = new List<ConnectedPlayer>();
//		for ( var i = 0; i < connectedPlayers.Values.Count; i++ )
//		{
//			ConnectedPlayer existingPlayer = connectedPlayers.Values[i];
//			if ( !Players.Contains(existingPlayer) && existingPlayer.Name != PlayerManager.LocalPlayerScript.playerName )
//			{
//				playersToRemove.Add(existingPlayer);
//			}
//		}
//
//		for (int i = 0; i < playersToRemove.Count; i++)
//		{
//			connectedPlayers.Values.Remove(playersToRemove[i]);
//		}
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
		return $"[UpdateConnectedPlayersMessage Subject={Subject} Type={MessageType} Players={string.Join(", ", Players)}]";
	}
}