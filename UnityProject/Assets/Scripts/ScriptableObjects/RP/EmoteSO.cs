using System;
using System.Collections.Generic;
using AddressableReferences;
using HealthV2;
using Messages.Server.SoundMessages;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace ScriptableObjects.RP
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/BasicEmote")]
	public class EmoteSO : ScriptableObject
	{
		[Tooltip("Never leave this blank!")]
		[SerializeField]
		protected string emoteName = "";
		public string EmoteName => emoteName;

		[field: SerializeField] public Sprite EmoteIcon;

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

		[Tooltip("Do sounds for this emote work only for specific bodyTypes/Races?")]
		[SerializeField]
		protected bool soundsAreTyped = false;
		[Tooltip("A list of sounds that can be played when this emote happens.")]
		[SerializeField]
		protected List<AddressableAudioSource> defaultSounds = new List<AddressableAudioSource>();
		[SerializeField, ShowIf(nameof(soundsAreTyped))]
		protected List<VoiceType> TypedSounds = new List<VoiceType>();

		[Tooltip("Sound pitch will be randomly chosen from this range.")]
		[SerializeField]
		private Vector2 pitchRange = new Vector2(0.7f, 1f);

		[FormerlySerializedAs("allowedPlayerStates")]
		[Tooltip("Which player states are allowed to use this emote")]
		[SerializeField]
		private PlayerTypes allowedPlayerTypes = PlayerTypes.Normal;

		protected enum FailType
		{
			Normal,
			Critical,
			MouthBlocked
		}

		public virtual void Do(GameObject player)
		{
			if (CheckAllBaseConditions(player) == false) return;
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
			if (audio.Count == 0) return;

			var audioSourceParameters = new AudioSourceParameters(Random.Range(pitchRange.x, pitchRange.y), 100f);
			var audioSource = audio.PickRandom();

			_ = SoundManager.PlayNetworkedAtPosAsync(audioSource, player.AssumedWorldPosServer(),
				audioSourceParameters, sourceObj: player, attachToSource: true);
		}

		/// <summary>
		/// Responsible for making sure the right audio plays
		/// for the correct body type.
		/// </summary>
		protected List<AddressableAudioSource> GetBodyTypeAudio(GameObject player)
		{
			if (player.TryGetComponent<PlayerScript>(out var playerScript) == false) return defaultSounds;
			var bodyType = playerScript.characterSettings.BodyType;
			// Get the player's species
			if (!RaceSOSingleton.TryGetRaceByName(playerScript.characterSettings.Species, out var race)) return defaultSounds;

			VoiceType voiceTypeToUse = new VoiceType();
			foreach (var voice in TypedSounds)
			{
				if(race != voice.VoiceRace) continue;
				voiceTypeToUse = voice;
			}

			List<AddressableAudioSource> GetSounds(BodyType bodyTypeToCheck)
			{
				if (voiceTypeToUse.VoiceDatas != null)
				{
					foreach (var sound in voiceTypeToUse.VoiceDatas)
					{
						if(sound.VoiceSex != bodyTypeToCheck) continue;
						return sound.Sounds;
					}
				}

				return defaultSounds;
			}

			switch (bodyType)
			{
				case (BodyType.Male):
					return GetSounds(BodyType.Male);
				case (BodyType.Female):
					return GetSounds(BodyType.Female);
				default: //for body types other than Male and Female (ex : Non-binary)
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
			//TODO : This sort of thing should be checked on the player script when reworking telecomms and adding a proper silencing system
			if(player.TryGetComponent<PlayerScript>(out var script) == false) return false;
			if (script.Mind.OrNull()?.occupation != null && script.Mind.occupation.JobType == JobType.MIME) return true; //FIXME : Find a way to check if vow of silence is broken
			foreach (var slot in script.Equipment.ItemStorage.GetItemSlots())
			{
				if(slot.IsEmpty) continue;
				if (slot.ItemAttributes.HasTrait(CommonTraits.Instance.Gag)) return true;
			}
			return false;
		}

		protected bool CheckAllBaseConditions(GameObject player)
		{
			if (player.TryGetComponent<PlayerScript>(out var playerScript)
			    && allowedPlayerTypes.HasFlag(playerScript.PlayerType) == false)
			{
				FailText(player, FailType.Normal);
				return false;
			}

			if (playerScript.playerHealth.IsDead)
			{
				return false;
			}

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

	[Serializable]
	public struct VoiceType
	{
		public PlayerHealthData VoiceRace;
		public List<VoiceData> VoiceDatas;
	}

	[Serializable]
	public struct VoiceData
	{
		public BodyType VoiceSex;
		public List<AddressableAudioSource> Sounds;
	}
}
