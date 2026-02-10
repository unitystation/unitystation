using System.Linq;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs;
using US13.Managers.MatrixManager;
using Util;

namespace US13.Systems.InGameEvents.InGameEventScripts
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