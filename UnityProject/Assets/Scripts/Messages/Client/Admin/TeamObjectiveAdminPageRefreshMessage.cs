using Antagonists;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Messages.Client.Admin
{
	public class TeamObjectiveAdminPageRefreshMessage : ServerMessage<TeamObjectiveAdminPageRefreshMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public uint Recipient;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Recipient);
			var info = JsonConvert.DeserializeObject<TeamsInfo>(msg.JsonData);

			var page = UnityEngine.Object.FindFirstObjectByType<TeamObjectiveAdminPage>();
			page.RefreshInformation(info);
		}

		public static NetMessage Send(GameObject recipient, string adminID)
		{
			var objectivesInfo = new TeamsInfo();

			foreach (var team in AntagManager.Instance.Teams)
			{
				TeamInfo teamInfo = new TeamInfo
				{
					Index = AntagData.Instance.GetTeamIndex(team.Value.Data),
					Name = team.Value.GetTeamName(),
					ID = team.Key
				};

				foreach (var teamMember in team.Value.TeamMembers)
				{
					teamInfo.MembersInfo.Add(new TeamMemberInfo()
					{
						Id = teamMember.Owner.ControlledBy.AccountId,
					});
				}

				foreach (var objective in team.Value.TeamObjectives)
				{
					teamInfo.ObjsInfo.Add(new ObjectiveInfo()
					{
						ID = objective.ID,
						PrefabID = AntagData.Instance.GetIndexObj(objective),
						Status = objective.IsComplete(),
						Description = objective.GetShortDescription(),
						Name = objective.ObjectiveName
					});
				}

				objectivesInfo.TeamsInfos.Add(teamInfo);
			}

			var data = JsonConvert.SerializeObject(objectivesInfo);

			NetMessage msg =
				new NetMessage { Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data };

			SendTo(recipient, msg);
			return msg;
		}
	}
}