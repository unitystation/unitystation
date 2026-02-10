using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Managers;
using US13.Messages.Client;
using US13.UI.Systems.AdminTools;

namespace US13.UI.Systems.MainHUD.UI_Bottom
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

		public string VOXStringLine = "Assets/Prefabs/AI/VOX/";
		public string VOXStringLineEnd = ".prefab";

		public void Start()
		{
			VOXLines = AdminGlobalAudio.audioList.Where(x => x.Contains(VOXStringLine))
				.Select(x => x.Replace(VOXStringLine, "").Replace(VOXStringLineEnd, "")).ToList();
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
					{AssetAddress = VOXStringLine + Text.Text.text + VOXStringLineEnd};
				AddressableAudioSource addressableAudioSource =
					await AudioManager.GetAddressableAudioSourceFromCache(Source);
				await UniTask.Delay(TimeSpan.FromSeconds(addressableAudioSource.AudioSource.clip.length),
					ignoreTimeScale: false);
			}
		}
	}
}