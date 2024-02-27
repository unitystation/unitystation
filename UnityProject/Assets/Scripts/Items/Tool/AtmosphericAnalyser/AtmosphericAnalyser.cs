﻿using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Systems.Atmospherics;
using Objects.Atmospherics;
using Systems.Pipes;


namespace Items.Atmospherics
{
	public class AtmosphericAnalyser : MonoBehaviour, ICheckedInteractable<HandActivate>,
		ICheckedInteractable<PositionalHandApply>, ICheckedInteractable<InventoryApply>
	{
		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (interaction.PerformerPlayerScript.ObjectPhysics.ContainedInObjectContainer != null &&
			    interaction.PerformerPlayerScript.ObjectPhysics.ContainedInObjectContainer
				    .TryGetComponent<GasContainer>(
					    out var container))
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, GetGasMixInfo(container.GasMixLocal));
				return;
			}

			var metaDataLayer = interaction.PerformerPlayerScript.RegisterPlayer.Matrix.MetaDataLayer;
			if (metaDataLayer != null)
			{
				var node = metaDataLayer.Get(interaction.Performer.transform.localPosition.RoundToInt());
				if (node != null)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, GetGasMixInfo(node.GasMixLocal));
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
					Chat.AddExamineMsgFromServer(interaction.Performer, GetGasMixInfo(container.GasMixLocal));
					return;
				}

				if (interaction.TargetObject.TryGetComponent(out MonoPipe monoPipe))
				{
					Chat.AddExamineMsgFromServer(interaction.Performer,
						GetMixAndVolumeInfo(PipeFunctions.PipeOrNet(monoPipe.pipeData)));
					return;
				}
			}

			Vector3 worldPosition = interaction.WorldPositionTarget;
			var matrix = MatrixManager.AtPoint(worldPosition.CutToInt(), true);
			var localPosition = MatrixManager.WorldToLocal(worldPosition, matrix).CutToInt();
			var metaDataNode = matrix.MetaDataLayer.Get(localPosition, false);

			if (metaDataNode.PipeData.Count > 0)
			{

				var mix = PipeFunctions.PipeOrNet(metaDataNode.PipeData[0].pipeData);
				Chat.AddExamineMsgFromServer(interaction.Performer, GetMixAndVolumeInfo(mix));
			}
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.TargetObject == null || interaction.UsedObject == null) return false;

			//Dont target self
			if (interaction.TargetObject == gameObject) return false;

			//Make sure used object is ourself
			if (interaction.UsedObject != gameObject) return false;

			return interaction.TargetObject.TryGetComponent<GasContainer>(out _);
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (interaction.TargetObject.TryGetComponent<GasContainer>(out var container) == false) return;

			Chat.AddExamineMsgFromServer(interaction.Performer, GetGasMixInfo(container.GasMixLocal));
		}


		private static string GetMixAndVolumeInfo(MixAndVolume mixAndVolume)
		{
			var density = mixAndVolume.Density();
			StringBuilder sb = new StringBuilder(
				$"Liquid density : {density.x:0.###},  {mixAndVolume.GetReagentMix().Total:0.###} U , Gas pressure : {density.y:0.###} kPa,  {mixAndVolume.GetGasMix().Moles:0.##} moles\n" +
				$"Temperature: {mixAndVolume.Temperature:0.##} K ({mixAndVolume.Temperature - Reactions.KOffsetC:0.##} °C)\n");
			// You want Fahrenheit? HAHAHAHA

			var gasMix = mixAndVolume.GetGasMix();

			lock (gasMix) //no Double lock
			{
				foreach (var gas in gasMix.GasesArray) //doesn't appear to modify list while iterating
				{
					var ratio = gasMix.GasRatio(gas.GasSO);

					if (ratio.Approx(0) == false)
					{
						sb.AppendLine($"{gas.GasSO.Name}: {ratio:P}");
					}
				}
			}

			var reagentMix = mixAndVolume.GetReagentMix();
			if (reagentMix.reagents.Count > 0)
			{
				sb.AppendLine($"================");
				lock (reagentMix.reagents)
				{
					foreach (var liquid in reagentMix.reagents)
					{
						var ratio = reagentMix.GetPercent(liquid.Key);

						if (ratio.Approx(0) == false)
						{
							sb.AppendLine($"{liquid.Key.Name}: {ratio:P}");
						}
					}
				}
			}


			return $"</i>{sb}<i>";
		}

		private static string GetGasMixInfo(GasMix gasMix)
		{
			StringBuilder sb = new StringBuilder(
				$"Pressure: {gasMix.Pressure:0.###} kPa, {gasMix.Moles:0.##} moles\n" +
				$"Temperature: {gasMix.Temperature:0.##} K ({gasMix.Temperature - Reactions.KOffsetC:0.##} °C)\n");
			// You want Fahrenheit? HAHAHAHA

			lock (gasMix.GasesArray) //no Double lock
			{
				foreach (var gas in gasMix.GasesArray) //doesn't appear to modify list while iterating
				{
					var ratio = gasMix.GasRatio(gas.GasSO);

					if (ratio.Approx(0) == false)
					{
						sb.AppendLine($"{gas.GasSO.Name}: {ratio:P}");
					}
				}
			}

			return $"</i>{sb}<i>";
		}
	}
}