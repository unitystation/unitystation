using System.Collections.Generic;
using Logs;
using UnityEngine;
using Systems.Pipes;


namespace Objects.Atmospherics
{
	public class PipeActions
	{
		public PipeData pipeData;
		public virtual void TickUpdate() { }
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
					Loggy.Log("Tried to update MonoPipe, but it was null", Category.Pipes);
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
