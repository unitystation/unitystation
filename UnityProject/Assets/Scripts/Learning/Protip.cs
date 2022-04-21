using UnityEngine;

namespace Learning
{
	public class Protip
	{
		public string Tip;
		public Sprite TipIcon;
		public ProtipUI.SpriteAnimation ShowAnimation;
		public TriggerType Trigger;
		public ExperienceLevel MinimumExperienceLevelToTrigger;
		public float ShowDuration;

		public enum TriggerType
		{
			OnPickupSpecificObject,
			OnPickupItemTrait,
			OnSpawn,
			OnSpawnAfterSpecificRoundTime,
			OnLowHealth,
			OnTakeDamage,
			OnEnteringRadius,
			OnChangeIntent,
			OnItemUse,
			Random,
			Custom
		}

		public enum ExperienceLevel
		{
			NewToSpaceStation, //TRIGGER EVERYTHING!!!1!!1!
			NewToUnityStation, //Unitystation changes only
			SomewhatExperienced, //Life critical Advice only
			Robust //Nothing will get triggered on this level.
		}

	}
}