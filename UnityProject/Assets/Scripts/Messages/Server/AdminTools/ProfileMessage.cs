using System.Collections.Generic;
using AdminTools;
using SecureStuff;
using Mirror;
using Newtonsoft.Json;
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
			var listData = JsonConvert.DeserializeObject<SafeProfileManager.ProfileEntryDataList>(msg.JsonData);
			UIManager.Instance.profileScrollView.RefreshProfileList(listData);

		}

		public static NetMessage Send(GameObject recipient)
		{
			var profileList = new SafeProfileManager.ProfileEntryDataList();
			profileList.Profiles = GetAllProfiles();
			var data = JsonConvert.SerializeObject(profileList);


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

		private static List<SafeProfileManager.ProfileEntryData> GetAllProfiles()
		{
			return SafeProfileManager.Instance.GetCurrentProfiles();
		}


	}
}
