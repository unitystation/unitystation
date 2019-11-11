
using System;
using UnityEngine;

/// <summary>
/// spatial relationship which fires some logic and ends the relationship
/// when the objects go out of range of each other or when the relationship is ended via
/// any other means.
/// </summary>
public class RangeRelationship : BaseSpatialRelationship
{
	private readonly float maxRange;
	private readonly Action<RangeRelationship> onRangeExceeded;

	private RangeRelationship(RegisterTile obj1, RegisterTile obj2, float maxRange, Action<RangeRelationship> onRangeExceeded) : base(obj1, obj2)
	{
		this.maxRange = maxRange;
		this.onRangeExceeded = onRangeExceeded;
	}

	/// <summary>
	/// Defines a relationship in which the onRangeExceeded action is invoked when
	/// the objects are further than maxRange from each other or the relationship is cancelled.
	/// </summary>
	/// <returns></returns>
	public static RangeRelationship Between(RegisterTile obj1, RegisterTile obj2, float maxRange, Action<RangeRelationship> onRangeExceeded)
	{
		return new RangeRelationship(obj1, obj2, maxRange, onRangeExceeded);
	}

	/// <summary>
	/// Defines a relationship in which the onRangeExceeded action is invoked when
	/// the objects are further than maxRange from each other or the relationship is cancelled.
	/// </summary>
	/// <returns></returns>
	public static RangeRelationship Between(GameObject obj1,
		GameObject obj2, float maxRange, Action<RangeRelationship> onRangeExceeded)
	{
		var reg1 = obj1.RegisterTile();
		if (reg1 == null)
		{
			Logger.LogErrorFormat("Cannot define relationship between {0} and {1} because {0} has no RegisterTile or subclass",
				Category.SpatialRelationship, obj1, obj2);
		}
		var reg2 = obj2.RegisterTile();
		if (reg2 == null)
		{
			Logger.LogErrorFormat("Cannot define relationship between {0} and {1} because {1} has no RegisterTile or subclass",
				Category.SpatialRelationship, obj1, obj2);
		}
		return new RangeRelationship(reg1, reg2, maxRange, onRangeExceeded);
	}

	public override bool OnRelationshipChanged()
	{
		if (Vector3Int.Distance(obj1.WorldPositionServer, obj2.WorldPositionServer) > maxRange)
		{
			onRangeExceeded.Invoke(this);
			return true;
		}

		return false;
	}

	public override void OnRelationshipEnded()
	{
		onRangeExceeded.Invoke(this);
	}
}
