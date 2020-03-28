using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RequestChangeVariableNetMessage : ClientMessage
{
	public override short MessageType => (short) MessageTypes.RequestChangeVariableNetMessage;
	public string newValue;
	public ulong PageID;
	public bool IsNewBookshelf = false;
	public string AdminId;
	public string AdminToken;

	public override IEnumerator Process()
	{
		ValidateAdmin();
		yield return null;
	}

	void ValidateAdmin()
	{
		var admin = PlayerList.Instance.GetAdmin(AdminId, AdminToken);
		if (admin == null) return;
		VariableViewer.RequestChangeVariable(PageID, newValue);

		Logger.Log($"Admin {admin.name} changed variable {PageID} (in VV) with a new value of: {newValue} ",
			Category.Admin);
	}


	public static RequestChangeVariableNetMessage Send(ulong _PageID, string _newValue, string adminId, string adminToken)
	{
		RequestChangeVariableNetMessage msg = new RequestChangeVariableNetMessage();
		msg.PageID = _PageID;
		msg.newValue = _newValue;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;

		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		newValue = reader.ReadString();
		PageID = reader.ReadUInt64();
		IsNewBookshelf = reader.ReadBoolean();
		AdminId = reader.ReadString();
		AdminToken = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(newValue);
		writer.WriteUInt64(PageID);
		writer.WriteBoolean(IsNewBookshelf);
		writer.WriteString(AdminId);
		writer.WriteString(AdminToken);
	}
}