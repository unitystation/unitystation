using System.Collections.Generic;
using US13.Managers;
using US13.ScriptableObjects.Atmospherics;
using US13.Strings;
using US13.Tilemaps.Behaviours.Meta;
using US13.Tilemaps.Behaviours.Meta.Atmospherics.Data;
using US13.Tilemaps.Behaviours.Meta.Atmospherics.Data.Reactions;

namespace US13.Systems.InGameEvents.InGameEventScripts
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

				minimumTileTemperature: 0,
				maximumTileTemperature:10000000000,
				minimumTilePressure:0,
				maximumTilePressure: 10000000000,
				minimumTileMoles: 0.01f,
				maximumTileMoles:10000000000
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

			gasMix.AddGasWithTemperature(Gas.Plasma, oxyMoles,gasMix.Temperature );
			gasMix.RemoveGas(Gas.Oxygen, oxyMoles);
		}
	}
}
