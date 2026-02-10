using Mirror;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Managers;
using US13.UI.Core.RightClick;
using Util;

namespace US13.Items
{
	public class HandPreparable : NetworkBehaviour, ICheckedInteractable<HandActivate>, IRightClickable
	{
		[SyncVar] private bool isPrepared = false;
		public bool IsPrepared => isPrepared;


		[SerializeField] protected SpriteHandler spriteHandler;
		[SerializeField] protected bool destroyThisComponentOnOpen = true;
		[Header("Feedback")]
		[SerializeField] private AddressableAudioSource openingNoise;
		[SerializeField] protected SpriteDataSO openedSprite;
		[Header("Chat Stuff")]
		[SerializeField] private string openingVerb = "open";
		public string openingRequirementText = "You need to open this first before using it!";

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Open();
			Chat.AddExamineMsg(interaction.Performer, $"You {openingVerb} the {gameObject.ExpensiveName()}");
		}

		public virtual void Open()
		{
			isPrepared = true;
			if (openingNoise != null) SoundManager.PlayNetworkedAtPos(openingNoise, gameObject.AssumedWorldPosServer());
			if (openedSprite != null) spriteHandler.SetSpriteSO(openedSprite);
			if (destroyThisComponentOnOpen) Destroy(this);
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			var rightClickResult = new RightClickableResult();
			if (isPrepared) return rightClickResult;
			rightClickResult.AddElement($"{openingVerb}", Open);
			return rightClickResult;
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return isPrepared ? false : true;
		}
	}
}