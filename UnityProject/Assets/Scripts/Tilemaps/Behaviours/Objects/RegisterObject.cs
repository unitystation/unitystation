using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;
using Mirror;
using Core.Editor.Attributes;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;


/// <summary>
/// <see cref="RegisterTile"/> for an object, adds additional logic to
/// make object passable / impassable.
/// </summary>
[ExecuteInEditMode]
public class RegisterObject : RegisterTile, IPointerEnterHandler, IPointerExitHandler
{

	public bool AtmosPassable = true;

	[NonSerialized]
	[SyncVar(hook = nameof(SetPassable))]
	public bool Passable = true;

	[NonSerialized]
	[SyncVar(hook = nameof(SetCrawlingPassable))]
	public bool CrawlPassable = false;


	[Tooltip("If true, this object won't block players from interacting with other objects")]
	public bool ReachableThrough = true;


	private bool initialAtmosPassable;

	[SerializeField, FormerlySerializedAs("Passable") ]
	private bool initialPassable;

	[SerializeField, FormerlySerializedAs("CrawlPassable") ]
	private bool initialCrawlPassable;

	[SerializeField ]
	private List<PassableExclusionTrait> passableExclusionsToThis = default;

	protected override void Awake()
	{
		base.Awake();
		SetCrawlingPassable(CrawlPassable, initialCrawlPassable);
		SetPassable(Passable,initialPassable);
		initialAtmosPassable = AtmosPassable;
	}

	/// <summary>
	/// Restore all variables specific to RegisterObject to the state they were on creation
	/// </summary>
	public void RestoreAllToDefault()
	{
		AtmosPassable = initialAtmosPassable;
		SetCrawlingPassable(CrawlPassable, initialCrawlPassable);
		SetPassable(Passable,initialPassable);
	}

	/// <summary>
	/// Restore the passable variable to the state it was on creation
	/// </summary>
	public void RestorePassableToDefault()
	{
		SetCrawlingPassable(CrawlPassable, initialCrawlPassable);
		SetPassable(Passable,initialPassable);
	}

	public void SetPassable(bool old, bool Newin)
	{
		Passable = Newin;
	}

	public void SetCrawlingPassable(bool old, bool Newin)
	{
		CrawlPassable = Newin;
	}

	public override bool IsPassableFromOutside(Vector3Int enteringFrom, bool isServer, GameObject context = null)
	{
		if (context == gameObject) return true; // Object can pass through its own RegisterTile.
		if (CheckPassableExclusions(context)) return true;
		if (Passable != CrawlPassable)
		{
			if (context != null && context.GetComponent<RegisterPlayer>() != null &&
			    context.GetComponent<RegisterPlayer>().IsLayingDown)
			{
				return CrawlPassable || (isServer ? LocalPositionServer == TransformState.HiddenPos : LocalPositionClient == TransformState.HiddenPos);
			}
		}

		return Passable || (isServer ? LocalPositionServer == TransformState.HiddenPos : LocalPositionClient == TransformState.HiddenPos);
	}

	public override bool IsPassable(bool isServer, GameObject context = null)
	{
		if (context != null)
		{
			if (context == gameObject) return true; // Object can pass through its own RegisterTile.
		}

		if (CheckPassableExclusions(context)) return true;

		if (Passable != CrawlPassable)
		{
			if (context != null && context.GetComponent<RegisterPlayer>() != null &&
			    context.GetComponent<RegisterPlayer>().IsLayingDown)
			{
				return CrawlPassable || (isServer ? LocalPositionServer == TransformState.HiddenPos : LocalPositionClient == TransformState.HiddenPos);
			}
		}
		return Passable || (isServer ? LocalPositionServer == TransformState.HiddenPos : LocalPositionClient == TransformState.HiddenPos );
	}

	public override bool IsAtmosPassable(Vector3Int enteringFrom, bool isServer)
	{
		//If despawning then always be atmos passable
		if (Active == false) return true;

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

	public void OnPointerEnter(PointerEventData eventData)
	{
		UIManager.SetHoverToolTip = gameObject;
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

	public void OnPointerExit(PointerEventData eventData)
	{
		UIManager.SetToolTip = "";
		UIManager.SetHoverToolTip = null;
	}

	#endregion
}
