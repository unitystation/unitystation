using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// <see cref="RegisterTile"/> for an object, adds additional logic to
/// make object passable / impassable.
/// </summary>
[ExecuteInEditMode]
public class RegisterObject : RegisterTile
{
	public bool AtmosPassable = true;
	public bool Passable = true;

	public override bool IsPassable(Vector3Int from) => Passable || PositionS == TransformState.HiddenPos;

	public override bool IsPassable()
	{
		return Passable || PositionS == TransformState.HiddenPos;
	}

	public override bool IsAtmosPassable(Vector3Int from)
	{
		return AtmosPassable || PositionS == TransformState.HiddenPos;
	}

	private CustomNetTransform pushable;
	protected override void InitDerived()
	{
		pushable = GetComponent<CustomNetTransform>();
	}

	public override void UpdatePositionServer()
	{
		if ( !pushable )
		{
			base.UpdatePositionServer();
		}
		else
		{
			PositionS = pushable.ServerLocalPosition;
		}
	}
	public override void UpdatePositionClient()
	{
		if ( !pushable )
		{
			base.UpdatePositionClient();
		}
		else
		{
			PositionC = pushable.ClientLocalPosition;
		}
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
