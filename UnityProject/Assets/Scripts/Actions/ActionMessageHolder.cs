using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameActions
{
	public class ActionMessageHolder<S, C>
	where S : struct
	where C : struct
	{
		public static C Test()
		{
			return new C();
		}
	}
}
