using Objects.Other;
using UnityEngine;

namespace UI.Objects.Security
{
	public class GUI_Turret : NetTab
	{
		[SerializeField]
		private NetLabel labelPower = null;

		[SerializeField]
		private NetLabel labelWeapons = null;

		[SerializeField]
		private NetLabel labelRecord = null;

		[SerializeField]
		private NetLabel labelArrest = null;

		[SerializeField]
		private NetLabel labelAuthorised = null;

		[SerializeField]
		private NetLabel labelLifeSigns = null;

		private Turret turret;
		private Turret Turret {
			get {
				if (turret == null)
					turret = Provider.GetComponent<Turret>();

				return turret;
			}
		}

		public void OnTabOpenedHandler(ConnectedPlayer connectedPlayer)
		{
			labelPower.Value = Turret.HasPower ? Turret.CurrentTurretState == Turret.TurretState.Off ? "Off" : "On" : "No Power";
			labelWeapons.Value = Turret.CheckWeaponAuthorisation ? "Yes" : "No";
			labelRecord.Value = Turret.CheckSecurityRecords ? "Yes" : "No";
			labelArrest.Value = Turret.CheckForArrest ? "Yes" : "No";
			labelAuthorised.Value = Turret.CheckUnauthorisedPersonnel ? "Yes" : "No";
			labelLifeSigns.Value = Turret.CheckUnidentifiedLifeSigns ? "Yes" : "No";
		}

		public void OnTogglePower()
		{
			Turret.ChangeBulletState(Turret.CurrentTurretState == Turret.TurretState.Off ? Turret.TurretState.Stun : Turret.TurretState.Off);
		}

		public void OnToggleWeapons()
		{
			Turret.CheckWeaponAuthorisation = !Turret.CheckWeaponAuthorisation;
			Turret.UpdateGui();
		}

		public void OnToggleRecord()
		{
			Turret.CheckSecurityRecords = !Turret.CheckSecurityRecords;
			Turret.UpdateGui();
		}

		public void OnToggleArrest()
		{
			Turret.CheckForArrest = !Turret.CheckForArrest;
			Turret.UpdateGui();
		}

		public void OnToggleAuthorised()
		{
			Turret.CheckUnauthorisedPersonnel = !Turret.CheckUnauthorisedPersonnel;
			Turret.UpdateGui();
		}

		public void OnToggleLifeSigns()
		{
			Turret.CheckUnidentifiedLifeSigns = !Turret.CheckUnidentifiedLifeSigns;
			Turret.UpdateGui();
		}
	}
}
