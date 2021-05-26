using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using NaughtyAttributes;
using ScriptableObjects.Systems.Spells;

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
	public SpriteDataSO ActiveSprite => Sprites[activeSpriteIndex];

	public bool HasCustomCursor => useCustomCursor;
	public Texture2D CursorTexture => cursorTexture;
	public bool HasCustomCursorOffset => cursorOffsetType == CursorOffsetType.Custom;
	public CursorOffsetType OffsetType => cursorOffsetType;
	public Vector2 CursorOffset => cursorOffset;

	public override string ToString()
	{
		if (SpellList.Instance && this == SpellList.Instance.InvalidData)
		{
			return "[InvalidData]";
		}
		return $"[ActionData '{Name}' ({Description})]";
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
