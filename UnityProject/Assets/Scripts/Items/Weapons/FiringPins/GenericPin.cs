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
	}
}