using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AddressableReferences;
using Audio.Managers;

namespace Audio.Containers
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioClipsArray enteringSoundTrack = null;
	    [SerializeField] private AudioClipsArray leavingSoundTrack = null;

        private AddressableAudioSource playing;

        private static AudioManager audioManager;
        public static AudioManager Instance
        {
            get
            {
                if (audioManager == null)
                {
                    audioManager = FindObjectOfType<AudioManager>();
                }

                return audioManager;
            }
        }

        [NaughtyAttributes.Button("Play Random Music")]
        private void DEBUG_PlayRandomMusic()
        {
            MusicManager.Instance.PlayRandomTrack();
        }

        [NaughtyAttributes.Button("Stop Music")]
        private void DEBUG_StopMusic()
        {
            MusicManager.StopMusic();
        }

        [NaughtyAttributes.Button("Play Entering Sound Track")]
        private void DEBUG_PlayEnteringSound()
        {
            if (enteringSoundTrack == null) return;

		SoundAmbientManager.StopAudio(playing);
		playing = enteringSoundTrack.AddressableAudioSource.GetRandom();
		SoundAmbientManager.PlayAudio(enteringSoundTrack.AddressableAudioSource.GetRandom());
        }

        [NaughtyAttributes.Button("Play Exiting Sound Track")]
        private void DEBUG_PlayExitingSound()
        {
            if (leavingSoundTrack == null) return;

		SoundAmbientManager.StopAudio(playing);
		playing = leavingSoundTrack.AddressableAudioSource.GetRandom();
		SoundAmbientManager.PlayAudio(leavingSoundTrack.AddressableAudioSource.GetRandom());
        }

        [NaughtyAttributes.Button("Stop Sound Track")]
        private void DEBUG_StopSound()
        {
            if (enteringSoundTrack == null) return;

		SoundAmbientManager.StopAudio(playing);
        }
    }
}
