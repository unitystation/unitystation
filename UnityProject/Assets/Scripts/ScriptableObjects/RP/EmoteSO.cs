using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using Random = UnityEngine.Random;
using Messages.Server.SoundMessages;

[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/BasicEmote")]
public class EmoteSO : ScriptableObject
{
	//All these are public for a reason!
	//They cannot be accssed from classes that inheirt them if they're private!

	[Tooltip("Never leave this blank!")]
	public string emoteName = "";

	[Tooltip("Does this emote require the player to have hands that exist and not handcuffed?")]
	public bool requiresHands = false;

	[Tooltip("The emote text viewed by others around you.")]
	public string viewText = "did something!";

	[Tooltip("The emote text viewed by you only. Leave blank if you don't want text to appear.")]
	public string youText = "";

	[Tooltip("If the emote has a special requirment and fails to meet it.")]
	public string failText = "You were unable to preform this action!";

	[Tooltip("A list of sounds that can be played when this emote happens.")]
	public List<AddressableAudioSource> defaultSounds = new List<AddressableAudioSource>();

	[Tooltip("A list of sounds for male characters.")]
	public List<AddressableAudioSource> maleSounds = new List<AddressableAudioSource>();

	[Tooltip("A list of sounds for female characters.")]
	public List<AddressableAudioSource> femaleSounds = new List<AddressableAudioSource>();

	[SerializeField, Range(0.1f, 2)]
	private RangeAttribute pitchRange = new RangeAttribute(0.7f, 1f); //This doesn't want to appear in the inspector for some reason.

	[HideInInspector]
	public List<AddressableAudioSource> audioToUse;

	public virtual void Do(GameObject player)
	{
		if(requiresHands == true && CheckHandState(player) == false)
		{
			Chat.AddActionMsgToChat(player, $"{failText}", $"");
		}
		else
		{
			Chat.AddActionMsgToChat(player, $"{youText}", $"{player.ExpensiveName()} {viewText}.");
			playAudio(defaultSounds, player);
		}
	}

	public void playAudio(List<AddressableAudioSource> audio, GameObject player)
	{
		List<AddressableAudioSource> audioList = audio;

		//If there is no audio in the audio list, exit out of this function.
		if (audioList.Count == 0)
		{
			Debug.LogWarning("[EmoteSO/" + $"{this.name}] - " + "No audio files detected!.");
			return;
		}

		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(pitchRange.min, pitchRange.max));
		SoundManager.PlayNetworkedAtPos(audioList.PickRandom(), player.transform.position, audioSourceParameters, polyphonic: true);
	}

	public void genderCheck(BodyType gender)
	{
		//Add race checks later when lizard men, slime people and lusty xeno-maids get added after the new health system gets merged.
		switch (gender)
		{
			case (BodyType.Male):
				audioToUse = maleSounds;
				break;
			case (BodyType.Female):
				audioToUse = femaleSounds;
				break;
			case (BodyType.Neutral):
				audioToUse = defaultSounds;
				break;
			default:
				audioToUse = defaultSounds;
				break;
		}
	}


	public bool CheckHandState(GameObject player)
	{
		if (!PlayerManager.LocalPlayerScript.playerMove.IsCuffed)
		{
			return true;
		}
		return Validations.HasHand(player);
	}


	public BodyType checkPlayerGender()
	{
		return PlayerManager.LocalPlayerScript.characterSettings.BodyType;
	}

	public PlayerHealthV2 getPlayerHealth()
	{
		return PlayerManager.LocalPlayerScript.playerHealth;
	}
}
