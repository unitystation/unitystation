using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Trigger for an item to be performed before attacking at close range
/// Example: A stun baton would stun the victim when the attack is happening
/// </summary>
public abstract class MeleeIemTrigger: NetworkBehaviour
{
	/// <summary>
	/// Triggers before a melee attack is performed
	/// </summary>
	/// <param name="victim">The entity being affected by the interaction</param>
	/// <returns>Whether damage should be done in the normal way</returns>
	public abstract bool MeleeItemInteract(GameObject victim);
}
