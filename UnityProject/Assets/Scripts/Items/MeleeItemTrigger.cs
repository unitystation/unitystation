using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Trigger for an item to be performed before attacking at close range
/// Example: A stun baton would stun the victim when the attack is happening
/// </summary>
public abstract class MeleeItemTrigger: NetworkBehaviour
{
	/// <summary>
	/// Triggers before a melee attack is performed
	/// </summary>
	/// <param name="victim">The entity being affected by the interaction</param>
	/// <returns>Whether hit damage should be done and the hit sound played</returns>
	public abstract bool MeleeItemInteract(GameObject originator, GameObject victim);
}
