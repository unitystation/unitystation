using System.Collections.Generic;
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

		public enum ExperienceLevel
		{
			NewToSpaceStation = 0, //TRIGGER EVERYTHING!!!1!!1!
			NewToUnityStation = 1, //Unitystation changes only
			SomewhatExperienced = 2, //Life critical Advice only
			Robust = 3 //Nothing will get triggered on this level.
		}

		public override void Awake()
		{
			base.Awake();

			var experience = PlayerPrefs.GetInt("Learning/ExperienceLevel", -1);
			if(experience == -1)
			{
				//TODO : TRIGGER FIRST TIME TIP SELECTION.
				return;
			}
			PlayerExperienceLevel = (ExperienceLevel) experience;
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

		//TODO : ADD TIP QUEUEING

		public void ShowTip(string TipText, float duration = 25f, Sprite img = null, ProtipUI.SpriteAnimation showAnim = ProtipUI.SpriteAnimation.ROCKING)
		{
			if(duration <= 0) duration = 25f; //Incase whoever was setting the SO data forgot to set the duration.
			UI.gameObject.SetActive(true);
			UI.ShowTip(TipText, duration, img, showAnim);
		}
	}
}