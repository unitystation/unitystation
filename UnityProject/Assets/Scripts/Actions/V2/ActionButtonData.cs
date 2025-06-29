using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

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
	public class ActionButtonData
	{
		public string ID;
		public string DisplayName;
		[TextArea(2, 4)] public string Description;
		public ActionTriggerType TriggerType;
		public ActionType Type;
		public List<SpriteDataSO> AnimatedIconCatalogue;
		public float CooldownTime;
		public bool CanUseWhileGhosting = false;

		public bool HasCustomCursorOffset { get; set; }
		public bool HasCustomCursor;
		public CursorOffsetType OffsetType;
		public Vector2 CursorOffset;
		public Texture2D CursorTexture;

		[HideInInspector] public NetworkIdentity TrackingObject;
	}
}