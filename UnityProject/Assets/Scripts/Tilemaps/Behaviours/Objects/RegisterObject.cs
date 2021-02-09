using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Blob;
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

	[Tooltip("If true, this object won't block players from interacting with other objects")]
	public bool ReachableThrough = true;

	private bool initialAtmosPassable;
	private bool initialPassable;

	[SerializeField] private List<PassableExclusionTrait> passableExclusionsToThis = default;

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

	public override bool IsPassableFromOutside(Vector3Int enteringFrom, bool isServer, GameObject context = null)
	{
		if (context == gameObject) return true; // Object can pass through its own RegisterTile.
		if (CheckPassableExclusions(context)) return true;

		return Passable || (isServer ? LocalPositionServer == TransformState.HiddenPos : LocalPositionClient == TransformState.HiddenPos);
	}

	public override bool IsPassable(bool isServer, GameObject context = null)
	{
		if (context == gameObject) return true; // Object can pass through its own RegisterTile.
		if (CheckPassableExclusions(context)) return true;

		return Passable || (isServer ? LocalPositionServer == TransformState.HiddenPos : LocalPositionClient == TransformState.HiddenPos );
	}

	public override bool IsAtmosPassable(Vector3Int enteringFrom, bool isServer)
	{
		return AtmosPassable || (isServer ? LocalPositionServer == TransformState.HiddenPos : LocalPositionClient == TransformState.HiddenPos );
	}

	public override bool IsReachableThrough(Vector3Int reachingFrom, bool isServer, GameObject context = null)
	{
		return ReachableThrough || (isServer ? LocalPositionServer == TransformState.HiddenPos : LocalPositionClient == TransformState.HiddenPos);
	}

	private bool CheckPassableExclusions(GameObject context)
	{
		if (context != null && context.TryGetComponent<PassableExclusionHolder>(out var passableExclusionsMono) && passableExclusionsMono != null)
		{
			foreach (var exclusion in passableExclusionsToThis)
			{
				if(!passableExclusionsMono.passableExclusions.Contains(exclusion)) continue;

				return true;
			}
		}

		return false;
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
