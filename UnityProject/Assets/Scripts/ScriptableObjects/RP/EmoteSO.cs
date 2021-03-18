using System.Collections.Generic;
using AddressableReferences;
using HealthV2;
using Messages.Server.SoundMessages;
using UnityEngine;
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

		[Tooltip("Does this emote require the player to have hands that exist and not handcuffed?")]
		[SerializeField]
		protected bool requiresHands = false;

		[Tooltip("The emote text viewed by others around you.")]
		[SerializeField]
		protected string viewText = "did something!";

		[Tooltip("The emote text viewed by you only. Leave blank if you don't want text to appear.")]
		[SerializeField]
		protected string youText = "";

		[Tooltip("If the emote has a special requirment and fails to meet it.")]
		[SerializeField]
		protected string failText = "You were unable to preform this action!";

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

		public virtual void Do(GameObject player)
		{
			if(requiresHands && CheckHandState(player) == false)
			{
				Chat.AddActionMsgToChat(player, $"{failText}", $"");
			}
			else
			{
				Chat.AddActionMsgToChat(player, $"{youText}", $"{player.ExpensiveName()} {viewText}.");
				PlayAudio(defaultSounds, player);
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
				case (BodyType.NonBinary):
					return defaultSounds;
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
	}
}
