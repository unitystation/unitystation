using UnityEngine;

namespace Weapons
{
	class ClownPin : PinBase
	{

		[SerializeField]
		private bool clusmyMisfire;

		public override void ServerBehaviour(AimApply interaction, bool isSuicide)
		{
			JobType job = GetJobServer(interaction.Performer);
			if (clusmyMisfire && job == JobType.CLOWN)
			{
				CallShotServer(interaction, isSuicide);
			}
			else
			{
				CallShotServer(interaction, true);

				Chat.AddActionMsgToChat(interaction.Performer,
				"You somehow shoot yourself in the face! How the hell?!",
				$"{interaction.Performer.ExpensiveName()} somehow manages to shoot themself in the face!");
			}
		}

		public override void ClientBehaviour(AimApply interaction, bool isSuicide)
		{
			//TODO Commented out as client doesnt sync job, after mind rework see if job is now sync'd
			// JobType job = GetJobClient();
			//
			// if (clusmyMisfire && job == JobType.CLOWN)
			// {
			// 	CallShotClient(interaction, isSuicide);
			// }
			// else
			// {
			// 	CallShotClient(interaction, true);
			// }

			CallShotClient(interaction, true);
		}
	}
}