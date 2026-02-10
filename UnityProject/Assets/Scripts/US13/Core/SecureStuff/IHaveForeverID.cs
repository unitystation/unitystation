using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SecureStuff
{
	public interface IHaveForeverID
	{
		public string ForeverID { get; }

		public void ForceSetID();
	}
}

