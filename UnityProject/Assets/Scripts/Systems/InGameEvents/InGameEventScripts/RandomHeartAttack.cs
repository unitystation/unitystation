using System.Linq;
using HealthV2;
using Items.Implants.Organs;
using UnityEngine;

namespace InGameEvents
{
	public class RandomHeartAttack : EventScriptBase
	{
		public override void OnEventStart()
		{
			if (FakeEvent) return;

			var Body = MatrixManager.MainStationMatrix.MatrixMove.GetComponentsInChildren<LivingHealthMasterBase>().Where(x => x.IsDead == false);

			if (Body.Any() == false) return;

			var Power = Body.PickRandom();
			var HeartAttacks = Power.GetBodyFunctionsOfType<Heart>();
			foreach (var HeartAttack in HeartAttacks)
			{
				if (HeartAttack.CanHaveHeartAttack == false) continue;
				HeartAttack.HeartAttack = true;
			}

			base.OnEventStart();
		}
	}
}