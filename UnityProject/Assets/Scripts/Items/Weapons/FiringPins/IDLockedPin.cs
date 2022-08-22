using System;
using Systems.Clearance;
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

		private ClearanceCheckable clearanceCheckable;

		[SerializeField]
		private string deniedMessage;

		private void Awake()
		{
			clearanceCheckable = GetComponent<ClearanceCheckable>();
		}

		public override void ServerBehaviour(AimApply interaction, bool isSuicide)
		{
			/* --ACCESS REWORK--
			 *  TODO Remove the AccessRestriction check when we finish migrating!
			 *
			 */
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
				return; //we found access skip clearance check
			}

			if (clearanceCheckable.HasClearance(interaction.Performer))
			{
				CallShotServer(interaction, isSuicide);
				return;
			}

			Chat.AddExamineMsg(interaction.Performer, deniedMessage);
		}

		public override void ClientBehaviour(AimApply interaction, bool isSuicide)
		{
			/* --ACCESS REWORK--
			 *  TODO Remove the AccessRestriction check when we finish migrating!
			 *
			 */
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
				return; //we found access skip clearance check
			}

			if (clearanceCheckable.HasClearance(interaction.Performer))
			{
				CallShotClient(interaction, isSuicide);

			}
		}
	}
}