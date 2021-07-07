using System.Collections.Generic;
using Systems;
using Messages.Server;
using Mirror;
using UI;

namespace Messages.Server
{
	public class UpdateJobCountsMessage : ServerMessage<UpdateJobCountsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public JobType JobType;
			public int Amount;
		}

		public override void Process(NetMessage msg)
		{
			CrewManifestManager.Instance.ChangeJobList(msg.JobType, msg.Amount);
			UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().UpdateJobsList();
		}

		public static NetMessage Send(JobType job, int newAmount)
		{
			NetMessage msg = new NetMessage
			{
				JobType = job,
				Amount = newAmount
			};

			SendToAll(msg);
			return msg;
		}
	}

	public class SetJobCountsMessage : ServerMessage<SetJobCountsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public JobType[] JobTypes;
			public int[] Amounts;
			public bool IsClear;
		}

		public override void Process(NetMessage msg)
		{
			if (msg.IsClear)
			{
				CrewManifestManager.Instance.ClientClearList();
				UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().UpdateJobsList();
				return;
			}

			var jobList = new Dictionary<JobType, int>(msg.JobTypes.Length);

			for (int i = 0; i < msg.JobTypes.Length; i++)
			{
				jobList.Add(msg.JobTypes[i], msg.Amounts[i]);
			}

			CrewManifestManager.Instance.SetJobList(jobList);
			UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().UpdateJobsList();
		}

		public static NetMessage SendToPlayer(Dictionary<JobType, int> jobs, ConnectedPlayer toPlayer)
		{
			var jobTypes = new JobType[jobs.Count];
			var amounts = new int[jobs.Count];
			var count = 0;

			foreach (var job in jobs)
			{
				jobTypes[count] = job.Key;
				amounts[count] = job.Value;

				count++;
			}

			NetMessage msg = new NetMessage
			{
				JobTypes = jobTypes,
				Amounts = amounts
			};

			SendTo(toPlayer, msg);
			return msg;
		}

		public static NetMessage SendClearMessage()
		{
			NetMessage clearMsg = new NetMessage
			{
				JobTypes = new JobType[0],
				Amounts = new int[0],
				IsClear = true
			};

			SendToAll(clearMsg);
			return clearMsg;
		}
	}
}
