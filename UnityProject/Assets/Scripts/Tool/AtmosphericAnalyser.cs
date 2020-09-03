using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class AtmosphericAnalyser : MonoBehaviour, IInteractable<HandActivate>, ICheckedInteractable<PositionalHandApply>
{
	public void ServerPerformInteraction(HandActivate interaction)
	{
		string toShow = "";
		var metaDataLayer = MatrixManager.AtPoint(interaction.PerformerPlayerScript.registerTile.WorldPositionServer, true).MetaDataLayer;
		if (metaDataLayer != null)
		{
			var node = metaDataLayer.Get(interaction.Performer.transform.localPosition.RoundToInt());
			if (node != null)
			{
				toShow = GetGasMixInfo(node.GasMix);
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
		Vector3 worldPosition = interaction.WorldPositionTarget;
		var matrix = MatrixManager.AtPoint(worldPosition.CutToInt(), true);
		var localPosition = MatrixManager.WorldToLocal(worldPosition, matrix).CutToInt();
		var metaDataNode = matrix.MetaDataLayer.Get(localPosition, false);

		if (metaDataNode.PipeData.Count > 0)
		{
			var gasMix = metaDataNode.PipeData[0].pipeData.GetMixAndVolume.GetGasMix();
			Chat.AddExamineMsgFromServer(interaction.Performer, GetGasMixInfo(gasMix));
		}
	}

	private string GetGasMixInfo(GasMix gasMix)
	{
		string info = $"Pressure: {gasMix.Pressure:0.###} kPa\n" +
				$"Temperature: {gasMix.Temperature:0.###} K ({gasMix.Temperature - Reactions.KOffsetC:0.###} °C)\n" +
				// You want Fahrenheit? HAHAHAHA
				$"Total Moles of gas: {gasMix.Moles:0.###}\n";

		foreach (var gas in Gas.All)
		{
			var ratio = gasMix.GasRatio(gas);

			if (ratio != 0)
			{
				info += $"{gas.Name}: {ratio * 100:0.###} %\n";
			}
		}

		return info;
	}
}
