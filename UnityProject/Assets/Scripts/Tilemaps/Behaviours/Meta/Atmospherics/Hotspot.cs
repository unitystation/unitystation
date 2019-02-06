using System.Runtime.ConstrainedExecution;
using UnityEngine;

namespace Atmospherics
{
	public class Hotspot
	{
		public float Temperature { get; private set; }

		public float Volume { get; private set; }

		private MetaDataNode node;

		public Hotspot(MetaDataNode node, float temperature, float volume)
		{
			this.node = node;
			Temperature = temperature;
			Volume = volume;
		}

		public void UpdateValues(float volume, float temperature)
		{
			if (Volume < volume)
			{
				Volume = volume;
			}
			if (Temperature < temperature)
			{
				Temperature = temperature;
			}
		}

		public bool Process()
		{
			if (!Check())
			{
				return false;
			}

			Expose();

			return true;
		}

		private void Expose()
		{
			if ((Volume / node.GasMix.Volume) > 0.95f)
			{
				GasMix gasMix = node.GasMix;
				float consumed = Reactions.React(ref gasMix);
				node.GasMix = gasMix;
				Volume = consumed * 40;
				Temperature = node.GasMix.Temperature;
			}
			else
			{
				GasMix removed = node.GasMix.RemoveVolume(Volume);
				removed.Temperature = Temperature;

				float consumed = Reactions.React(ref removed);
				Volume = consumed * 40;

				Temperature = removed.Temperature;
				node.GasMix += removed;
			}
		}

		private bool Check()
		{
			return Temperature > Reactions.PLASMA_MINIMUM_BURN_TEMPERATURE && Volume > 0.0001 && node.GasMix.GetMoles(Gas.Plasma) > 0 &&
			       node.GasMix.GetMoles(Gas.Oxygen) > 0;
		}
	}
}