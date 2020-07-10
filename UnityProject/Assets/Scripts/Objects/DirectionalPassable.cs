using System;
using UnityEngine;

/// <summary>
/// Allows only some sides of an objects's tile to be passable. Useful for things such as directional windows, disposal intakes.
/// Must be the sole RegisterTile-derived component on the object, else it is glitchy.
/// </summary>
[RequireComponent(typeof(Directional))]
public class DirectionalPassable : RegisterObject
{
	[Header("Set which sides are passable when in orientation Down")]
	public bool PassableUp;
	public bool PassableDown;
	public bool PassableLeft;
	public bool PassableRight;

	bool passableOnAll = false;
	public bool PassableOnAll => passableOnAll;

	public Directional Directional { get; private set; }

	#region RegisterObject Overrides

	protected override void Awake()
	{
		base.Awake();
		Directional = GetComponent<Directional>();
	}

	public override bool IsPassableTo(Vector3Int to, bool isServer)
	{
		return CheckPassable(to);
	}

	public override bool IsPassable(Vector3Int from, bool isServer)
	{
		return CheckPassable(from);
	}

	#endregion RegisterObject Overrides

	public void AllowPassableOnAllSides()
	{
		passableOnAll = true;
		Passable = true;
	}

	public void DenyPassableOnAllSides()
	{
		passableOnAll = false;
		Passable = false;
	}

	public void AllowPassableAtSetSides()
	{
		passableOnAll = false;
		Passable = true;
	}

	bool CheckPassable(Vector3Int sideVector)
	{
		// If passable on all, can be passable on any side.
		if (PassableOnAll) return true;
		// If not passable, cannot be passed on any side.
		if (!Passable) return false;

		Vector2Int sideToCross = (sideVector - LocalPosition).To2Int();
		bool result = IsPassableAtSide(sideToCross);
		return result;
	}

	bool IsPassableAtSide(Vector2Int sideToCross)
	{
		// TODO Consider using some data structure
		switch (Directional.CurrentDirection.AsEnum())
		{
			case OrientationEnum.Up:
				if (sideToCross == Vector2Int.up) return PassableDown;
				else if (sideToCross == Vector2Int.down) return PassableUp;
				else if (sideToCross == Vector2Int.left) return PassableRight;
				else if (sideToCross == Vector2Int.right) return PassableLeft;
				break;
			case OrientationEnum.Down:
				if (sideToCross == Vector2Int.up) return PassableUp;
				else if (sideToCross == Vector2Int.down) return PassableDown;
				else if (sideToCross == Vector2Int.left) return PassableLeft;
				else if (sideToCross == Vector2Int.right) return PassableRight;
				break;
			case OrientationEnum.Left:
				if (sideToCross == Vector2Int.up) return PassableLeft;
				else if (sideToCross == Vector2Int.down) return PassableRight;
				else if (sideToCross == Vector2Int.left) return PassableDown;
				else if (sideToCross == Vector2Int.right) return PassableUp;
				break;
			case OrientationEnum.Right:
				if (sideToCross == Vector2Int.up) return PassableRight;
				else if (sideToCross == Vector2Int.down) return PassableLeft;
				else if (sideToCross == Vector2Int.left) return PassableUp;
				else if (sideToCross == Vector2Int.right) return PassableDown;
				break;
		}

		return false;
	}
}
