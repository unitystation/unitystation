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
		[SerializeField, Tooltip("GlowSprite's handlers cannot be automatically fetched from Awake()")]
		private SpriteHandler glowHandler;
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
			if(glowHandler != null) glowHandler.SetActive(false);
			if(actionButton == null) return;
			actionButton.ServerActionClicked += Open;
		}

		public override void Open()
		{
			lightControl.Toggle(true);
			if(runsOutOverTime) LightsOutAfterTime(lightControl, spriteHandler);
			if(glowHandler != null) glowHandler.SetActive(true);
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