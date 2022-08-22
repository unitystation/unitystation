using UnityEngine;

/// <summary>
/// Abstract, only used internally for IF2 - should not be used in interactable components.
/// Base class containing the info on an attempt to perform a particular interaction -
/// each interaction has a performer (player performing it)
/// and a used object (object they are using, dropping, or combining)
/// </summary>
public abstract class Interaction
{
	/// <summary>The gameobject of the player performing the interaction</summary>
	public GameObject Performer { get; protected set; }

	/// <summary><see cref="PlayerScript"/> of the performer.</summary>
	public PlayerScript PerformerPlayerScript { get; protected set; }

	/// <summary>
	/// Object that is being used by the player to perform the interaction.
	/// For example...
	/// For hand apply - object in hand. Null if empty hand.
	/// For combine - object that was dragged to another slot.
	/// For activate - the object being activated
	/// For mouse drop - the object being dragged and dropped.
	/// </summary>
	public GameObject UsedObject { get; protected set; }

	/// <summary>Intent of the player for this interaction.</summary>
	public Intent Intent { get; protected set; }

	/// <param name="performer">The gameobject of the player performing the interaction</param>
	/// <param name="usedObject">Object that is being used by the player to perform the interaction.
	///  For example...
	/// For hand apply - object in hand. Null if empty hand.
	/// For combine - object that was dragged to another slot.
	/// For activate - the object being activated
	/// For mouse drop - the object being dragged and dropped.</param>
	public Interaction(GameObject performer, GameObject usedObject, Intent intent)
	{
		Performer = performer;
		UsedObject = usedObject;
		Intent = intent;

		if (performer != null)
		{
			PerformerPlayerScript = performer.GetComponent<PlayerScript>();
		}
	}
}
