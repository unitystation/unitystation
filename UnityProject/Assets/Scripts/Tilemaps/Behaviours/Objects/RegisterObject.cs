using System.Text.RegularExpressions;
using UnityEngine;
using Mirror;

/// <summary>
/// <see cref="RegisterTile"/> for an object, adds additional logic to
/// make object passable / impassable.
/// </summary>
[ExecuteInEditMode]
public class RegisterObject : RegisterTile
{
	public bool AtmosPassable = true;
	[SyncVar]
	public bool Passable = true;

	private bool initialAtmosPassable;
	private bool initialPassable;

	protected override void Awake()
	{
		base.Awake();
		initialPassable = Passable;
		initialAtmosPassable = AtmosPassable;
	}

	/// <summary>
	/// Restore all variables specific to RegisterObject to the state they were on creation
	/// </summary>
	public void RestoreAllToDefault()
	{
		AtmosPassable = initialAtmosPassable;
		Passable = initialPassable;
	}

	/// <summary>
	/// Restore the passable variable to the state it was on creation
	/// </summary>
	public void RestorePassableToDefault()
	{
		Passable = initialPassable;
	}

	public override bool IsPassable(Vector3Int from, bool isServer)
	{
		return Passable || (isServer ? LocalPositionServer == TransformState.HiddenPos : LocalPositionClient == TransformState.HiddenPos );
	}

	public override bool IsPassable(bool isServer)
	{
		return Passable || (isServer ? LocalPositionServer == TransformState.HiddenPos : LocalPositionClient == TransformState.HiddenPos );
	}

	public override bool IsAtmosPassable(Vector3Int from, bool isServer)
	{
		return AtmosPassable || (isServer ? LocalPositionServer == TransformState.HiddenPos : LocalPositionClient == TransformState.HiddenPos );
	}

	#region UI Mouse Actions

	public void OnHoverStart()
	{
		if (GetComponent<Attributes>())
		{
			return;
		}

		if (gameObject.IsAtHiddenPos()) return;

		//thanks stack overflow!
		Regex r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

		UIManager.SetToolTip = r.Replace(name, " ");
	}

	public void OnHoverEnd()
	{
		UIManager.SetToolTip = "";
	}

	#endregion
}