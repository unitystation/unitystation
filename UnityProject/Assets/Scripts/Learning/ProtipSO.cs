using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Learning
{
	[CreateAssetMenu(fileName = "Protip", menuName = "ScriptableObjects/Learning/ProtipSO")]
    public class ProtipSO : ScriptableObject
    {
		[FormerlySerializedAs("TipTile")]
        public string TipTitle;
        public Protip TipData;
    }

    [Serializable]
	public struct Protip : IEquatable<Protip>
	{
		public string Tip;
		public Sprite TipIcon;
		public ProtipUI.SpriteAnimation ShowAnimation;
		public ProtipManager.ExperienceLevel MinimumExperienceLevelToTrigger;
		public float ShowDuration;

		public bool Equals(Protip obj)
		{
			if(obj.Tip == null || Tip == null) return false;
			if(obj.Tip != Tip) return false;
			return base.Equals (obj);
		}
	}
}