using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chemistry
{
	public interface IEffect
	{
		void Apply(MonoBehaviour sender, float amount);
	}
}
