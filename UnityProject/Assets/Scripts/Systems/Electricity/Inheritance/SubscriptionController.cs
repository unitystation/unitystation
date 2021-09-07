using System.Collections.Generic;
using UnityEngine;
using Mirror;


namespace Systems.ObjectConnection
{
	/// <summary>
	/// Used for editor scripts.
	/// </summary>
	public abstract class SubscriptionController : NetworkBehaviour
	{
		/// <summary>
		/// Used in <see cref="SubscriptionControllerEditor"/>.
		/// <para>Passes a list of game objects of a tile user click on</para>
		/// </summary>
		/// <returns>Chosen objects.</returns>
		public abstract IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects);
	}
}
