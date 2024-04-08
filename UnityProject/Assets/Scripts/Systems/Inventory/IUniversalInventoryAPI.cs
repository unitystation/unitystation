using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems
{
	public interface IUniversalInventoryAPI
	{
		/// <summary>
		/// Shoves all objects in the target list into a storage system, such as ItemStorage or ObjectContainer.
		/// </summary>
		/// <param name="target">The objects that you want to add.</param>
		/// <param name="onGrab">An action to do after the inventory successfully finishes adding all items.</param>
		public void GrabObjects(List<GameObject> target, Action onGrab = null);

		/// <summary>
		/// Drops all objects inside an inventory.
		/// </summary>
		/// <param name="onDrop"></param>
		public void DropObjects(Action onDrop = null);
	}
}