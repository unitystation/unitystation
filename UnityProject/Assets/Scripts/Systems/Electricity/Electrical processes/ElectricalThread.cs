using System;
using Core.Threading;

namespace Systems.Electricity
{
	public class ElectricalThread : ThreadedBehaviour
	{
		public override void ThreadedWork()
		{
			var electricalSync = ElectricalManager.Instance.electricalSync;
			electricalSync.sampler.Begin();
			if (electricalSync.MainThreadStep == false)
			{
				electricalSync.DoTick();
			}
			electricalSync.sampler.End();
		}
	}
}