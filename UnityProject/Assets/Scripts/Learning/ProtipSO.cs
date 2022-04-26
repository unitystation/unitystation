using System;
using UnityEngine;

namespace Learning
{
	[CreateAssetMenu(fileName = "Protip", menuName = "ScriptableObjects/Learning/ProtipSO")]
    public class ProtipSO : ScriptableObject
    {
        public string TipTile;
        public Protip TipData;
    }

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