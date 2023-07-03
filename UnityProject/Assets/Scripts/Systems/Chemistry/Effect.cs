using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chemistry
{
	public abstract class Effect : ScriptableObject
	{
		public abstract void Apply(GameObject sender, float amount);
	}
}