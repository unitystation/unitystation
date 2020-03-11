using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;

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

    public void ServerAddPlayerReply(string message, string fromUserID)
    {
	    if (!serverAdminChatLogs.ContainsKey(fromUserID))
	    {
		    serverAdminChatLogs.Add(fromUserID, new List<AdminChatMessage>());
	    }

	    serverAdminChatLogs[fromUserID].Add(new AdminChatMessage
	    {
		    fromUserid = fromUserID,
		    message = message
	    });
    }

    public void ServerAddAdminReply(string message, string playerId, string adminId)
    {
	    if (!serverAdminChatLogs.ContainsKey(playerId))
	    {
		    serverAdminChatLogs.Add(playerId, new List<AdminChatMessage>());
	    }

	    serverAdminChatLogs[playerId].Add(new AdminChatMessage
	    {
		    fromUserid = adminId,
		    message = message,
		    wasFromAdmin = true
	    });
    }

//    public List<AdminChatMessage> ServerGetUnreadMessages(string playerId)
//    {
//
//    }
}
