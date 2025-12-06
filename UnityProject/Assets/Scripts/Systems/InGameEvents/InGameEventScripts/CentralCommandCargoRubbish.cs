using System.Linq;
using HealthV2;
using InGameEvents;
using Items.Implants.Organs;
using Systems.Cargo;
using UnityEngine;

namespace InGameEvents
{
	public class CentralCommandCargoRubbish : EventScriptBase
	{
		public override void OnEventStart()
		{
			if (FakeEvent) return;

			CargoManager.Instance.NTNeedsSomethingDumping = true;

			base.OnEventStart();
		}
	}
}