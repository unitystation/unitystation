using UnityEngine;

namespace Weapons
{
	public abstract class PinBase: MonoBehaviour
	{

		private System.Random rnd = new System.Random();
		private Gun gunComp;

		private void OnEnable()
		{
			gunComp = GetComponent<Gun>();
		}

		public abstract void ServerBehaviour(AimApply interaction);
		public abstract void ClientBehaviour(AimApply interaction);

		protected void CallShotServer(AimApply interaction, bool isSuicide)
		{
			gunComp.ServerShoot(interaction.Performer, interaction.TargetVector.normalized, UIManager.DamageZone, isSuicide);
		}

		protected void CallShotClient(AimApply interaction, bool isSuicide)
		{	
			var dir = gunComp.ApplyRecoil(interaction.TargetVector.normalized);
			gunComp.DisplayShot(PlayerManager.LocalPlayer, dir, UIManager.DamageZone, isSuicide, gunComp.CurrentMagazine.containedBullets[0].name, gunComp.CurrentMagazine.containedProjectilesFired[0]);
		}

		protected bool IsSuicide(AimApply interaction)
		{
			var isSuicide = false;
			if (interaction.MouseButtonState == MouseButtonState.PRESS ||
				(gunComp.WeaponType != WeaponType.SemiAutomatic && gunComp.AllowSuicide))
			{
				isSuicide = interaction.IsAimingAtSelf;
				gunComp.AllowSuicide = isSuicide;
			}
			return isSuicide;
		}

		protected JobType GetJobServer(GameObject player)
		{
			return PlayerList.Instance.Get(player).Job;
		}

		protected JobType GetJobClient()
		{
			return PlayerManager.LocalPlayerScript.mind.occupation.JobType; 
		}

		protected void ClumsyShotServer(AimApply interaction)
		{
			//shooting a non-clusmy weapon as a clusmy person
			int chance = rnd.Next(0 ,2);
			if (chance == 0)
			{
				CallShotServer(interaction, true);

				Chat.AddActionMsgToChat(interaction.Performer,
				"You fumble up and shoot yourself!",
				$"{interaction.Performer.ExpensiveName()} fumbles up and shoots themself!");
			}
			else
			{
				CallShotServer(interaction, IsSuicide(interaction));
			}
		}
	}
}