using Mirror;
using UnityEngine;

namespace Actions.V2
{
	public enum ActionTriggerType
	{
		ServerOnly,
		ClientOnly,
		Both
	}

	public enum ActionType
	{
		Trigger,
		Activated,
	}

	[System.Serializable]
	public struct ActionButtonData
	{
		public string ID;
		public string DisplayName;
		public string Description;
		public ActionTriggerType TriggerType;
		public ActionType Type;
		public Sprite Icon;
		public SpriteDataSO AnimatedIcon;
		public float CooldownTime;

		public bool HasCustomCursorOffset { get; set; }
		public bool HasCustomCursor;
		public CursorOffsetType OffsetType;
		public Vector2 CursorOffset;
		public Texture2D CursorTexture;
	}
}