using System;
using System.Collections;
using NaughtyAttributes;
using Systems.Explosions;
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
		[SerializeField, Tooltip("Create a spark when activating this light source?")]
		private bool sparkOnOpen = false;
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

		private void OnDisable()
		{
			StopCoroutine(LightsOutAfterTime(lightControl, spriteHandler));
		}

		public override void Open()
		{
			lightControl.Toggle(true);
			if(sparkOnOpen) SparkUtil.TrySpark(gameObject);
			if(runsOutOverTime) StartCoroutine(LightsOutAfterTime(lightControl, spriteHandler));
			if(glowHandler != null) glowHandler.SetActive(true);
			actionButton.ServerActionClicked -= Open;
			base.Open();
		}

		private IEnumerator LightsOutAfterTime(ItemLightControl lightController, SpriteHandler handler)
		{
			yield return WaitFor.Seconds(runsOutOverSeconds);
			lightController.Toggle(false);
			handler.SetSpriteSO(offSprite);
		}
	}
}