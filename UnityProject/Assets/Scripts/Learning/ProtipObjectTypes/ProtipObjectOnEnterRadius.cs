using System;
using System.Collections.Generic;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnEnterRadius : ProtipObject
	{
		public Dictionary<GameObject, bool> ObjectsToCheck = new Dictionary<GameObject, bool>();

		private void Start()
		{
			throw new NotImplementedException();
		}

		private void CheckForNearbyItems()
		{

		}
	}
}