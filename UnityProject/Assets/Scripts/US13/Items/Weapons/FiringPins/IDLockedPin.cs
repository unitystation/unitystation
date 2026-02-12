using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Systems.Clearance;
using US13.Systems.Occupations;

namespace US13.Items.Weapons.FiringPins
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
				JobType job = GetJobServer(interaction.Performer);

				if (clusmyMisfire && job == JobType.CLOWN)
				{
					ClumsyShotServer(interaction, isSuicide);
				}
				else
				{
					CallShotServer(interaction, isSuicide);
				}

				return;
			}

			Chat.AddExamineMsg(interaction.Performer, deniedMessage);
		}
	}
}