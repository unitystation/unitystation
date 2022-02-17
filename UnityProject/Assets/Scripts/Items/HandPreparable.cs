using AddressableReferences;
using UnityEngine;

namespace Items
{
	public class HandPreparable : MonoBehaviour, ICheckedInteractable<HandActivate>, IRightClickable
	{
		private bool isPrepared = false;
		public bool IsPrepared => isPrepared;


		[SerializeField] private SpriteHandler spriteHandler;
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
			Destroy(this);
		}

		/// <summary>
		/// Generates a right click button for items like Space Cola where another script gets in the way of HandActivate
		/// </summary>
		/// <returns></returns>
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