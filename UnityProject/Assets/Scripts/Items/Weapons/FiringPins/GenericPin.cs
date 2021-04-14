using UnityEngine;
namespace Weapons
{
	public class GenericPin : PinBase
	{

		[SerializeField]
		private bool clusmyMisfire;

		public override void ServerBehaviour(AimApply interaction)
		{
			JobType job = GetJobServer(interaction.Performer);
			if (clusmyMisfire && job == JobType.CLOWN)
			{
				ClumsyShotServer(interaction);
			}
			else
			{
				CallShotServer(interaction, IsSuicide(interaction));
			}
		}

		public override void ClientBehaviour(AimApply interaction)
		{
			JobType job = GetJobClient();
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