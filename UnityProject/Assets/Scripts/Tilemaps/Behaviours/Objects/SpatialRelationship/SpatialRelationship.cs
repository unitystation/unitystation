using Logs;

/// <summary>
/// Main API for working with spatial relationships. If you want to activate or end a relationship,
/// use these methods. Use spatial relationships if you need to have some logic run when 2 things
/// move relative to each other, such as going out of range.
///
/// A spatial relationship defines an arbitrary server-side spatial relationship between 2 RegisterTiles, so that logic
/// can be invoked when either of the objects in the relationship moves relative to each other.
///
/// A relationship is first started by calling ServerActivate. The relationship will be checked appropriately when the
/// objects move relative to each other according to the RegisterTiles. The relationship ends
/// by being ended, which may be due to the OnRelationshipChanged method returning false, or
/// by some other external means (such as one of the objects being destroyed). When it ends, the
/// OnRelationshipEnded hook is invoked. It can be ended at will by calling ServerEnd.
///
/// This can be used to avoid needing to do Update() polling - checking if the objects have moved away from each other
/// in an update loop. Instead of polling, it simply invokes the OnRelationshipChanged hook whenever either
/// of the objects moves according to the RegisterTile position, so that polling isn't needed. It will
/// correctly handle cross matrix situations as well (I hope).
///
/// NOTE that it currently handles cross matrix situations by polling.
/// </summary>
public static class SpatialRelationship
{
	/// <summary>
	/// Server side only. Activates the relationship, such that both sides will start checking it when they move relative to each other.
	/// This is the main method to use to create and start checking a relationship.
	/// </summary>
	/// <param name="relationship"></param>
	public static void ServerActivate(BaseSpatialRelationship relationship)
	{
		Loggy.LogTraceFormat("Activating spatial relationship {0}", Category.SpatialRelationship, relationship);
		relationship.obj1._AddSpatialRelationship(relationship);
		relationship.obj2._AddSpatialRelationship(relationship);
		//check the relationship immediately
		if (relationship.ShouldRelationshipEnd())
		{
			ServerEnd(relationship);
		}
	}

	/// <summary>
	/// Server side only. Ends the relationship, such that it will no longer be checked by either side of the relationship.
	/// Only valid for relationships that have already been activated via SpatialRelationship.Activate.
	/// </summary>
	/// <param name="relationship"></param>
	public static void ServerEnd(BaseSpatialRelationship relationship)
	{
		relationship.OnRelationshipEnded();
		relationship.obj1._RemoveSpatialRelationship(relationship);
		relationship.obj2._RemoveSpatialRelationship(relationship);
	}
}
