using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Objects.Engineering;
using US13.Systems.Electricity.Interfaces;

namespace US13.Objects.Pipes.Devices
{
	public class GasDigitalValve : MonoPipe, IAPCPowerable
	{
		[SerializeField]
		private SpriteHandler spriteHandlerValve = null;

		[SerializeField]
		private bool isOn = false;

		private PowerState powerState;

		public override void OnSpawnServer(SpawnInfo info)
		{
			UpdateSprite();

			base.OnSpawnServer(info);
		}

		public override void HandApplyInteraction(HandApply interaction)
		{
			ToggleState();
		}

		public override void AiInteraction(AiActivate interaction)
		{
			ToggleState();
		}

		private void ToggleState()
		{
			isOn = !isOn;

			if (powerState == PowerState.Off) return;

			UpdateSprite();
		}

		private void UpdateSprite()
		{
			if (isOn)
			{
				spriteHandlerValve.SetCatalogueIndexSprite((int)DigitalValveSprites.On);
			}
			else
			{
				spriteHandlerValve.SetCatalogueIndexSprite((int)DigitalValveSprites.Off);
			}
		}

		public override void TickUpdate()
		{
			if (isOn == false) return;

			if (powerState == PowerState.Off) return;

			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.ConnectedPipes);
		}

		public void PowerNetworkUpdate(float voltage)
		{
			//Voltage data not needed
		}

		public void StateUpdate(PowerState state)
		{
			powerState = state;

			if (powerState == PowerState.Off)
			{
				spriteHandlerValve.SetCatalogueIndexSprite((int)DigitalValveSprites.Unpowered);
				return;
			}

			UpdateSprite();
		}

		private enum DigitalValveSprites
		{
			Unpowered,
			Off,
			On
		}
	}
}
