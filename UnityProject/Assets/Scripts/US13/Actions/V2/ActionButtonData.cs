using System.Collections.Generic;
using Mirror;
using NaughtyAttributes;
using UnityEngine;

namespace US13.Actions.V2
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

		[BoxGroup("Interaction Settings")] public ActionTriggerType TriggerType;
		[BoxGroup("Interaction Settings")] public ActionType Type;
		[BoxGroup("Interaction Settings")] public float CooldownTime;
		[BoxGroup("Interaction Settings")] public bool CanUseWhileGhosting = false;
		[BoxGroup("Sprites")] public List<SpriteDataSO> AnimatedIconCatalogue;
		[BoxGroup("Sprites")] public bool TryToGrabSpritesFromRelatedObject = false;

		[BoxGroup("Cursor Settings")] public bool HasCustomCursorOffset;
		[BoxGroup("Cursor Settings")] public bool HasCustomCursor;
		[BoxGroup("Cursor Settings")] public CursorOffsetType OffsetType;
		[BoxGroup("Cursor Settings")] public Vector2 CursorOffset;
		[BoxGroup("Cursor Settings")] public SpriteDataSO CursorTexture;

		[HideInInspector] public NetworkIdentity TrackingObject;
		[HideInInspector] public GameObject ObjectRelatedToThisAction;
	}
}