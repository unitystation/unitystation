using UnityEngine;

namespace Chemistry
{
	public abstract class Effect : ScriptableObject
	{
		private string displayName;

		[SerializeField] private string overrideDisplayName = null;
		public string DisplayName
		{
			get
			{
				if (overrideDisplayName != null) return overrideDisplayName;
				if (displayName != null) return displayName;

				displayName = name;
				return displayName;
			}
		}

		public abstract void Apply(MonoBehaviour sender, float amount);
	}
}