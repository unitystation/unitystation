using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using NaughtyAttributes;
using ScriptableObjects.Systems.Spells;
using Logs;
using System;

[System.Serializable]

[CreateAssetMenu(fileName = "ActionData", menuName = "ScriptableObjects/ActionData")]
public class ActionData : ScriptableObject
{
	[SerializeField]
	private UIActionType actionType = UIActionType.Momentary;
	[Tooltip("The sprite index to use when the toggle is active.")]
	[SerializeField, ShowIf(nameof(IsToggle))]
	private int activeSpriteIndex = 1;

	[FormerlySerializedAs("CallOnClient")]
	[SerializeField]
	private bool callOnClient = default;
	[FormerlySerializedAs("CallOnServer")]
	[SerializeField]
	private bool callOnServer = default;
	public UIActionType ActionType => actionType;
	public virtual bool CallOnClient => callOnClient;
	public virtual bool CallOnServer => callOnServer;

	[FormerlySerializedAs("spellName")]
	[SerializeField] protected string actionName = "";
	[SerializeField] protected string description = "";
	public string Name => actionName;
	public string Description => description;

	public List<SpriteDataSO> Sprites = null;
	public List<SpriteDataSO> Backgrounds = null;

	public List<ActionController> PreventBeingControlledBy = new List<ActionController>();

	public List<Event> DisableOnEvent = new List<Event>();

	[Tooltip("Whether the toggle allows for cursor aiming.")]
	[SerializeField, BoxGroup("Custom Cursor Settings"), ShowIf(nameof(IsToggle))]
	private bool isAimable = false;
	[Tooltip("Whether a custom cursor texture should be applied when this action is toggled on. Useful for e.g. fireball.")]
	[SerializeField, BoxGroup("Custom Cursor Settings"), ShowIf(nameof(IsAimable))]
	private bool useCustomCursor = false;
	[SerializeField, BoxGroup("Custom Cursor Settings"), ShowIf(nameof(HasCustomCursor))]
	private Texture2D cursorTexture = default;
	[SerializeField, BoxGroup("Custom Cursor Settings"), ShowIf(nameof(HasCustomCursor))]
	private CursorOffsetType cursorOffsetType = CursorOffsetType.TopLeft;
	[SerializeField, BoxGroup("Custom Cursor Settings"), ShowIf(nameof(HasCustomCursorOffset))]
	private Vector2 cursorOffset = Vector2.zero;

	public bool IsToggle => ActionType == UIActionType.Toggle;
	public bool IsAimable => isAimable;
	/// <summary> The sprite SO that is used when the toggle is active. </summary>
	public SpriteDataSO ActiveSprite => (Sprites.Count - 1 < activeSpriteIndex && !HandleFallbackSpriteIndex()) ?
										default : Sprites[activeSpriteIndex]; //Only access our index if its actually valid, if not then use our 0 state as a fallback and print an error
																			  //if STILL invalid then print another error telling you to set a sprite and set our value to default

	public bool HasCustomCursor => useCustomCursor;
	public Texture2D CursorTexture => cursorTexture;
	public bool HasCustomCursorOffset => cursorOffsetType == CursorOffsetType.Custom;
	public CursorOffsetType OffsetType => cursorOffsetType;
	public Vector2 CursorOffset => cursorOffset;
	[Tooltip("Do we stay as the active action even after being used.")]
	[SerializeField]
	public bool StaySelectedOnUse = false;

	public override string ToString()
	{
		if (SpellList.Instance && this == SpellList.Instance.InvalidData)
		{
			return "[InvalidData]";
		}
		return $"[ActionData '{Name}' ({Description})]";
	}

	private bool HandleFallbackSpriteIndex()
	{
		if(Sprites.Count - 1 < 0)
		{
			Loggy.LogError("ScriptableObject.ActionData.activeSpriteIndex created without any set sprites, add some!");
			return false;
		}
		Loggy.LogError($"ScriptableObject.ActionData.activeSpriteIndex set to a value({activeSpriteIndex}) without a matching sprite, falling back to sprite 0.");
		activeSpriteIndex = 0;
		return true;
	}
}

public enum ActionController
{
	Inventory,
}

public enum UIActionType
{
	Momentary,
	Toggle,
}

public enum CursorOffsetType
{
	TopLeft,
	Centered,
	Custom
}
