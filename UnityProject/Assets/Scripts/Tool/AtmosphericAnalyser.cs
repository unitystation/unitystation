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
				toShow = "Pressure : " + node.GasMix.Pressure + " Kpa \n"
				+ "Temperature : " + node.GasMix.Temperature + "K (" + (node.GasMix.Temperature-Atmospherics.Reactions.KOffsetC) + "C)" + " \n" //You want Fahrenheit? HAHAHAHA
				+ "Total Moles of gas : " + node.GasMix.Moles + " \n"
				+ "Oxygen : %" + node.GasMix.GasRatio(Atmospherics.Gas.Oxygen) + "\n"
				+ "Plasma : %" + node.GasMix.GasRatio(Atmospherics.Gas.Plasma) + "\n"
				+ "Nitrogen : %" + node.GasMix.GasRatio(Atmospherics.Gas.Nitrogen) + "\n"
				+ "Carbon dioxide : %" + node.GasMix.GasRatio(Atmospherics.Gas.CarbonDioxide) + "\n";
			}
		}
		Chat.AddExamineMsgFromServer(interaction.Performer, toShow);
	}
}
