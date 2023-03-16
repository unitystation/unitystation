using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using ScriptableObjects.RP;

[CreateAssetMenu(fileName = "BodyHealthEmoteEffect",
	menuName = "ScriptableObjects/Chemistry/Reactions/BodyHealthEmoteEffect")]
public class BodyHealthEmoteEffect : BodyHealthEffect
{

	public List<EmoteTypeAndChance> EmoteEffects = new List<EmoteTypeAndChance>();

	[System.Serializable]
	public struct EmoteTypeAndChance
	{
		public bool CustomEmote;
		[Tooltip("the message only the person doing the emote can see")]
		[ShowIf(nameof(CustomEmote))] [AllowNesting] public string CustomEmoterMessage;
		[Tooltip("the emote those who are watching can see")]
		[ShowIf(nameof(CustomEmote))] [AllowNesting] public string CustomShownMessage;
		[HideIf("CustomEmote")] [AllowNesting] public EmoteSO Emote;
		[Tooltip("Chance this action will happen every tick, first in the list rolls first")]
		[Range(0, 100)] [AllowNesting] public int ChancePerTick;
		[ShowIf(nameof(CanOverdose))] [AllowNesting] public bool StopIfOverdosed;
	}

	public override void PossibleReaction(List<MetabolismComponent> senders, ReagentMix reagentMix,
		float reactionMultiple, float BodyReactionAmount, float TotalChemicalsProcessed, out bool overdose) //limitedReactionAmountPercentage = 0 to 1
	{

		base.PossibleReaction(senders, reagentMix, reactionMultiple, BodyReactionAmount, TotalChemicalsProcessed, out overdose);

		foreach (EmoteTypeAndChance emote in EmoteEffects)
		{
					//Check if there are organs to act on
					if (senders.Count == 0) { break; }
			GameObject player = senders[0].RelatedPart.HealthMaster.gameObject;
			if (emote.StopIfOverdosed == true && overdose == true) { continue; }

			if (Random.Range(0, 100) <= emote.ChancePerTick)
			{
				if (emote.CustomEmote == true)
				{
					Chat.AddActionMsgToChat(player, "You " + emote.CustomEmoterMessage,
						player.GetComponent<PlayerScript>().playerName + " " + emote.CustomShownMessage);
					break;
				}
				else if (emote.Emote != null)
				{
					emote.Emote.Do(player);
					break;
				}
			}
		}
	}
}
