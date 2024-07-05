using Systems.Atmospherics;
using UnityEngine;
using Systems.Interaction;
using Systems.Pipes;

namespace Objects.Atmospherics
{
	public class VolumePump : MonoPipe
	{
		public SpriteHandler spriteHandlerOverlay = null;

		public float MaxPressure = 9000f;
		public float TransferVolume = 200f;

		public bool IsOn = false;

		public override void OnSpawnServer(SpawnInfo info)
		{
			if (IsOn)
			{
				spriteHandlerOverlay.PushTexture();
			}
			else
			{
				spriteHandlerOverlay.PushClear();
			}

			base.OnSpawnServer(info);
		}

		public override void HandApplyInteraction(HandApply interaction)
		{
			ToggleState();
		}

		//Ai interaction
		public override void AiInteraction(AiActivate interaction)
		{
			ToggleState();
		}

		private void ToggleState()
		{
			IsOn = !IsOn;
			if (IsOn)
			{
				spriteHandlerOverlay.PushTexture();
			}
			else
			{
				spriteHandlerOverlay.PushClear();
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
			
			if (pressureDensity.x > MaxPressure && pressureDensity.y > MaxPressure) return;

			var inputMix = inputPipe.GetMixAndVolume;
			var inputDensity = inputMix.Density();
			
			float chemVolumeRatio = TransferVolume / AtmosUtils.CalcVolume(inputDensity.x, inputMix.Total.x, inputMix.Temperature);
			float gasVolumeRatio = TransferVolume / AtmosUtils.CalcVolume(inputDensity.y, inputMix.Total.y, inputMix.Temperature);
			
			Vector2 transferValue = new Vector2
			{
				x = pressureDensity.x > MaxPressure ? 0 : FiniteOrDefault(inputMix.Total.x * chemVolumeRatio),
				y = pressureDensity.y > MaxPressure ? 0 : FiniteOrDefault(inputMix.Total.y * gasVolumeRatio)
			};
			
			inputPipe.GetMixAndVolume.TransferTo(pipeData.mixAndVolume, transferValue);
		}
		
		public static float FiniteOrDefault(float value)
		{
		    return float.IsNaN(value) == false && float.IsInfinity(value) == false ? value : default;
		}
	}	
}