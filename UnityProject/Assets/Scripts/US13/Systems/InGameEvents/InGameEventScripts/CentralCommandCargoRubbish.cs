using US13.Managers;

namespace US13.Systems.InGameEvents.InGameEventScripts
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