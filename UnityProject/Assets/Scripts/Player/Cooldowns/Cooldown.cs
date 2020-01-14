
using UnityEngine;

/// <summary>
/// Defines a particular kind of cooldown. Each cooldown is tracked independently of each other.
/// </summary>
public class Cooldown
{
	private enum CooldownType
	{
		Category,
		Interaction
	}

	private readonly CooldownCategory? category;
	private readonly ushort interactableComponentTypeID;
	private readonly CooldownType cooldownType;

	private Cooldown(CooldownCategory? category, ushort interactableComponentTypeID, CooldownType cooldownType)
	{
		this.category = category;
		this.interactableComponentTypeID = interactableComponentTypeID;
		this.cooldownType = cooldownType;
	}

	/// <summary>
	/// A cooldown defined by a particular category.
	/// </summary>
	/// <param name="category"></param>
	/// <returns></returns>
	public static Cooldown Category(CooldownCategory category)
	{
		return new Cooldown(category, 0, CooldownType.Category);
	}

	/// <summary>
	/// A cooldown which is defined by a particular TYPE of interactable component. Useful for
	/// creating interactable components with their own specific cooldown. The specific instance
	/// doesn't matter - this cooldown is shared by all instances of that type. For example, you'd always get
	/// the same Cooldown regardless of which object's Meleeable component you passed to this.
	/// </summary>
	/// <param name="component"></param>
	/// <returns></returns>
	public static Cooldown Interaction<T>(IInteractable<T> interactable) where T : Interaction
	{
		return new Cooldown(null, RequestInteractMessage._GetInteractableComponentTypeID(interactable), CooldownType.Interaction);
	}

	protected bool Equals(Cooldown other)
	{
		return category == other.category && interactableComponentTypeID == other.interactableComponentTypeID && cooldownType == other.cooldownType;
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((Cooldown) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = category.GetHashCode();
			hashCode = (hashCode * 397) ^ interactableComponentTypeID.GetHashCode();
			hashCode = (hashCode * 397) ^ (int) cooldownType;
			return hashCode;
		}
	}
}

/// <summary>
/// Different kinds of cooldowns based on a broad category.
/// </summary>
public enum CooldownCategory
{
	//Base cooldown for all interactions.
	Interaction = 0,
	//Melee type interactions
	Melee = 1
}
