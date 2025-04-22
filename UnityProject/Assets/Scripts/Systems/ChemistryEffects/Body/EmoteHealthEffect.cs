using System.Collections.Generic;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Items;
using UnityEngine;

namespace Chemistry
{

	[CreateAssetMenu(fileName = "newEmoteHealthEffect", menuName = "ScriptableObjects/Chemistry/EmoteHealthEffect")]
	public class EmoteHealthEffect : Chemistry.Effect
	{
		[Tooltip("Can this emote only trigger on certain body parts? If so, what is the trait for those parts?"),
		 SerializeField]
		private ItemTrait requiredTrait = null;

		[SerializeField] private BodyHealthEmoteEffect.EmoteTypeAndChance EmoteEffect = new();


		public override void Apply(MonoBehaviour sender, float amount)
		{
			if (DMMath.Prob(EmoteEffect.ChancePerTick) == false) return;
			if (sender.TryGetComponent<ItemAttributesV2>(out var attributes) == false) return;
			if (requiredTrait == true && attributes.HasTrait(requiredTrait) == false) return;

			var metabolismComponent = sender as MetabolismComponent;
			if (metabolismComponent == false) return;

			GameObject player = metabolismComponent.RelatedPart.HealthMaster.gameObject;
			if (EmoteEffect.CustomEmote)
			{
				Chat.AddActionMsgToChat(player, "You " + EmoteEffect.CustomEmoterMessage,
					player.GetComponent<PlayerScript>().playerName + " " + EmoteEffect.CustomShownMessage);
			}
			else if (EmoteEffect.Emote != null) EmoteEffect.Emote.Do(player);
		}
	}
}
