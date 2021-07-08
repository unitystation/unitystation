using System.Collections.Generic;
using System.IO;
using AdminTools;
using Mirror;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	public class ProfileMessage : ServerMessage<ProfileMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public uint Recipient;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Recipient);
			var listData = JsonUtility.FromJson<ProfileEntryDataList>(msg.JsonData);
			UIManager.Instance.profileScrollView.RefreshProfileList(listData);

		}

		public static NetMessage Send(GameObject recipient)
		{
			var profileList = new ProfileEntryDataList();
			profileList.Profiles = GetAllProfiles();
			var data = JsonUtility.ToJson(profileList);


			NetMessage msg = new NetMessage {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

			SendTo(recipient, msg);
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
}
