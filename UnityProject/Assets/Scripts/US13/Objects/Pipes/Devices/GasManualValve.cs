using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;

namespace US13.Objects.Pipes.Devices
{
	public class GasManualValve : MonoPipe
	{
		[SerializeField]
		private SpriteHandler spriteHandlerValve = null;

		[SerializeField]
		private bool isOn = false;

		public override void OnSpawnServer(SpawnInfo info)
		{
			UpdateSprite();

			base.OnSpawnServer(info);
		}

		public override void HandApplyInteraction(HandApply interaction)
		{
			ToggleState();
		}

		private void ToggleState()
		{
			isOn = !isOn;

			UpdateSprite();
		}

		private void UpdateSprite()
		{
			if (isOn)
			{
				spriteHandlerValve.SetCatalogueIndexSprite((int)ManualValveSprites.On);
			}
			else
			{
				spriteHandlerValve.SetCatalogueIndexSprite((int)ManualValveSprites.Off);
			}
		}

		public override void TickUpdate()
		{
			if (isOn == false) return;

			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.ConnectedPipes);
		}

		private enum ManualValveSprites
		{
			Off,
			On
		}
	}
}