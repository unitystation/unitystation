using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Systems.Occupations;
using Util;

namespace US13.Items.Weapons.FiringPins
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
	}
}