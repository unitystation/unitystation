using System.Collections.Generic;
using System.Linq;
using SecureStuff;
using Managers;
using NaughtyAttributes;
using Newtonsoft.Json;
using Shared.Managers;
using UnityEngine;

namespace Learning
{
	public class ProtipManager : SingletonManager<ProtipManager>
	{
		public ProtipUI UI;
		public ProtipListUI ListUI;
		public ExperienceLevel PlayerExperienceLevel;
		public List<ProtipSO> RecordedProtips;
		public Dictionary<string, bool> ProtipSaveStates { private set; get; } = new Dictionary<string, bool>();


		private readonly Queue<QueueTipData> queuedTips = new Queue<QueueTipData>();

		private const string JSON_FILE_NAME = "protips.json";
		private const int JSON_EMPTY_LIST = 5;

		public enum ExperienceLevel
		{
			NewToSpaceStation = 0, //TRIGGER EVERYTHING!!!1!!1!
			NewToUnityStation = 1, //Unitystation changes only
			SomewhatExperienced = 2, //Life critical Advice only
			Robust = 3 //Nothing will get triggered on this level.
		}

		public bool IsShowingTip { get; set; }

		public override void Awake()
		{
			base.Awake();
			var experience = PlayerPrefs.GetInt("Learning/ExperienceLevel", -1);
			if(experience == -1)
			{
				UIManager.Instance.FirstTimePlayerExperienceScreen.SetActive(true);
				return;
			}
			PlayerExperienceLevel = (ExperienceLevel) experience;
		}

		private void OnEnable()
		{
			UpdateManager.Add(CheckQueue, 1f);
			UpdateRecordedTips();
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckQueue);
		}

		public void SaveTipState(string ID, bool dontShowAgain = true)
		{
			UpdateRecordedTips();
			if (ProtipSaveStates.ContainsKey(ID))
			{
				ProtipSaveStates[ID] = dontShowAgain;
			}
			else
			{
				ProtipSaveStates.Add(ID, dontShowAgain);
			}
			SaveProtipSaveStates();
		}

		private void UpdateRecordedTips()
		{
			if (AccessFile.Exists(JSON_FILE_NAME,  folderType: FolderType.Data,  userPersistent : true) == false
			    || AccessFile.Load(JSON_FILE_NAME, FolderType.Data, userPersistent : true).Length <= JSON_EMPTY_LIST) return;

			var newList =
				JsonConvert.DeserializeObject<Dictionary<string, bool>>(
					AccessFile.Load(JSON_FILE_NAME, FolderType.Data, userPersistent : true))
				?? new Dictionary<string, bool>();
			ProtipSaveStates = newList;
		}

		private void SaveProtipSaveStates()
		{
			var newData = JsonConvert.SerializeObject(ProtipSaveStates, Formatting.Indented);
			AccessFile.Save(JSON_FILE_NAME, newData, FolderType.Data, userPersistent: true);
		}

		public void ClearSaveState()
		{
			ProtipSaveStates.Clear();
			AccessFile.Save(JSON_FILE_NAME, "", FolderType.Data,  userPersistent: true);
		}

		public void ShowListUI()
		{
			ListUI.SetActive(true);
		}

		public void SetExperienceLevel(ExperienceLevel level)
		{
			PlayerExperienceLevel = level;
			PlayerPrefs.SetInt("Learning/ExperienceLevel", (int)level);
			PlayerPrefs.Save();
		}

		private void CheckQueue()
		{
			if (IsShowingTip || queuedTips.Count == 0) return;
			var tip = queuedTips.Dequeue();
			ShowTip(tip.Tip, tip.highlightNames);
		}

		public void QueueTip(ProtipSO tip, List<string> highlightNames)
		{
			if (tip == null || queuedTips.Any(x => x.Tip == tip)) return;
			if (ProtipSaveStates.ContainsKey(tip.TipTitle)) return;
			QueueTipData data = new QueueTipData
			{
				Tip = tip,
				highlightNames = highlightNames
			};
			queuedTips.Enqueue(data);
		}

		private void ShowTip(ProtipSO tip, List<string> searchNames)
		{
			UI.gameObject.SetActive(true);
			UI.ShowTip(tip);
			if (searchNames != null)
			{
				foreach (var nameOfObj in searchNames)
				{
					GlobalHighlighterManager.Highlight(nameOfObj);
				}
			}
			IsShowingTip = true;
		}

		[Button("Test")]
		public void TriggerTestUI()
		{
			if (Application.isPlaying == false) return;
			ShowTip(RecordedProtips.PickRandom(), new List<string>());
		}

		private struct QueueTipData
		{
			public ProtipSO Tip;
			public List<string> highlightNames;
		}
	}
}
