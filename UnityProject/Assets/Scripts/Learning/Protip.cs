using System;
using UnityEngine;

namespace Learning
{
	[Serializable]
	public struct Protip
	{
		public string Tip;
		public Sprite TipIcon;
		public ProtipUI.SpriteAnimation ShowAnimation;
		public ProtipManager.ExperienceLevel MinimumExperienceLevelToTrigger;
		public float ShowDuration;

	}
}