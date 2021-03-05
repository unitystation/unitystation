using UnityEngine;

namespace Objects
{
	public interface IBumpObject
	{
		void OnBump(GameObject bumpedBy);
	}
}
