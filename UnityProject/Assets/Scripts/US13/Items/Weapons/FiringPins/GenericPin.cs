using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Systems.Occupations;

namespace US13.Items.Weapons.FiringPins
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
	}
}