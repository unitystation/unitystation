using System.Runtime.ConstrainedExecution;
using UnityEngine;

namespace Atmospherics
{
	/// <summary>
	/// Represents the potential for a MetaDataNode to ignite the gases on it, and provides logic related to igniting the actual
	/// gases.
	/// </summary>
	public class Hotspot
	{
		/// <summary>
		/// Current temperature on the tile
		/// </summary>
		public float Temperature { get; private set; }

		/// <summary>
		/// Current volume of the tile
		/// </summary>
		public float Volume { get; private set; }

		/// <summary>
		/// Node this hotspot lives on.
		/// </summary>
		public MetaDataNode node;

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

		/// <summary>
		/// Exposes the hotspot, igniting gases on the tile
		/// </summary>
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
				node.GasMix -= removed;
			}
		}

		private bool Check()
		{
			if (Temperature > Reactions.PlasmaMaintainFire && Volume > 0.0001 && node.GasMix.GetMoles(Gas.Plasma) > 0.1f &&
				node.GasMix.GetMoles(Gas.Oxygen) > 0.1f)
			{
				if (PlasmaFireReaction.GetOxygenContact(node.GasMix) > Reactions.MinimumOxygenContact)
				{
					return (true);
				}else{
					return (false);
				}
			}
			else {
				return (false);
			}
		}
	}
}