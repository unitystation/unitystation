using HealthV2;
using Logs;
using UnityEngine;
using Mobs.BrainAI.States.SimpleBot;
using Systems.Clothing;

namespace Items.Implants.Organs
{
	public class SimpleBotBody : BodyPartFunctionality
	{
		private SimpleBotTaskAi _taskAi = null;
		private int _currentSpriteIndex = 0;

		[SerializeField, Tooltip("Sprites are: (0) Regular-Idle, (1) Regular-Performing,\n (2) Emagged-Idle, (3) Emagged-Performing")]
		private SpriteDataSO[] possibleSprites = new SpriteDataSO[4];

		public override void OnAddedToBody(LivingHealthMasterBase addedToBody)
		{
			base.OnAddedToBody(addedToBody);

			if (LivingHealthMaster.brain == false)
			{
				Loggy.Error($"SimpleBotBody/OnAddedToBody(): Could not find brain on LivingHealthMasterBase 'addedToBody'");
				return;
			}
			if (LivingHealthMaster.brain.TryGetComponent<SimpleBotTaskAi>(out _taskAi) == false)
			{
				Loggy.Error($"SimpleBotBody/OnAddedToBody(): Could not find SimpleBotTaskAi script on LivingHealthMaster Brain");
				return;
			}

			_taskAi.OnSpriteChange += UpdateBodySprites;
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase addedToBody, GameObject source = null)
		{
			base.OnRemovedFromBody(addedToBody, source);

			if (_taskAi == false) return;
			_taskAi.OnSpriteChange -= UpdateBodySprites;
		}

		private void UpdateBodySprites(bool isEmagged, bool isPerformingTask)
		{

			_currentSpriteIndex = isEmagged ? 2 : 0;
			_currentSpriteIndex += isPerformingTask ? 1 : 0;

			RelatedPart.RelatedPresentSprites[0].UpdateSpritesForImplant(RelatedPart, ClothingHideFlags.HIDE_NONE, possibleSprites[_currentSpriteIndex], RelatedPart.SpritePrefab.SpriteOrder);
		}
	}
}
