using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	public class PipeActions
	{
		public PipeData pipeData;
		public virtual void TickUpdate()
		{

		}
	}

	public class MonoActions : PipeActions
	{
		public MonoPipe MonoPipe;
		private bool Initialised = false;
		public override void TickUpdate()
		{
			if (Initialised == false)
			{
				MonoPipe = pipeData.MonoPipe;
				if (MonoPipe == null)
				{
					Logger.Log("oh no.. MonoPipe is null");
				}
			}
			MonoPipe.TickUpdate();
		}
	}

	public class WaterPumpAction : MonoActions
	{
		public override void TickUpdate()
		{
			base.TickUpdate();
		}
	}

	public class ReservoirAction : MonoActions
	{
		public override void TickUpdate()
		{
			base.TickUpdate();
			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);
		}
	}
}
