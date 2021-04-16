using UnityEngine;

namespace Weapons
{
    class IDLockedPin : PinBase
    {

		[SerializeField]
		private bool clusmyMisfire;

		[SerializeField]
		private JobType setRestriction;

		public JobType SetRestriction => setRestriction;

		[SerializeField]
		private string deniedMessage;

		public override void ServerBehaviour(AimApply interaction, bool isSuicide)
		{
			JobType job = GetJobServer(interaction.Performer);
			if (job == setRestriction || setRestriction == JobType.NULL)
			{
				if (clusmyMisfire && job == JobType.CLOWN)
				{
					ClumsyShotServer(interaction, isSuicide);
				}
				else
				{
					CallShotServer(interaction, isSuicide);				
				}
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, deniedMessage);
			}
        }

		public override void ClientBehaviour(AimApply interaction, bool isSuicide)
		{
			JobType job = GetJobClient();

			if (job == setRestriction || setRestriction == JobType.NULL)
			{
				if ((clusmyMisfire && job == JobType.CLOWN) == false)
				{
					CallShotClient(interaction, isSuicide);
				}
			}
		}
    }
}