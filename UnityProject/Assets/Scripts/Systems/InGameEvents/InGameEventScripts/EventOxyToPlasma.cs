using System.Collections;
using System.Collections.Generic;
using Systems.Atmospherics;
using Managers;
using ScriptableObjects.Atmospherics;
using Strings;
using UnityEngine;

namespace InGameEvents
{
	public class EventOxyToPlasma : EventScriptBase
	{
		private GasReactions? currentReaction;

		public override void OnEventStart()
		{
			//Dont add another reaction if one is already going on
			if(currentReaction != null) return;

			if (AnnounceEvent)
			{
				var text = "It appears the chemistry of the universe has been broken, damn those science nerds.";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			}

			if (FakeEvent) return;

			currentReaction = new GasReactions(

				reaction: new OxyToPlasma(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
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

		public override void OnEventEnd()
		{
			if (currentReaction != null)
			{
				GasReactions.RemoveReaction(currentReaction.Value);
				currentReaction = null;
			}
		}
	}

	public class OxyToPlasma : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			var oxyMoles = gasMix.GetMoles(Gas.Oxygen);

			gasMix.AddGas(Gas.Plasma, oxyMoles);
			gasMix.RemoveGas(Gas.Oxygen, oxyMoles);
		}
	}
}
