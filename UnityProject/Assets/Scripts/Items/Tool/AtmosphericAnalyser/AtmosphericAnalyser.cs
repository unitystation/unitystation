using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Systems.Atmospherics;
using Objects.Atmospherics;


namespace Items.Atmospherics
{
	public class AtmosphericAnalyser : MonoBehaviour, ICheckedInteractable<HandActivate>, ICheckedInteractable<PositionalHandApply>
	{
		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			var metaDataLayer = MatrixManager.AtPoint(interaction.PerformerPlayerScript.registerTile.WorldPositionServer, true).MetaDataLayer;
			if (metaDataLayer != null)
			{
				var node = metaDataLayer.Get(interaction.Performer.transform.localPosition.RoundToInt());
				if (node != null)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, GetGasMixInfo(node.GasMix));
				}
			}
		}

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.TargetObject == gameObject) return false;

			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (interaction.TargetObject != null)
			{
				if (interaction.TargetObject.TryGetComponent(out GasContainer container))
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, GetGasMixInfo(container.GasMix));
					return;
				}

				if (interaction.TargetObject.TryGetComponent(out MonoPipe monoPipe))
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, GetGasMixInfo(monoPipe.pipeData.mixAndVolume.GetGasMix()));
					return;
				}
			}

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

		private static string GetGasMixInfo(GasMix gasMix)
		{
			StringBuilder sb = new StringBuilder(
					$"Pressure: {gasMix.Pressure:0.###} kPa, {gasMix.Moles:0.##} moles\n" +
					$"Temperature: {gasMix.Temperature:0.##} K ({gasMix.Temperature - Reactions.KOffsetC:0.##} °C)\n");
					// You want Fahrenheit? HAHAHAHA

			foreach (var gas in gasMix.GasesArray)
			{
				var ratio = gasMix.GasRatio(gas.GasSO);

				if (ratio.Approx(0) == false)
				{
					sb.AppendLine($"{gas.GasSO.Name}: {ratio:P}");
				}
			}

			return $"</i>{sb}<i>";
		}
	}
}
