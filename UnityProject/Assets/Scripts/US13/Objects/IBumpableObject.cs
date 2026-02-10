using UnityEngine;

namespace US13.Objects
{
	public interface IBumpableObject
	{
		void OnBump(GameObject bumpedBy, GameObject client);
	}
}
