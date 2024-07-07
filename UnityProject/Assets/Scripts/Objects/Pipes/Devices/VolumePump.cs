using Messages.Server;
using Systems.Atmospherics;
using UnityEngine;
using Systems.Interaction;
using Systems.Pipes;

namespace Objects.Atmospherics
{
	public class VolumePump : MonoPipe
	{
		public SpriteHandler spriteHandlerOverlay = null;
		public SpriteHandler spriteHandlerOverclockedOverlay = null;

		public float MaxPressure = 9000f;
		public float MinPressure = 10f;
		public float MaxVolume = 200f;
		public float TransferVolume = 200f;

		public bool IsOn = false;
		private bool isOverclocked = false;

		private MetaDataLayer metaDataLayer;
		private MetaDataNode metaNode;

		public override void OnSpawnServer(SpawnInfo info)
		{
			if (IsOn)
			{
				UpdateOverclockOverlay();
			}
			else
			{
				spriteHandlerOverclockedOverlay.PushClear();
				spriteHandlerOverlay.PushClear();
			}

			metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
			metaNode = metaDataLayer.Get(registerTile.LocalPositionServer);

			base.OnSpawnServer(info);
		}

		public override void HandApplyInteraction(HandApply interaction)
		{
			if (interaction.IsAltClick)
			{
				TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.VolumePump, TabAction.Open);
			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				Chat.AddExamineMsg(interaction.Performer, isOverclocked ? "The pump quiets down as you turn its limiters back on." : "The pump makes a grinding noise and air starts to hiss out as you disable its pressure limits.");
				isOverclocked = !isOverclocked;
				if (IsOn)
				{
					UpdateOverclockOverlay();
				}
			}
			else
			{
				ToggleState();
			}
		}

		//Ai interaction
		public override void AiInteraction(AiActivate interaction)
		{
			if (interaction.ClickType == AiActivate.ClickTypes.AltClick)
			{
				TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.VolumePump, TabAction.Open);
			}
			else
			{
				ToggleState();
			}
		}

		private void ToggleState()
		{
			IsOn = !IsOn;
			if (IsOn)
			{
				UpdateOverclockOverlay();
			}
			else
			{
				spriteHandlerOverclockedOverlay.PushClear();
				spriteHandlerOverlay.PushClear();
			}
		}

		private void UpdateOverclockOverlay()
		{
			if (isOverclocked)
			{
				spriteHandlerOverclockedOverlay.PushTexture();
				spriteHandlerOverlay.PushClear();
			}
			else
			{
				spriteHandlerOverclockedOverlay.PushClear();
				spriteHandlerOverlay.PushTexture();
			}
		}

		public override void TickUpdate()
		{
			if (IsOn == false)
			{
				return;
			}
			
			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);

			PipeData inputPipe = pipeData.Connections.GetFlagToDirection(FlagLogic.InputOne)?.Connected;

			if (inputPipe == null) return;
			
			Vector2 pressureDensity = pipeData.mixAndVolume.Density();
			
			if (isOverclocked == false && pressureDensity.x < MinPressure && pressureDensity.y < MinPressure) return;
			if (isOverclocked == false && pressureDensity.x > MaxPressure && pressureDensity.y > MaxPressure) return;

			var inputMix = inputPipe.GetMixAndVolume;
			var inputDensity = inputMix.Density();
			
			float chemVolumeRatio = Mathf.Min(TransferVolume / AtmosUtils.CalcVolume(inputDensity.x, inputMix.Total.x, inputMix.Temperature), 1);
			float gasVolumeRatio = Mathf.Min(TransferVolume / AtmosUtils.CalcVolume(inputDensity.y, inputMix.Total.y, inputMix.Temperature), 1);
			
			Vector2 transferValue = new Vector2
			{
				x = (pressureDensity.x < MinPressure || pressureDensity.x > MaxPressure) && isOverclocked == false ? 0 : FiniteOrDefault(inputMix.Total.x * chemVolumeRatio),
				y = (pressureDensity.y < MinPressure || pressureDensity.y > MaxPressure) && isOverclocked == false ? 0 : FiniteOrDefault(inputMix.Total.y * gasVolumeRatio)
			};
			
			inputPipe.GetMixAndVolume.TransferTo(pipeData.mixAndVolume, transferValue);

			if (isOverclocked)
			{
				if (metaNode == null || metaNode.Exists == false)
				{
					metaNode = metaDataLayer.Get(registerTile.LocalPositionServer);
				}
				GasMix.TransferGas(metaNode.GasMixLocal, pipeData.mixAndVolume.GetGasMix(), FiniteOrDefault(transferValue.y * 0.1f));
			}
		}
		
		public static float FiniteOrDefault(float value)
		{
		    return float.IsNaN(value) == false && float.IsInfinity(value) == false ? value : default;
		}
	}	
}