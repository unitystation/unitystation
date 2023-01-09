using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chemistry.Components
{
	public interface IProvideReagentMix
	{
		public ReagentMix GetReagentMix();
	}
}