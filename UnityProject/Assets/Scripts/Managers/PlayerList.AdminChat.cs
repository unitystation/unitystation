using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;

/// <summary>
/// The admin chat relay
/// </summary>
public partial class PlayerList
{
    public Dictionary<string, List<AdminChatMessage>> adminChatInbox
	    = new Dictionary<string, List<AdminChatMessage>>();

    public void AddPlayerReply(string message, string fromUserID)
    {
	    foreach (var a in adminChatInbox)
	    {
			a.Value.Add(new AdminChatMessage
			{
				fromUserid = fromUserID,
				toUserid = a.Key,
				message = message
			});
	    }
    }

    public List<AdminChatMessage> CheckAdminInbox(string adminUserID)
    {
	    var list = new List<AdminChatMessage>();

	    if (!adminChatInbox.ContainsKey(adminUserID)) return list;
	    if (adminChatInbox[adminUserID].Count == 0) return list;

	    list = new List<AdminChatMessage>(adminChatInbox[adminUserID]);
	    adminChatInbox[adminUserID].Clear();
	    return list;
    }
}
