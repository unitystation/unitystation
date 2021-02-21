using System.Collections.Generic;
using UnityEngine;
using Mirror;
using AdminTools;


using System.IO;

public class ProfileMessage : ServerMessage
{
	public class ProfileMessageNetMessage : NetworkMessage
	{
		public string JsonData;
		public uint Recipient;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as ProfileMessageNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.Recipient);
		var listData = JsonUtility.FromJson<ProfileEntryDataList>(newMsg.JsonData);
		UIManager.Instance.profileScrollView.RefreshProfileList(listData);

	}

	public static ProfileMessageNetMessage Send(GameObject recipient)
	{
		var profileList = new ProfileEntryDataList();
		profileList.Profiles = GetAllProfiles();
		var data = JsonUtility.ToJson(profileList);


		ProfileMessageNetMessage msg = new ProfileMessageNetMessage {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};
		new ProfileMessage().SendTo(recipient, msg);

		return msg;
	}

	public static void SendToApplicable()
	{
		var adminList = PlayerList.Instance.GetAllAdmins();
		foreach (var admin in adminList)
		{
			Send(admin.GameObject);
		}
	}

	private static List<ProfileEntryData> GetAllProfiles()
	{
		var profileList = new List<ProfileEntryData>();
		var info = new DirectoryInfo("Profiles");

		if (!info.Exists)
			return profileList;

		var fileInfo = info.GetFiles();
		foreach (var file in fileInfo)
		{
			var entry = new ProfileEntryData();
			entry.Name = file.Name;
			var size = (float)file.Length / 1048576; // 1048576 = 1024 * 1024
			entry.Size = System.Math.Round(size, 2) + " MB";
			profileList.Add(entry);
		}
		return profileList;
	}


}
