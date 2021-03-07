using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using Random = UnityEngine.Random;
using SoundMessages;

[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/BasicEmote")]
public class EmoteSO : ScriptableObject
{
	[Tooltip("Never leave this blank!")]
	public string emoteName = "";

	[SerializeField, Tooltip("The emote text viewed by others around you.")]
	private string viewText = "did something!";

	[SerializeField, Tooltip("The emote text viewed by you only. Leave blank if you don't want text to appear.")]
	private string youText = "";

	[SerializeField, Tooltip("A list of sounds that can be played when this emote happens.")]
	private List<AddressableAudioSource> emoteDefaultSounds = new List<AddressableAudioSource>();

	[SerializeField, Range(0.1f, 2)]
	private RangeAttribute pitchRange = new RangeAttribute(0.7f, 1f);

	public virtual void Do(GameObject player)
	{
		Chat.AddActionMsgToChat(player, $"{youText}", $"{player.ExpensiveName()} {viewText}.");
		playAudio(emoteDefaultSounds, player);
	}

	private void playAudio(List<AddressableAudioSource> audio, GameObject player)
	{
		List<AddressableAudioSource> audioList = audio;

		//If there is no audio in the audio list, exit out of this function.
		if (audioList.Count == 0)
		{
			Debug.LogWarning("[EmoteSO/" + $"{this.name}]" + "No audio files detected! No sounds will be played.");
			return;
		}

		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(pitchRange.min, pitchRange.max));
		SoundManager.PlayNetworkedAtPos(audioList.PickRandom(), player.transform.position, audioSourceParameters, polyphonic: true);
	}

	private Gender checkPlayerGender()
	{
		return PlayerManager.LocalPlayerScript.characterSettings.Gender;
	}
}
