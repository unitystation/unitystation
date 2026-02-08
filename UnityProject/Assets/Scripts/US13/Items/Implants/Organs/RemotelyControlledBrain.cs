using UnityEngine;
using US13.HealthV2.Living;
using US13.Player.MovementV2;
using US13.Systems.Clearance;
using US13.UI.Core.Alerts;

namespace US13.Items.Implants.Organs
{
	public class RemotelyControlledBrain : BodyPartFunctionality
	{

		public AlertSO LockeddownAlertSO;

		private bool lockdown = false;

		public bool Lockdown => lockdown;

		private BasicClearanceSource BasicClearanceSource;

		public override void Awake()
		{
			base.Awake();

			BasicClearanceSource = this.GetComponent<BasicClearanceSource>();
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			if (!lockdown) return;

			var MovementSynchronisation = livingHealth.GetComponent<MovementSynchronisation>();
			if (MovementSynchronisation != null)
			{
				MovementSynchronisation.ServerAllowInput.RemovePosition(this);
				if (lockdown)
				{
					var BodyAlertManager = livingHealth.GetComponent<BodyAlertManager>();
					if (BodyAlertManager != null)
					{
						BodyAlertManager.UnRegisterAlert(LockeddownAlertSO);
					}
				}
			}

		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			if (!lockdown) return;

			var MovementSynchronisation = livingHealth.GetComponent<MovementSynchronisation>();
			if (MovementSynchronisation != null)
			{
				MovementSynchronisation.ServerAllowInput.RecordPosition(this, !lockdown);
				if (lockdown)
				{
					var BodyAlertManager = livingHealth.GetComponent<BodyAlertManager>();
					if (BodyAlertManager != null)
					{
						BodyAlertManager.RegisterAlert(LockeddownAlertSO);
					}
				}


			}

		} //Warning only add body parts do not remove body parts in this

		[NaughtyAttributes.Button()]
		public void ToggleLockdown()
		{
			lockdown = !lockdown;
			LockdownChange();
		}

		public void SetLockdown(bool Value)
		{
			lockdown = Value;
			LockdownChange();
		}


		private void LockdownChange()
		{
			//MovementSynchronisation

			if (RelatedPart.HealthMaster != null)
			{
				if (lockdown)
				{
					var MovementSynchronisation = RelatedPart.HealthMaster.GetComponent<MovementSynchronisation>();
					if (MovementSynchronisation != null)
					{
						MovementSynchronisation.ServerAllowInput.RecordPosition(this, !lockdown);
					}
				}
				else
				{
					var MovementSynchronisation = RelatedPart.HealthMaster.GetComponent<MovementSynchronisation>();
					if (MovementSynchronisation != null)
					{
						MovementSynchronisation.ServerAllowInput.RemovePosition(this);
					}
				}
			}

			var BodyAlertManager = RelatedPart.HealthMaster.GetComponent<BodyAlertManager>();
			if (BodyAlertManager != null)
			{
				if (lockdown)
				{
					BodyAlertManager.RegisterAlert(LockeddownAlertSO);
				}
				else
				{
					BodyAlertManager.UnRegisterAlert(LockeddownAlertSO);
				}

			}


			if (BasicClearanceSource != null)
			{
				BasicClearanceSource.ClearanceDisabled = lockdown;
			}

		}

	}
}
