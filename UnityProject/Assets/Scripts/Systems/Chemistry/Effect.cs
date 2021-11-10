using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Chemistry
{
	public abstract class Effect : ScriptableObject
	{
		public abstract void Apply(MonoBehaviour sender, float amount);
	}
}