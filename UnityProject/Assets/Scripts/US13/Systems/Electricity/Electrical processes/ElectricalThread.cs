using US13.Core.Threading;

namespace US13.Systems.Electricity.Electrical_processes
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