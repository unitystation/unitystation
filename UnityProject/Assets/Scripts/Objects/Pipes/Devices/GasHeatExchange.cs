using Systems.Pipes;
using UnityEngine;

namespace Objects.Atmospherics
{
	public class GasHeatExchange : MonoPipe
	{
		public override void TickUpdate()
		{
			MixAndVolume otherMixAndVolume = null;

			foreach (var pipe in pipeData.ConnectedPipes)
			{
				if(PipeFunctions.IsPipeTypeFlagTo(pipeData, pipe, PipeType.HeatExchange) == false) continue;

				if(PipeFunctions.IsPipeTypeFlagTo(pipe, pipeData, PipeType.HeatExchange) == false) continue;

				otherMixAndVolume = pipe.GetMixAndVolume;
				break;
			}

			if(otherMixAndVolume == null) return;

			var thisMixAndVolume = pipeData.mixAndVolume;

			var thisHeatCapacity = thisMixAndVolume.WholeHeatCapacity;
			var otherHeatCapacity = otherMixAndVolume.WholeHeatCapacity;
			var combinedHeatCapacity = thisHeatCapacity + otherHeatCapacity;

			if (combinedHeatCapacity <= 0) return;

			var thisOldTemperature = thisMixAndVolume.Temperature;
			var otherOldTemperature = otherMixAndVolume.Temperature;

			var combinedEnergy = (otherOldTemperature * otherHeatCapacity) + (thisOldTemperature * thisHeatCapacity);
			var newTemperature = combinedEnergy / combinedHeatCapacity;

			thisMixAndVolume.Temperature = newTemperature;
			otherMixAndVolume.Temperature = newTemperature;
		}
	}
}