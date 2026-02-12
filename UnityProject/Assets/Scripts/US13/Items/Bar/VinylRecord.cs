using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Managers;

namespace US13.Items.Bar
{
	public class VinylRecord : MonoBehaviour, IExaminable
	{
		[SerializeField] public AddressableAudioSource music = null;

		private string songName;
		private string songArtist;

		private void Start()
		{
			_ = LoadMusic();
		}

		private async Task LoadMusic()
		{
			var song = await AudioManager.GetAddressableAudioSourceFromCache(new List<AddressableAudioSource> {music});
			var songtitle = song.AudioSource.clip.name;
			songName = songtitle.Split('_')[0];
			songArtist = songtitle.Contains("_") ? songtitle.Split('_')[1] : "Unknown";
		}

		public string Examine(Vector3 worldPos = default)
		{
			return $"Contains the song {songName} by {songArtist}";
		}
	}
}