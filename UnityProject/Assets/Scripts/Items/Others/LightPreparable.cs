using System;
using System.Threading.Tasks;
using NaughtyAttributes;
using UI.Action;
using UnityEngine;

namespace Items.Others
{
	[RequireComponent(typeof(ItemLightControl))]
	public class LightPreparable : HandPreparable
	{
		private ItemLightControl lightControl;
		private SpriteDataSO offSprite;
		[SerializeField]
		private bool runsOutOverTime = false;
		[SerializeField, ShowIf(nameof(runsOutOverTime))]
		private float runsOutOverSeconds = 356;
		[SerializeField] private ItemActionButton actionButton;


		private void Awake()
		{
			lightControl = GetComponent<ItemLightControl>();
			if (runsOutOverTime)
			{
				destroyThisComponentOnOpen = false;
				offSprite = spriteHandler.GetCurrentSpriteSO();
			}
			if(actionButton == null) return;
			actionButton.ServerActionClicked += Open;
		}

		public override void Open()
		{
			lightControl.Toggle(true);
			if(runsOutOverTime) LightsOutAfterTime(lightControl, spriteHandler);
			actionButton.ServerActionClicked -= Open;
			base.Open();
		}

		private async void LightsOutAfterTime(ItemLightControl lightController, SpriteHandler handler)
		{
			await Task.Delay((int)runsOutOverSeconds * 1000);
			lightController.Toggle(false);
			handler.SetSpriteSO(offSprite);
		}
	}
}