using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Managers;
using US13.Systems.Occupations;
using Util;

namespace US13.Items.Weapons.FiringPins
{
	public abstract class PinBase: MonoBehaviour
	{
		[HideInInspector]
		public Gun gunComp;

		public abstract void ServerBehaviour(AimApply interaction, bool isSuicide);

		protected void CallShotServer(AimApply interaction, bool isSuicide)
		{
			gunComp.ServerShoot(interaction.Performer, interaction.TargetVector.normalized, interaction.TargetBodyPart, isSuicide, interaction.TargetObject);
		}

		protected JobType GetJobServer(GameObject player)
		{
			return PlayerList.Instance.GetOnline(player).Job;
		}

		protected void ClumsyShotServer(AimApply interaction, bool isSuicide)
		{
			//shooting a non-clusmy weapon as a clusmy person
			if (DMMath.Prob(50))
			{
				CallShotServer(interaction, true);

				Chat.AddActionMsgToChat(interaction.Performer,
				"You fumble up and shoot yourself!",
				$"{interaction.Performer.ExpensiveName()} fumbles up and shoots themself!");
			}
			else
			{
				CallShotServer(interaction, isSuicide);
			}
		}
	}
}