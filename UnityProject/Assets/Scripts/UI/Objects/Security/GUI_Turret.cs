using UnityEngine;
using UI.Core.NetUI;
using Objects.Other;

namespace UI.Objects.Security
{
	public class GUI_Turret : NetTab
	{
		[SerializeField]
		private NetText_label labelPower = null;

		[SerializeField]
		private NetText_label labelWeapons = null;

		[SerializeField]
		private NetText_label labelRecord = null;

		[SerializeField]
		private NetText_label labelArrest = null;

		[SerializeField]
		private NetText_label labelAuthorised = null;

		[SerializeField]
		private NetText_label labelLifeSigns = null;

		private Turret turret;
		private Turret Turret => turret ??= Provider.GetComponent<Turret>();

		public void OnTabOpenedHandler(PlayerInfo connectedPlayer)
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
