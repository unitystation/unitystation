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

		public override void ServerBehaviour(AimApply interaction)
		{
			JobType job = GetJobServer(interaction.Performer);
			if (job == setRestriction || setRestriction == JobType.NULL)
			{
				if (clusmyMisfire && job == JobType.CLOWN)
				{
					ClumsyShotServer(interaction);
				}
				else
				{
					CallShotServer(interaction, IsSuicide(interaction));				
				}
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, deniedMessage);
			}
        }

		public override void ClientBehaviour(AimApply interaction)
		{
			JobType job = GetJobClient();

			if (job == setRestriction || setRestriction == JobType.NULL)
			{
				if (clusmyMisfire && job == JobType.CLOWN)
				{
					return;
				}
				else
				{
					CallShotClient(interaction, IsSuicide(interaction));				
				}
			}
		}
    }
}