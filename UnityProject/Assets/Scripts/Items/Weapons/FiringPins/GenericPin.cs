using UnityEngine;

namespace Weapons
{
	public class GenericPin : PinBase
	{

		[SerializeField]
		private bool clusmyMisfire;

		public override void ServerBehaviour(AimApply interaction, bool isSuicide)
		{
			JobType job = GetJobServer(interaction.Performer);
			if (clusmyMisfire && job == JobType.CLOWN)
			{
				ClumsyShotServer(interaction, isSuicide);
			}
			else
			{
				CallShotServer(interaction, isSuicide);
			}
		}

		public override void ClientBehaviour(AimApply interaction, bool isSuicide)
		{
			//TODO Commented out as client doesnt sync job, after mind rework see if job is now sync'd
			// JobType job = GetJobClient();
			// if ((clusmyMisfire && job == JobType.CLOWN) == false)
			// {
			// 	CallShotClient(interaction, isSuicide);
			// }

			CallShotClient(interaction, isSuicide);
		}
	}
}