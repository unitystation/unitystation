using UnityEngine;

namespace Objects
{
	public interface IBumpableObject
	{
		void OnBump(GameObject bumpedBy, GameObject client);
	}
}
