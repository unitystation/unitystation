
/// <summary>
/// Defines an arbitrary server-side spatial relationship between 2 RegisterTiles, so that logic
/// can be invoked when either of the objects in the relationship moves relative to each other.
///
/// This can be used to avoid needing to do Update() polling - checking if the objects have moved away from each other
/// in an update loop. Instead of polling, it simply invokes the OnRelationshipChanged hook whenever either
/// of the objects moves according to the RegisterTile position, so that polling isn't needed. It should
/// correctly handle cross matrix situations as well.
/// </summary>
public abstract class BaseSpatialRelationship
{
	/// <summary>
	/// Partner in the relationship, also the leader.
	/// </summary>
	public readonly RegisterTile obj1;
	/// <summary>
	/// Other partner in the relationship.
	/// </summary>
	public readonly RegisterTile obj2;

	/// <summary>
	/// Returns the other side of the relationship relative to one of the
	/// partners.
	/// </summary>
	/// <param name="me"></param>
	/// <returns></returns>
	public RegisterTile Other(RegisterTile me)
	{
		if (obj1 == me)
		{
			return obj2;
		}

		if (obj2 == me)
		{
			return obj1;
		}

		return null;
	}

	/// <summary>
	/// Returns true iff this partner of the relationship is the leader (there is only one leader)
	/// </summary>
	/// <param name="me"></param>
	/// <returns></returns>
	public bool IsLeader(RegisterTile me)
	{
		return me == obj1;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="obj1"></param>
	/// <param name="obj2"></param>
	protected BaseSpatialRelationship(RegisterTile obj1, RegisterTile obj2)
	{
		this.obj1 = obj1;
		this.obj2 = obj2;
	}

	/// <summary>
	/// Invoked to check if the relationship should be ended.
	///
	/// Invoked automatically on creation and when either of the objects moves relative to each other (i.e. it wouldn't be called if they're
	/// both on the same matrix and the matrix is moving, but it would be called if they are in different matrices
	/// and either of the matrices is moving)
	/// </summary>
	/// <returns>true iff the relationship should be ended. The relationship will end
	/// and no further checking will be performed. OnRelationshipEnded hook will be called. false to continue the relationship.</returns>
	public abstract bool ShouldRelationshipEnd();

	/// <summary>
	/// Invoked when the relationship is going to be ended for any reason, such as one side of the relationship
	/// being destroyed or true being returned from ShouldRelationshipEnd.
	/// </summary>
	/// <returns></returns>
	public abstract void OnRelationshipEnded();

	public override string ToString()
	{
		return $"{nameof(obj1)}: {obj1}, {nameof(obj2)}: {obj2}";
	}
}
