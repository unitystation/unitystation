using UnityEngine;

namespace Weapons
{
	public abstract class PinBase: MonoBehaviour
	{
		[HideInInspector]
		public Gun gunComp;

		public abstract void ServerBehaviour(AimApply interaction, bool isSuicide);
		public abstract void ClientBehaviour(AimApply interaction, bool isSuicide);

		protected void CallShotServer(AimApply interaction, bool isSuicide)
		{
			gunComp.ServerShoot(interaction.Performer, interaction.TargetVector.normalized, UIManager.DamageZone, isSuicide);
		}

		protected void CallShotClient(AimApply interaction, bool isSuicide)
		{
			var dir = gunComp.ApplyRecoil(interaction.TargetVector.normalized);
			gunComp.DisplayShot(PlayerManager.LocalPlayer, dir, UIManager.DamageZone, isSuicide, gunComp.CurrentMagazine.containedBullets[0].name, gunComp.CurrentMagazine.containedProjectilesFired[0]);
		}

		protected JobType GetJobServer(GameObject player)
		{
			return PlayerList.Instance.Get(player).Job;
		}

		protected JobType GetJobClient()
		{
			//TODO Client doesnt sync job, after mind rework see if job is now sync'd
			return PlayerManager.LocalPlayerScript.mind.occupation.JobType;
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