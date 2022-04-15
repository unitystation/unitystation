using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Systems.ObjectConnection
{
	/// <summary>
	/// Used for editor scripts.
	/// </summary>
	public interface ISubscriptionController
	{
		/// <summary>
		/// Used in <see cref="SubscriptionControllerEditor"/>.
		/// <para>Passes a list of game objects of a tile user click on</para>
		/// </summary>
		/// <returns>Chosen objects.</returns>
		public IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects);
	}
}
