using System.Collections.Generic;
using AddressableReferences;
using HealthV2;
using Messages.Server.SoundMessages;
using NaughtyAttributes;
using UnityEngine;

namespace ScriptableObjects.RP
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/BasicEmote")]
	public class EmoteSO : ScriptableObject
	{
		[Tooltip("Never leave this blank!")]
		[SerializeField]
		protected string emoteName = "";
		public string EmoteName => emoteName;

		[Tooltip("Does this emote require the player to have hands that exist and not handcuffed?")]
		[SerializeField]
		protected bool requiresHands;

		[Tooltip("Disallow or change emote behavior if the player is in critical condition.")]
		[SerializeField]
		protected bool allowEmoteWhileInCrit = true;

		[Tooltip("Does this emote require the user's mouth to be free?")]
		[SerializeField]
		protected bool isAudibleEmote;

		[Tooltip("Disallow or change emote behavior if the player is crawling on the ground.")]
		[SerializeField]
		protected bool allowEmoteWhileCrawling = true;

		[Tooltip("The emote text viewed by others around you.")]
		[SerializeField]
		protected string viewText = "did something!";

		[Tooltip("The emote text viewed by you only. Leave blank if you don't want text to appear.")]
		[SerializeField]
		protected string youText = "";

		[Tooltip("If the emote has a special requirment and fails to meet it.")]
		[SerializeField]
		protected string failText = "You were unable to preform this action!";

		[SerializeField, ShowIf(nameof(isAudibleEmote))]
		protected string mouthBlockedText = "You are unable to make a sound!";

		[SerializeField, Tooltip("Emote view text when the character is in critical condition.")]
		protected string critViewText = "screams in pain!";

		[Tooltip("A list of sounds that can be played when this emote happens.")]
		[SerializeField]
		protected List<AddressableAudioSource> defaultSounds = new List<AddressableAudioSource>();

		[Tooltip("A list of sounds for male characters.")]
		[SerializeField]
		protected List<AddressableAudioSource> maleSounds = new List<AddressableAudioSource>();

		[Tooltip("A list of sounds for female characters.")]
		[SerializeField]
		protected List<AddressableAudioSource> femaleSounds = new List<AddressableAudioSource>();

		[Tooltip("Sound pitch will be randomly chosen from this range.")]
		[SerializeField]
		private Vector2 pitchRange = new Vector2(0.7f, 1f);

		protected enum FailType
		{
			Normal,
			Critical,
			MouthBlocked
		}

		public virtual void Do(GameObject player)
		{
			if(CheckAllBaseConditions(player) == false) return;
			Chat.AddActionMsgToChat(player, $"{youText}", $"{player.ExpensiveName()} {viewText}.");
			PlayAudio(defaultSounds, player);
		}


		/// <summary>
		/// Use this instead of rewriting Chat.AddActionMsgToChat() when adding text to a failed conditon.
		/// </summary>
		/// <param name="player">The orignator</param>
		/// <param name="type">Normal : Displays failText string. Critical: Displays critViewText string.</param>
		protected void FailText(GameObject player, FailType type)
		{
			switch (type)
			{
				case FailType.Normal:
					Chat.AddActionMsgToChat(player, $"{failText}", "");
					break;
				case FailType.Critical:
					Chat.AddActionMsgToChat(player, $"{player.ExpensiveName()} {critViewText}.", $"{player.ExpensiveName()} {critViewText}.");
					break;
				case FailType.MouthBlocked:
					Chat.AddExamineMsg(player, mouthBlockedText);
					break;
			}
		}

		protected void PlayAudio(List<AddressableAudioSource> audio, GameObject player)
		{
			//If there is no audio in the audio list, exit out of this function.
			if (audio.Count == 0)
			{
				Logger.LogWarning("[EmoteSO/" + $"{name}] - " + "No audio files detected!.");
				return;
			}

			var audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(pitchRange.x, pitchRange.y));

			SoundManager.PlayNetworkedAtPos(
				audio.PickRandom(),
				player.AssumedWorldPosServer(),
				audioSourceParameters,
				true);
		}

		/// <summary>
		/// Responsible for making sure the right audio plays
		/// for the correct body type.
		/// </summary>
		protected List<AddressableAudioSource> GetBodyTypeAudio(GameObject player)
		{
			player.TryGetComponent<LivingHealthMasterBase>(out var health);
			var bodyType = health.OrNull()?.BodyType;

			//Add race checks later when lizard men, slime people and lusty xeno-maids get added after the new health system gets merged.
			switch (bodyType)
			{
				case (BodyType.Male):
					return maleSounds;
				case (BodyType.Female):
					return femaleSounds;
				default:
					return defaultSounds;
			}
		}

		/// <summary>
		/// Checks if the player has arms and if they're cuffed or not.
		/// </summary>
		protected bool CheckHandState(GameObject player)
		{
			return !player.GetComponent<PlayerScript>().playerMove.IsCuffed && Validations.HasBothHands(player);
		}

		/// <summary>
		/// Checks if the player is in critical condition or not
		/// </summary>
		/// <param name="player">The player that you want to check their health.</param>
		/// <returns>True if crit, false otherwise.</returns>
		protected bool CheckPlayerCritState(GameObject player)
		{
			var health = player.GetComponent<LivingHealthMasterBase>();
			if (health == null || health.IsCrit || health.IsSoftCrit)
			{
				return true;
			}

			return false;
		}

		protected bool CheckIfPlayerIsCrawling(GameObject player)
		{
			return player.GetComponent<RegisterPlayer>().IsLayingDown;
		}

		/// <summary>
		/// If an item is blocking this player from making audible emotes is a mime.
		/// having no mask slot should also count as not having a mouth therefore you can't use an emote (like screaming) that requires a mouth
		/// </summary>
		protected static bool CheckIfPlayerIsGagged(GameObject player)
		{
			Logger.Log("Checking if player is gagged");
			//TODO : This sort of thing should be checked on the player script when reworking telecomms and adding a proper silencing system
			if(player.TryGetComponent<PlayerScript>(out var script) == false) return false;
			if (script.mind.occupation.JobType == JobType.MIME) return true; //FIXME : Find a way to check if vow of silence is broken
			foreach (var slot in script.Equipment.ItemStorage.GetItemSlots())
			{
				if(slot.IsEmpty) continue;
				if (slot.ItemAttributes.HasTrait(CommonTraits.Instance.Gag)) return true;
			}
			return false;
		}

		protected bool CheckAllBaseConditions(GameObject player)
		{
			if (allowEmoteWhileInCrit == false && CheckPlayerCritState(player))
			{
				FailText(player, FailType.Critical);
				return false;
			}
			if ((allowEmoteWhileCrawling == false && CheckIfPlayerIsCrawling(player))
			    || (requiresHands && CheckHandState(player) == false))
			{
				FailText(player, FailType.Normal);
				return false;
			}
			if (isAudibleEmote && CheckIfPlayerIsGagged(player))
			{
				FailText(player, FailType.MouthBlocked);
				return false;
			}

			return true;
		}
	}
}
