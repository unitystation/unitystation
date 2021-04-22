using System;
using UnityEngine;

namespace Weapons
{
	[RequireComponent(typeof(AccessRestrictions))]
    class IDLockedPin : PinBase
    {

		[SerializeField]
		private bool clusmyMisfire;

		private AccessRestrictions accessRestrictions;
		public AccessRestrictions AccessRestrictions {
			get {
				if (accessRestrictions == false)
				{
					accessRestrictions = GetComponent<AccessRestrictions>();
				}
				return accessRestrictions;
			}
		}

		[SerializeField]
		private string deniedMessage;

		public override void ServerBehaviour(AimApply interaction, bool isSuicide)
		{
			if (AccessRestrictions.CheckAccess(interaction.Performer))
			{
				//TODO Commented out as client doesnt sync job, after mind rework see if job is now sync'd
				// JobType job = GetJobServer(interaction.Performer);
				//
				// if (clusmyMisfire && job == JobType.CLOWN)
				// {
				// 	ClumsyShotServer(interaction, isSuicide);
				// }
				// else
				// {
				// 	CallShotServer(interaction, isSuicide);
				// }

				CallShotServer(interaction, isSuicide);
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, deniedMessage);
			}
        }

		public override void ClientBehaviour(AimApply interaction, bool isSuicide)
		{

			if (AccessRestrictions.CheckAccess(interaction.Performer))
			{
				//TODO Commented out as client doesnt sync job, after mind rework see if job is now sync'd
				// JobType job = GetJobClient();
				//
				// if ((clusmyMisfire && job == JobType.CLOWN) == false)
				// {
				// 	CallShotClient(interaction, isSuicide);
				// }

				CallShotClient(interaction, isSuicide);
			}
		}
    }
}