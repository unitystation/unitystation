using UnityEngine;

namespace Objects.Atmospherics
{
	public class GasHeatExchange : MonoPipe
	{
		[SerializeField]
		private float maxTempExchange = 10f;

		public override void TickUpdate()
		{
			var averageTemp = pipeData.mixAndVolume.Temperature;

			foreach (var pipe in pipeData.ConnectedPipes)
			{
				averageTemp += pipe.mixAndVolume.Temperature;
			}

			averageTemp /= (pipeData.ConnectedPipes.Count + 1);

			var difference = averageTemp - pipeData.mixAndVolume.Temperature;
			difference = Mathf.Clamp(difference, -maxTempExchange, maxTempExchange);

			pipeData.mixAndVolume.Temperature += difference;

			foreach (var pipe in pipeData.ConnectedPipes)
			{
				pipe.mixAndVolume.Temperature += difference;
			}
		}
	}
}