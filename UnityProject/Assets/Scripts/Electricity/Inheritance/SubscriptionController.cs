using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Electric.Inheritance
{
	/// <summary>
	/// Used for editor scripts
	/// </summary>
	public abstract class SubscriptionController : NetworkBehaviour
	{

		/// <summary>
		/// Used in SubscriptionControllerEditor
		/// Passes a list of game objects of a tile user click on
		/// </summary>
		/// <param name="potentialObjects"></param>
		/// <returns> Chosen objects </returns>
		 public abstract IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects);
	}
}