using AdminTools;
using Antagonists;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine;
namespace Messages.Client.Admin
{
	public class ObjectiveRefreshMessage : ServerMessage<ObjectiveRefreshMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public uint Recipient;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Recipient);
			var info = JsonConvert.DeserializeObject<AntagonistInfo>(msg.JsonData);

			var page = UnityEngine.Object.FindFirstObjectByType<PlayerObjectiveManagerPage>();
			page.RefreshInformation(info);
		}

		public static NetMessage Send(GameObject recipient, string adminID, string playerForRequestID)
		{
			//Gather the data
			var objectivesInfo = new AntagonistInfo();
			var player = PlayerList.Instance.GetPlayerByID(playerForRequestID);
			if (player.Mind.AntagPublic.Antagonist != null)
			{
				objectivesInfo.antagID = AntagData.Instance.GetIndexAntag(player.Mind.AntagPublic.Antagonist);
			}
			objectivesInfo.IsAntagCanSeeObjectivesStatus = player.Mind.AntagPublic.IsAntagCanSeeObjectivesStatus;

			for (int i = 0; i < player.Mind.AntagPublic.Objectives.Count(); i++)
			{
				var x = player.Mind.AntagPublic.Objectives.ElementAt(i);
				var objInfo = new ObjectiveInfo
				{
					Status = x.IsComplete(),
					Description = x.Description,
					Name = x.name,
					ID = x.ID,
					IsCustom = x is CustomObjective,
					IsEndRound = x.IsEndRoundObjective
				};

				objectivesInfo.Objectives.Add(objInfo);
			}

			var data = JsonConvert.SerializeObject(objectivesInfo);

			NetMessage msg =
				new NetMessage { Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data };

			SendTo(recipient, msg);
			return msg;
		}
	}
}