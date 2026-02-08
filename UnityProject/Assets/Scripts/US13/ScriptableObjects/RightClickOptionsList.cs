using UnityEngine;
using US13.Core.Input_System;

namespace US13.ScriptableObjects
{
	public class RightClickOptionsList : ScriptableObject
	{
		[SerializeField]
		private RightClickOption[] rightClickOptions = new RightClickOption[0];
		public RightClickOption[] RightClickOptions => rightClickOptions;
	}
}
