using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AddressableReferences;
using AdminTools;
using Audio.Containers;
using Cysharp.Threading.Tasks;
using Messages.Client;
using Messages.Server.SoundMessages;
using TMPro;
using UnityEngine;

namespace AI
{
	public class VOXUI : MonoBehaviour
	{
		public VOXText VOXTextPrefab;

		public GameObject SearchBox;


		public GameObject SaysBox;

		public List<VOXText> ActiveSearches = new List<VOXText>();
		public List<VOXText> ActiveSays = new List<VOXText>();

		public TMP_InputField InputField;

		public List<string> VOXLines = new List<string>();

		public void Start()
		{
			VOXLines = AdminGlobalAudio.audioList.Where(x => x.Contains("AI/VOX"))
				.Select(x => x.Replace("Assets/Prefabs/AI/VOX/", "").Replace(".prefab", "")).ToList();
			InputField.onValueChanged.AddListener(Search);
		}

		public void Search(string searching)
		{
			foreach (var txt in ActiveSearches)
			{
				if (txt == null) continue;
				Destroy(txt.gameObject);
			}

			ActiveSearches.Clear();
			if (searching.Length == 0) return;

			var Search = VOXLines.OrderBy(s =>
					s.Equals(searching, StringComparison.OrdinalIgnoreCase) ? 0 :
					s.StartsWith(searching, StringComparison.OrdinalIgnoreCase) ? 1 :
					s.Contains(searching, StringComparison.OrdinalIgnoreCase) ? 2 :
					3)
				.ThenBy(s => Math.Abs(s.Length - searching.Length))
				.ToList();

			int i = 0;

			foreach (var Found in Search)
			{
				i++;
				if (i > 6) return;
				var Text = Instantiate(VOXTextPrefab, SearchBox.transform);
				Text.SetUp(Found, this, true);
				ActiveSearches.Add(Text);
			}
		}

		public void AddToSaysBox(VOXText VOXText)
		{
			ActiveSearches.Remove(VOXText);
			ActiveSays.Add(VOXText);
			VOXText.gameObject.transform.SetParent(SaysBox.transform);
			VOXText.SetUp(VOXText.Text.text, this, false);
		}

		public void RemoveAndDestroyFromSaysBox(VOXText VOXText)
		{
			ActiveSays.Remove(VOXText);
			Destroy(VOXText.gameObject);
		}

		public void SimplePlay()
		{
			_ = Play();
		}

		public async UniTask Play()
		{
			foreach (var Text in ActiveSays)
			{
				await UniTask.SwitchToMainThread();
				RequestVOXsay.Send(Text.Text.text);
				var Source = new AddressableAudioSource()
					{AssetAddress = "Assets/Prefabs/AI/VOX/" + Text.Text.text + ".prefab"};
				AddressableAudioSource addressableAudioSource =
					await AudioManager.GetAddressableAudioSourceFromCache(Source);
				await UniTask.Delay(TimeSpan.FromSeconds(addressableAudioSource.AudioSource.clip.length),
					ignoreTimeScale: false);
			}
		}
	}
}