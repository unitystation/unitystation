using Systems.Electricity;
using Systems.Interaction;
using UnityEngine;

namespace Objects.Atmospherics
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
				spriteHandlerValve.ChangeSprite((int)DigitalValveSprites.On);
			}
			else
			{
				spriteHandlerValve.ChangeSprite((int)DigitalValveSprites.Off);
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
				spriteHandlerValve.ChangeSprite((int)DigitalValveSprites.Unpowered);
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
