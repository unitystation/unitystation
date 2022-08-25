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
				spriteHandlerValve.ChangeSprite(1);
			}
			else
			{
				spriteHandlerValve.ChangeSprite(0);
			}
		}

		public override void TickUpdate()
		{
			if (isOn == false) return;

			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.ConnectedPipes);
		}
	}
}