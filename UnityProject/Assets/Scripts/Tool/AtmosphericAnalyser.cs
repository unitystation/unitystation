using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;

public class AtmosphericAnalyser : MonoBehaviour, IInteractable<HandActivate>, ICheckedInteractable<PositionalHandApply>
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
				         + $"Temperature : {node.GasMix.Temperature:0.###} K {node.GasMix.Temperature - Atmospherics.Reactions.KOffsetC:0.###} C)" +
				         " \n" //You want Fahrenheit? HAHAHAHA
				         + $"Total Moles of gas : {node.GasMix.Moles:0.###} \n";

				foreach (var gas in Gas.All)
				{
					var ratio = node.GasMix.GasRatio(gas);

					if (ratio != 0)
					{
						toShow += $"{gas.Name} : {ratio * 100:0.###} %\n";
					}
				}
			}
		}
		Chat.AddExamineMsgFromServer(interaction.Performer, toShow);
	}

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (interaction.HandObject == null) return false;
		if (Validations.IsInReach(interaction.PerformerPlayerScript.WorldPos, interaction.WorldPositionTarget) == false) return false;
		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		Vector3Int worldPosInt = interaction.WorldPositionTarget.To2Int().To3Int();
		MatrixInfo matrixinfo = MatrixManager.AtPoint(worldPosInt, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixinfo);
		var matrix = interaction.Performer.GetComponentInParent<Matrix>();
		string toShow = "";
		foreach (var pipeNode in matrix.GetPipeConnections(localPosInt))
		{
			toShow += pipeNode.ToAnalyserExamineString() + "\n";
		}


		Chat.AddExamineMsgFromServer(interaction.Performer, toShow);
	}
}