using UnityEngine;

namespace Learning
{
	public class ProtipManager : Managers.SingletonManager<ProtipManager>
	{
		public ProtipUI UI;
		public ExperienceLevel PlayerExperienceLevel;

		public enum ExperienceLevel
		{
			NewToSpaceStation = 0, //TRIGGER EVERYTHING!!!1!!1!
			NewToUnityStation = 1, //Unitystation changes only
			SomewhatExperienced = 2, //Life critical Advice only
			Robust = 4 //Nothing will get triggered on this level.
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
			UI.ShowTip(TipText, duration, img, showAnim);
		}
	}
}