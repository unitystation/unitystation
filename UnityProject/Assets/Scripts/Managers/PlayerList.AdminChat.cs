using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AdminTools;
using Mirror;

/// <summary>
/// The admin chat relay
/// </summary>
public partial class PlayerList
{
	/// <summary>
	/// All messages sent and recieved from players to admins
	/// </summary>
    private Dictionary<string, List<AdminChatMessage>> serverAdminChatLogs
	    = new Dictionary<string, List<AdminChatMessage>>();

	/// <summary>
	/// The admins client local cache for the ui
	/// </summary>
    private Dictionary<string, List<AdminChatMessage>> clientAdminChatLogs
	    = new Dictionary<string, List<AdminChatMessage>>();

	[Server]
	public void ServerAddChatRecord(string message, string playerId, string adminId = "")
    {
	    if (!serverAdminChatLogs.ContainsKey(playerId))
	    {
		    serverAdminChatLogs.Add(playerId, new List<AdminChatMessage>());
	    }

	    var entry = new AdminChatMessage
	    {
		    fromUserid = playerId,
		    message = message
	    };

	    if (!string.IsNullOrEmpty(adminId))
	    {
		    entry.fromUserid = adminId;
		    entry.wasFromAdmin = true;
	    }
	    serverAdminChatLogs[playerId].Add(entry);

	    ServerMessageRecordingAndTrim(playerId, entry);
    }

	[Server]
    void ServerMessageRecordingAndTrim(string playerId, AdminChatMessage entry)
    {
	    var chatlogDir = Path.Combine(Application.streamingAssetsPath, "chatlogs");
	    if (!Directory.Exists(chatlogDir))
	    {
		    Directory.CreateDirectory(chatlogDir);
	    }

	    var filePath = Path.Combine(chatlogDir, $"{playerId}.txt");

	    var connectedPlayer = GetByUserID(playerId);

	    if (!File.Exists(filePath))
	    {
		    File.Create(filePath);
		    string header = $"Username: {connectedPlayer.Username} Player Name: {connectedPlayer.Name} \r\n" +
		                    $"IsAntag: {AntagPlayers.Contains(connectedPlayer)}  role: {connectedPlayer.Job} \r\n" +
		                    $"-----Chat Log----- \r\n" +
		                    $" \r\n";
		    File.AppendAllText(filePath, header);
	    }

	    string entryName = connectedPlayer.Name;
	    if (entry.wasFromAdmin)
	    {
		    var adminPlayer = GetByUserID(entry.fromUserid);
		    entryName = "[A] " + adminPlayer.Name;
	    }

	    File.AppendAllText(filePath, $"{entryName}: {entry.message}");

	    if (serverAdminChatLogs[playerId].Count == 70)
	    {
		    var firstEntry = serverAdminChatLogs[playerId][0];
		    serverAdminChatLogs[playerId].Remove(firstEntry);
	    }
    }

    public void ClientGetUnreadMessages(string playerId)
    {
	    if (!clientAdminChatLogs.ContainsKey(playerId))
	    {
		    clientAdminChatLogs.Add(playerId, new List<AdminChatMessage>());
	    }

	    AdminCheckMessages.Send(playerId, clientAdminChatLogs[playerId].Count);
    }

    [Server]
    public void ServerGetUnreadMessages(string playerId, int currentCount, NetworkConnection requestee)
    {
	    if (!serverAdminChatLogs.ContainsKey(playerId))
	    {
		    serverAdminChatLogs.Add(playerId, new List<AdminChatMessage>());
	    }

	    if (currentCount >= serverAdminChatLogs[playerId].Count)
	    {
		    return;
	    }

	    AdminChatUpdate update = new AdminChatUpdate();
	    update.messages = serverAdminChatLogs[playerId].GetRange(currentCount - 1,
		    serverAdminChatLogs[playerId].Count - currentCount);

	    TargetUpdateChatLog(requestee, JsonUtility.ToJson(update), playerId);
    }

    [TargetRpc]
    void TargetUpdateChatLog(NetworkConnection target, string unreadMessagesJson, string playerId)
    {
	    if (string.IsNullOrEmpty(unreadMessagesJson)) return;

	    if (!clientAdminChatLogs.ContainsKey(playerId))
	    {
		    clientAdminChatLogs.Add(playerId, new List<AdminChatMessage>());
	    }

	    clientAdminChatLogs[playerId].AddRange(JsonUtility.FromJson<AdminChatUpdate>(unreadMessagesJson).messages);

		EventManager.Broadcast(EVENT.UpdatedAdminChatLogs);
    }
}
