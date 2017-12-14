using System.Collections;
using System.Linq;
using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Message that tells clients what their ConnectedPlayers list should contain
/// </summary>
public class UpdateConnectedPlayersMessage : ServerMessage<UpdateConnectedPlayersMessage>
{
    public GameObject[] Players;
    public NetworkInstanceId Subject;

    public override IEnumerator Process()
    {
        yield return WaitFor(Subject);

        var connectedPlayers = PlayerList.Instance.connectedPlayers;
        //Add missing players
        foreach (var player in Players)
        {
            if (!connectedPlayers.ContainsKey(player.name))
            {
                var name = player.GetComponent<PlayerScript>().playerName;
                connectedPlayers.Add(name, player);
            }
        }

        //Remove players that are stored locally, but not on server. Unless its us.
        foreach (var entry in connectedPlayers)
        {
            if (!Players.Contains(entry.Value) && entry.Key != PlayerManager.LocalPlayerScript.playerName)
            {
                connectedPlayers.Remove(entry.Key);
            }
        }
    }

    public static UpdateConnectedPlayersMessage Send(GameObject[] players)
    {
        var msg = new UpdateConnectedPlayersMessage();
        msg.Players = players;

        msg.SendToAll();
        return msg;
    }

    public override string ToString()
    {
        return string.Format("[GibMessage Subject={0} Type={1} Players={2}]", Subject, MessageType, Players);
    }
}