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
				try
				{
					electricalSync.DoTick();
				}
				catch (Exception e)
				{
					Logger.LogError($"Electrical Thread Error! {e.GetStack()}", Category.Electrical);
				}
			}
			electricalSync.sampler.End();
		}
	}
}