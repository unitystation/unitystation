using Systems.Interaction;
using UnityEngine;

namespace Objects.Atmospherics
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
				spriteHandlerValve.ChangeSprite((int)ManualValveSprites.On);
			}
			else
			{
				spriteHandlerValve.ChangeSprite((int)ManualValveSprites.Off);
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