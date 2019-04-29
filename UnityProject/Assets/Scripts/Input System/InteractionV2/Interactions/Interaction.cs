
using UnityEngine;

/// <summary>
/// Base class containing the info on an attempt to perform a particular interaction -
/// each interaction has a performer (player performing it)
/// and a used object (object they are using, dropping, or combining)
/// </summary>
public abstract class Interaction
{
	private readonly GameObject usedObject;
	private readonly GameObject performer;

	/// <summary>
	/// The gameobject of the player performing the interaction
	/// </summary>
	public GameObject Performer => performer;
	/// <summary>
	/// Object that is being used by the player to perform the interaction.
	/// For example...
	/// For hand apply - object in hand. Null if empty hand.
	/// For combine - object that was dragged to another slot.
	/// For activate - the object being activated
	/// For mouse drop - the object being dragged and dropped.
	/// </summary>
	public GameObject UsedObject => usedObject;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the interaction</param>
	/// <param name="usedObject">Object that is being used by the player to perform the interaction.
	///  For example...
	/// For hand apply - object in hand. Null if empty hand.
	/// For combine - object that was dragged to another slot.
	/// For activate - the object being activated
	/// For mouse drop - the object being dragged and dropped.</param>
	public Interaction(GameObject performer, GameObject usedObject)
	{
		this.performer = performer;
		this.usedObject = usedObject;
	}
}
