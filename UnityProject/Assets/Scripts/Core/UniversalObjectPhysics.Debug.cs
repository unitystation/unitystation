using UnityEngine;

namespace Core
{
	public partial class UniversalObjectPhysics
	{
		public bool DEBUG = false;

		[NaughtyAttributes.Button]
		public void DeBGUNewtonianPush()
		{
			NewtonianPush(Vector2.right, 4, 3);
		}
	}
}