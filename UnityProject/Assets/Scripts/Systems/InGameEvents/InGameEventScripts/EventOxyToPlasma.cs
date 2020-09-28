using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;

namespace InGameEvents
{
	public class EventOxyToPlasma : EventScriptBase
	{
		public override void OnEventStart()
		{
			if (AnnounceEvent)
			{
				var text = "It appears the chemistry of the universe has been broken, damn those science nerds.";

				CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
			}

			if (FakeEvent) return;

			new GasReactions(

				reaction: new OxyToPlasma(),

				gasReactionData: new Dictionary<Gas, GasReactionData>()
				{
					{
						Gas.Oxygen,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					}
				},

				minimumTemperature: 0f,
				maximumTemperature:10000000000f,
				minimumPressure:0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles:10000000000f,
				energyChange: 0f
				);

			base.OnEventStart();
		}
	}

	public class OxyToPlasma : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public float React(ref GasMix gasMix, Vector3 tilePos)
		{
			gasMix.AddGas(Gas.Plasma, 1f);

			gasMix.RemoveGas(Gas.Oxygen, 1f);

			return 0f;
		}
	}
}
