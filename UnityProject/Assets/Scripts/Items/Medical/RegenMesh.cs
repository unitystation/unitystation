using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Items.Medical
{
	public class RegenMesh : HealsTheLiving, IInteractable<HandActivate>
	{
		private bool isOpen = false;
		private SpriteDataSO lastOpenSprite;

		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private SpriteDataSO closedSprite;

		private void Awake()
		{
			if (spriteHandler == null) spriteHandler = GetComponentInChildren<SpriteHandler>();
			lastOpenSprite = spriteHandler.GetCurrentSpriteSO();
		}

		public override void ServerPerformInteraction(HandApply interaction)
		{
			if (isOpen == false)
			{
				Chat.AddExamineMsg(interaction.Performer, $"You need to open the {gameObject.ExpensiveName()} first before using it!");
				return;
			}
			base.ServerPerformInteraction(interaction);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (isOpen) lastOpenSprite = spriteHandler.GetCurrentSpriteSO();
			isOpen = !isOpen;
			spriteHandler.SetSpriteSO(isOpen ? lastOpenSprite : closedSprite);
		}
	}

}
