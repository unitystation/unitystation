using Systems.Clearance;
using UnityEngine;

namespace Weapons
{
	[RequireComponent(typeof(ClearanceRestricted))]
	class IDLockedPin : PinBase
	{

		[SerializeField]
		private bool clusmyMisfire;

		private ClearanceRestricted clearanceRestricted;

		[SerializeField]
		private string deniedMessage;

		private void Awake()
		{
			clearanceRestricted = GetComponent<ClearanceRestricted>();
		}

		public override void ServerBehaviour(AimApply interaction, bool isSuicide)
		{
			if (clearanceRestricted.HasClearance(interaction.Performer))
			{
				//TODO Commented out as client doesnt sync job, after mind rework see if job is now sync'd
				// JobType job = GetJobServer(interaction.Performer);
				//
				// if (clusmyMisfire && job == JobType.CLOWN)
				// {
				// 	ClumsyShotServer(interaction, isSuicide);
				// }
				// else
				// {
				// 	CallShotServer(interaction, isSuicide);
				// }

				CallShotServer(interaction, isSuicide);
				return;
			}

			Chat.AddExamineMsg(interaction.Performer, deniedMessage);
		}

		public override void ClientBehaviour(AimApply interaction, bool isSuicide)
		{
			if (clearanceRestricted.HasClearance(interaction.Performer))
			{
				//TODO Commented out as client doesnt sync job, after mind rework see if job is now sync'd
				// JobType job = GetJobClient();
				//
				// if ((clusmyMisfire && job == JobType.CLOWN) == false)
				// {
				// 	CallShotClient(interaction, isSuicide);
				// }

				CallShotClient(interaction, isSuicide);

			}
		}
	}
}