using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosphericAnalyser : MonoBehaviour, ICheckedInteractable<HandActivate>
{

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		string ToShow = "";
		var MetaDataLayer = MatrixManager.AtPoint(interaction.PerformerPlayerScript.registerTile.WorldPositionServer, true).MetaDataLayer;
		if (MetaDataLayer != null) { 
			var Node = MetaDataLayer.Get(interaction.Performer.transform.localPosition.RoundToInt());
			if(Node != null){
				ToShow = "Pressure : " + Node.GasMix.Pressure + " Kpa \n"
				+ "Temperature : " + Node.GasMix.Temperature + "K \n"
				+ "Total Moles of gas : " + Node.GasMix.Moles + " \n"
				+ "Oxygen : %" + Node.GasMix.GasRatio(Atmospherics.Gas.Oxygen) + "\n"
				+ "Plasma : %" + Node.GasMix.GasRatio(Atmospherics.Gas.Plasma) + "\n"
				+ "Nitrogen : %" + Node.GasMix.GasRatio(Atmospherics.Gas.Nitrogen) + "\n"
				+ "Carbon dioxide : %" + Node.GasMix.GasRatio(Atmospherics.Gas.CarbonDioxide) + "\n";
			}
		}
		Chat.AddExamineMsgFromServer(interaction.Performer, ToShow);
	}
}
