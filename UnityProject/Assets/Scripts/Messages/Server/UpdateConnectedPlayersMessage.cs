using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using System.Linq;

/// <summary>
/// Message that tells clients what their ConnectedPlayers list should contain
/// </summary>
public class UpdateConnectedPlayersMessage : ServerMessage<UpdateConnectedPlayersMessage>
{
    public NetworkInstanceId Subject;
    public GameObject[] Players;

    public override IEnumerator Process()
    {
        yield return WaitFor(Subject);

        Dictionary<string, GameObject> connectedPlayers = PlayerList.Instance.connectedPlayers;
        //Add missing players
        foreach (GameObject player in Players)
        {
            if (!connectedPlayers.ContainsKey(player.name))
            {
                string name = player.GetComponent<PlayerScript>().playerName;
                connectedPlayers.Add(name, player);
            }
        }

        //Remove players that are stored locally, but not on server. Unless its us.
        foreach (KeyValuePair<string, GameObject> entry in connectedPlayers)
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