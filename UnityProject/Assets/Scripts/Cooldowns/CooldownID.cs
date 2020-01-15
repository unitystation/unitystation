
/// <summary>
/// Uniquely identifies a particular cooldown. There are multiple ways to define cooldowns (SOs, Interactable Components...),
/// so this encapsulates those different ways. Cooldown identifiers also explicitly indicate if they are
/// for clientside or serverside logic.
/// </summary>
public class CooldownID
{
	private enum CooldownType
	{
		//identified by a cooldown asset
		Asset,
		//identified by an interactable component type ID
		Interaction
	}

	private readonly Cooldown cooldownAsset;
	private readonly ushort interactableComponentTypeID;
	private readonly NetworkSide networkSide;
	private readonly CooldownType cooldownType;

	private CooldownID(Cooldown cooldownAsset, ushort interactableComponentTypeID, NetworkSide networkSide, CooldownType cooldownType)
	{
		this.cooldownAsset = cooldownAsset;
		this.interactableComponentTypeID = interactableComponentTypeID;
		this.cooldownType = cooldownType;
		this.networkSide = networkSide;
	}

	/// <summary>
	/// Cooldown identified by a particular cooldown asset.
	/// </summary>
	/// <param name="cooldownAsset"></param>
	/// <param name="side">network side this cooldown is for</param>
	/// <returns></returns>
	public static CooldownID Asset(Cooldown cooldownAsset, NetworkSide side)
	{
		return new CooldownID(cooldownAsset, 0, side, CooldownType.Asset);
	}

	/// <summary>
	/// A cooldown which is identified by a particular TYPE of interactable component. Useful for
	/// creating interactable components with their own specific cooldown without needing to create
	/// an actual cooldown asset. The specific instance
	/// doesn't matter - this cooldown is shared by all instances of that type. For example, you'd always get
	/// the same CooldownIdentifier regardless of which object's Meleeable component you passed to this.
	/// </summary>
	/// <param name="interactable"></param>
	/// <param name="side">network side this cooldown is for</param>
	/// <returns></returns>
	public static CooldownID Interaction<T>(IInteractable<T> interactable, NetworkSide side) where T : Interaction
	{
		return new CooldownID(null, RequestInteractMessage._GetInteractableComponentTypeID(interactable),
			side, CooldownType.Interaction);
	}

	protected bool Equals(CooldownID other)
	{
		return Equals(cooldownAsset, other.cooldownAsset) && interactableComponentTypeID == other.interactableComponentTypeID && networkSide == other.networkSide && cooldownType == other.cooldownType;
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((CooldownID) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = (cooldownAsset != null ? cooldownAsset.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ interactableComponentTypeID.GetHashCode();
			hashCode = (hashCode * 397) ^ (int) networkSide;
			hashCode = (hashCode * 397) ^ (int) cooldownType;
			return hashCode;
		}
	}

	public override string ToString()
	{
		return $"{nameof(cooldownAsset)}: {cooldownAsset}, {nameof(interactableComponentTypeID)}: " +
		       $"{interactableComponentTypeID}, {nameof(networkSide)}: {networkSide}, {nameof(cooldownType)}: {cooldownType}";
	}
}
