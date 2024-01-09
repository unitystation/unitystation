using System;
using System.Collections.Generic;
using AddressableReferences;
using Messages.Server.SoundMessages;
using Systems.Explosions;
using UnityEngine;

namespace Items.Toys
{
	public class PlushNoise : MonoBehaviour
	{
		private int numberOfPets = 0;
		private float petDecay = 0.24f;
		private float pitchUpPerPet = 0.1f;
		private float currentPitchExtra = 0f;
		private TimeSpan lastTime = DateTime.Now.TimeOfDay;

		[SerializeField] private List<AddressableAudioSource> petSounds = new List<AddressableAudioSource>();
		[SerializeField] private float supriseStrength = 100f;

		private void Awake()
		{
			UpdateManager.Add(Decay, 0.45f);
		}

		private void OnDestroy()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Decay);
		}

		private void Decay()
		{
			if (lastTime.TotalSeconds + 3 > DateTime.Now.TimeOfDay.TotalSeconds) return;
			if (currentPitchExtra <= 0)
			{
				currentPitchExtra = 0;
				return;
			}
			currentPitchExtra -= petDecay;
		}

		public void PlaySound()
		{
			AudioSourceParameters parameters = new AudioSourceParameters
			{
				Pitch = 1f + currentPitchExtra,
			};
			SoundManager.PlayNetworkedAtPos(petSounds.PickRandom(), gameObject.AssumedWorldPosServer(), parameters);
			currentPitchExtra += pitchUpPerPet;
			lastTime = DateTime.Now.TimeOfDay;
			Suprise();
		}

		private void Suprise()
		{
			if (currentPitchExtra < 2.5f) return;
			var location = gameObject.AssumedWorldPosServer().CutToInt();
			_ = Despawn.ServerSingle(gameObject);
			Explosion.StartExplosion(location, supriseStrength);
		}
	}
}