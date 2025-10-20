using System;
using System.Collections;
using UnityEngine;
using NaughtyAttributes;
using Core.Editor.Attributes;
using AddressableReferences;
using Audio.Managers;
using Messages.Server;
using Messages.Server.SoundMessages;
using Mirror;

namespace Doors
{
	public class DoorSoundController : NetworkBehaviour
	{
		[SerializeField, Tooltip("Sound that plays when opening this door")]
		private AddressableAudioSource openingSFX;
		[SerializeField, Tooltip("Sound that plays when closing this door")]
		private AddressableAudioSource closingSFX;
        [SerializeField, Tooltip("Sound that plays when closing this door")]
		private AddressableAudioSource forcedSFX;
		[SerializeField, Tooltip("Sound that plays when access is denied by this door")]
		private AddressableAudioSource deniedSFX;
		[SerializeField, Tooltip("Sound that plays when pressure warning is played by this door")]
		private AddressableAudioSource warningSFX;
        [SerializeField, Tooltip("Sound that plays when prying open this door with tools")] 
        private AddressableAudioSource toolPrySFX;
        [SerializeField, Tooltip("Sound that plays when prying open this door with claws")] 
        private AddressableAudioSource handPrySFX;
        [SerializeField, Tooltip("Sound that plays when door bolts engage")] 
        private AddressableAudioSource boltsDownSFX;
        [SerializeField, Tooltip("Sound that plays when door bolts disengage")] 
        private AddressableAudioSource boltsUpSFX;

        private DoorMasterController doorMasterController;

        private string soundGuid = "";

        private void Awake()
		{
			doorMasterController = this.GetComponent<DoorMasterController>();
		}

        public enum DoorSoundType
		{
			Open,
			Close,
            Forced,
			AccessDenied,
			PressureWarn,
            ToolPry,
            HandPry,
            BoltsDown,
            BoltsUp,
		}

        public void PlaySound(DoorSoundType sound)
        {
            StopSound();
            AddressableAudioSource toPlay = getSound(sound);

            if (toPlay != null)
            {
                soundGuid = Guid.NewGuid().ToString();
                _ = SoundManager.PlayAtPosition(toPlay, gameObject.AssumedWorldPosServer(), gameObject, soundGuid);
            }

        }
                
        public void ServerPlaySound(DoorSoundType sound)
        {
            StopSound();
            AddressableAudioSource toPlay = getSound(sound);
            
            if (toPlay != null)
            {
                soundGuid = Guid.NewGuid().ToString();
                _ = SoundManager.PlayNetworkedAtPosAsync(toPlay, gameObject.AssumedWorldPosServer(), gameObject, soundGuid);
            }
            
        }

        private AddressableAudioSource getSound(DoorSoundType sound)
        {
            AddressableAudioSource toPlay = null;
            switch (sound)
            {
                case DoorSoundType.Open:
                    toPlay = openingSFX;
                    break;
                case DoorSoundType.Close:
                    toPlay = closingSFX;
                    break;
                case DoorSoundType.Forced:
                    toPlay = forcedSFX;
                    break;
                case DoorSoundType.AccessDenied:
                    toPlay = deniedSFX;
                    break;
                case DoorSoundType.PressureWarn:
                    toPlay = warningSFX;
                    break;
                case DoorSoundType.ToolPry:
                    toPlay = toolPrySFX;
                    break;
                case DoorSoundType.HandPry:
                    toPlay = handPrySFX;
                    break;
                case DoorSoundType.BoltsDown:
                    toPlay = boltsDownSFX;
                    break;
                case DoorSoundType.BoltsUp:
                    toPlay = boltsUpSFX;
                    break;
            }
            return toPlay;
        }

        public void StopSound()
        {
            if (soundGuid != "")
			{
				SoundManager.StopNetworked(soundGuid);
			}
		    soundGuid = "";
        }
	}
}
