using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosphericAnalyser : MonoBehaviour, IInteractable<HandActivate>
{
	public void ServerPerformInteraction(HandActivate interaction)
	{
		string toShow = "";
		var metaDataLayer = MatrixManager.AtPoint(interaction.PerformerPlayerScript.registerTile.WorldPositionServer, true).MetaDataLayer;
		if (metaDataLayer != null)
		{
			var node = metaDataLayer.Get(interaction.Performer.transform.localPosition.RoundToInt());
			if(node != null)
			{
				toShow = $"Pressure : {node.GasMix.Pressure:0.###} kPa \n"
				         + $"Temperature : {node.GasMix.Temperature:0.###} K {node.GasMix.Temperature-Atmospherics.Reactions.KOffsetC:0.###} C)" + " \n" //You want Fahrenheit? HAHAHAHA
				         + $"Total Moles of gas : {node.GasMix.Moles:0.###} \n"
				         + $"Oxygen : {node.GasMix.GasRatio(Atmospherics.Gas.Oxygen) * 100:0.###} %\n"
				         + $"Plasma : {node.GasMix.GasRatio(Atmospherics.Gas.Plasma) * 100:0.###} %\n"
				         + $"Nitrogen : {node.GasMix.GasRatio(Atmospherics.Gas.Nitrogen) * 100:0.###} %\n"
				         + $"Carbon dioxide : {node.GasMix.GasRatio(Atmospherics.Gas.CarbonDioxide) * 100:0.###} %\n";
			}
		}
		Chat.AddExamineMsgFromServer(interaction.Performer, toShow);
	}
}