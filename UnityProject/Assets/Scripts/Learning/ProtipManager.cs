using System.Collections.Generic;
using System.IO;
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

		public List<ProTipSaveData> ProtipSaveStates { private set; get; } = new List<ProTipSaveData>();
		private readonly Queue<ProtipSO> queuedTips = new Queue<ProtipSO>();
		private string jsonPath;
		private string jsonFileName = "/protips.json";
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
			jsonPath = Application.streamingAssetsPath + jsonFileName;
			var experience = PlayerPrefs.GetInt("Learning/ExperienceLevel", -1);
			if(experience == -1)
			{
				//TODO : TRIGGER FIRST TIME TIP SELECTION.
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
			var found = false;
			foreach (var tip in ProtipSaveStates)
			{
				if (tip.ID != ID) continue;
				tip.Remembered = dontShowAgain;
				found = true;
				break;
			}
			if(found == false)
			{
				var newTip = new ProTipSaveData { ID = ID, Remembered = dontShowAgain };
				ProtipSaveStates.Add(newTip);
			}

			SaveProtipSaveStates();
		}

		private void UpdateRecordedTips()
		{
			if (File.Exists(jsonPath) == false || File.ReadAllText(jsonPath).Length <= JSON_EMPTY_LIST) return;
			var newList = JsonConvert.DeserializeObject<List<ProTipSaveData>>(File.ReadAllText(jsonPath));
			ProtipSaveStates = newList;
		}

		private void SaveProtipSaveStates()
		{
			var newData = JsonConvert.SerializeObject(ProtipSaveStates);
			File.WriteAllText(jsonPath, newData);
		}

		public void ClearSaveState()
		{
			ProtipSaveStates.Clear();
			File.WriteAllText(jsonPath, "");
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
			if(IsShowingTip || queuedTips.Count == 0) return;
			var tip = queuedTips.Dequeue();
			ShowTip(tip);
		}

		public void QueueTip(ProtipSO tip)
		{
			queuedTips.Enqueue(tip);
		}

		private void ShowTip(ProtipSO tip)
		{
			UI.gameObject.SetActive(true);
			UI.ShowTip(tip);
			IsShowingTip = true;
		}
	}

	public class ProTipSaveData
	{
		public string ID;
		public bool Remembered;
	}
}
