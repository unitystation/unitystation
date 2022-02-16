using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Items.Medical
{
	public class RegenMesh : HealsTheLiving, IInteractable<InventoryApply>
	{
		private bool isOpen = false;

		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private SpriteDataSO closedSprite;
		[SerializeField] private SpriteDataSO openedSprite;
		public override void ServerPerformInteraction(HandApply interaction)
		{
			if (isOpen == false)
			{
				Chat.AddExamineMsg(interaction.Performer, $"You need to open the {gameObject.ExpensiveName()} first before using it!");
				return;
			}
			base.ServerPerformInteraction(interaction);
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			isOpen = !isOpen;
			spriteHandler.SetSpriteSO(isOpen ? openedSprite : closedSprite);
		}
	}

}
