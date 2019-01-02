using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// RegisterTile for an object, handles whether the object is passable.
/// </summary>
[ExecuteInEditMode]
public class RegisterObject : RegisterTile
{
	public bool AtmosPassable = true;

	public bool Passable = true;

	public override bool IsPassable(Vector3Int from) => Passable;

	public override bool IsPassable()
	{
		return Passable;
	}

	public override bool IsAtmosPassable()
	{
		return AtmosPassable;
	}

	public override void AfterUnregister()
	{
		//no longer interferes with anything
		Passable = true;
		AtmosPassable = true;
	}

	#region UI Mouse Actions

	public void OnHoverStart()
	{
		//thanks stack overflow!
		Regex r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

		string tmp = r.Replace(name, " ");
		UIManager.SetToolTip = tmp;
	}

	public void OnHoverEnd()
	{
		UIManager.SetToolTip = "";
	}

	#endregion
}
