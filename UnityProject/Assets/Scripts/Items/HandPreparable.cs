using AddressableReferences;
using Mirror;
using UnityEngine;

namespace Items
{
	public class HandPreparable : NetworkBehaviour, ICheckedInteractable<HandActivate>, IRightClickable
	{
		[SyncVar] private bool isPrepared = false;
		public bool IsPrepared => isPrepared;


		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private bool destroyThisComponentOnOpen = true;
		[Header("Feedback")]
		[SerializeField] private AddressableAudioSource openingNoise;
		[SerializeField] private SpriteDataSO openedSprite;
		[Header("Chat Stuff")]
		[SerializeField] private string openingVerb = "open";
		public string openingRequirementText = "You need to open this first before using it!";

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Open();
			Chat.AddExamineMsg(interaction.Performer, $"You {openingVerb} the {gameObject.ExpensiveName()}");
		}

		public void Open()
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
			rightClickResult.AddElement("Open This", Open);
			return rightClickResult;
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return isPrepared ? false : true;
		}
	}
}