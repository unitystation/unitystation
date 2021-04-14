using UnityEngine;

namespace Weapons
{
	class ClownPin : PinBase
	{

		[SerializeField]
		private bool clusmyMisfire;

		public override void ServerBehaviour(AimApply interaction)
		{
			JobType job = GetJobServer(interaction.Performer);
			if (clusmyMisfire && job == JobType.CLOWN)
			{
				CallShotServer(interaction, IsSuicide(interaction));
			}
			else
			{
				CallShotServer(interaction, true);

				Chat.AddActionMsgToChat(interaction.Performer,
				"You somehow shoot yourself in the face! How the hell?!",
				$"{interaction.Performer.ExpensiveName()} somehow manages to shoot themself in the face!");
			}
		}

		public override void ClientBehaviour(AimApply interaction)
		{
			JobType job = GetJobClient();

			if (clusmyMisfire && job == JobType.CLOWN)
			{
				CallShotClient(interaction, IsSuicide(interaction));
			}
			else
			{
				CallShotClient(interaction, true);
			}
		}
	}
}